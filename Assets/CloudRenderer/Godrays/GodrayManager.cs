using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Light), typeof(Volume), typeof(CloudManager))]
public class GodrayManager : MonoBehaviour
{
    
    private Light mainLight;
    private CloudManager cloudManager;
    private RenderParams rp;
    private Mesh quadMesh;
    [SerializeField] private Camera mainCamera;
    private Volume volume;
    
    const int chunkSize = 129;   //(including "border cells" necessary for the unique vert generation scheme)
                                //For this implementation we must have: (chunkSize * chunkSize) < MAX = 1024 * 1024 
    [SerializeField] private ComputeShader marchingSquaresShader;
    [SerializeField] private ComputeShader prefixSumScanShader;
    [SerializeField] private Material godrayMaterial;
    [SerializeField] private Material godrayMaskMaterial;

    private ComputeBuffer cellDataBuffer;
    private ComputeBuffer offsetBuffer;
    private ComputeBuffer sumsBuffer;
    private GraphicsBuffer indirectDrawBuffer;

    private ComputeBuffer vertexBuffer;//buffer for rendering the quads that represent godrays
    private GraphicsBuffer indexBuffer;

    //Constant Buffers:
    private ComputeBuffer MSEdgeLUTsBuffer;
    private ComputeBuffer lineConnectTable;
    private ComputeBuffer numLinesTable;

    private int cellCount;
    private int maximumCellIds;

    private RenderParams renderParams;


    private static readonly int 
        chunkSizeID = Shader.PropertyToID("chunkSize"),
        totalCellsID = Shader.PropertyToID("totalCells"),

        cellDataID = Shader.PropertyToID("cellData"),
        offsetsID = Shader.PropertyToID("offsetBuffer"),

        edgeLUTsID = Shader.PropertyToID("MSEdgeLUTs"),
        numLineTableID = Shader.PropertyToID("caseToLineNum"),
        lineConnectTableID = Shader.PropertyToID("lineConnectTable"),

        vertBufferID = Shader.PropertyToID("vertexBuffer"),
        indBufferID = Shader.PropertyToID("indexBuffer"),

        indArgsID = Shader.PropertyToID("indirectArgsBuffer"),
        sumsInID = Shader.PropertyToID("sumsBufferIn"),
        sumsOutID = Shader.PropertyToID("sumsBufferOut"),

        posOffsetID = Shader.PropertyToID("centerOffset"),
        lightDirID = Shader.PropertyToID("lightDir"),
        scaleID = Shader.PropertyToID("scale")
        ;




    struct Vert 
    {
        public Vector4 position;
        public Vector4 normal;

        public static int Size()
        {
            return 8 * sizeof(float);
        }
    };
    private Mesh GenerateQuadMesh()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = {
            new Vector3(0, 0, -1),
            new Vector3(0, 1, -1),
            new Vector3(0, 1, 1),
            new Vector3(0, 0, 1),
        };
        mesh.vertices = vertices;

        int[] tris = {0, 2, 1, 0, 3, 2};
        mesh.triangles = tris;

        return mesh;
    }
    
    void Start()
    {
        mainLight = GetComponent<Light>();
        cloudManager = GetComponent<CloudManager>();

        quadMesh = GenerateQuadMesh();

        cellCount = chunkSize * chunkSize; // MAX = 1024 * 1024

        maximumCellIds = Mathf.CeilToInt(cellCount / 1024.0f) * 1024;

        cellDataBuffer = new ComputeBuffer(cellCount, sizeof(uint));

        offsetBuffer = new ComputeBuffer(maximumCellIds, 2 * sizeof(uint));
        sumsBuffer = new ComputeBuffer(1024, 2 * sizeof(uint));

        indirectDrawBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        GraphicsBuffer.IndirectDrawIndexedArgs[] indirectDrawData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];

        /* For RenderPrimitivesIndexedIndirect (DOESNT WORK): 
        indirectDrawData[0].indexCountPerInstance = 0;
        indirectDrawData[0].baseVertexIndex = 0;
        indirectDrawData[0].startIndex = 0;
        indirectDrawData[0].instanceCount = 1;
        indirectDrawData[0].startInstance = 0;
        */
        indirectDrawData[0].indexCountPerInstance = quadMesh.GetIndexCount(0);
        indirectDrawData[0].baseVertexIndex = 0;
        indirectDrawData[0].startIndex = 0;
        indirectDrawData[0].instanceCount = 0;
        indirectDrawData[0].startInstance = 0;
        indirectDrawBuffer.SetData(indirectDrawData);

        vertexBuffer = new ComputeBuffer(2 * cellCount, Vert.Size());
        indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, 2 * 2 * cellCount, sizeof(uint));
        

        MSEdgeLUTsBuffer = new ComputeBuffer(1, MSTables.EdgeLUTs.Size(), ComputeBufferType.Constant);
        MSEdgeLUTsBuffer.SetData(MSTables.EdgeLUTs.GetEdgeLUTs());

        numLinesTable = new ComputeBuffer(MSTables.caseToNumLines.Length, sizeof(uint));
        numLinesTable.SetData(MSTables.caseToNumLines);

        lineConnectTable = new ComputeBuffer(MSTables.caseToNumLines.Length * 2, 2 * sizeof(int));
        lineConnectTable.SetData(MSTables.edgeConnectList);


        renderParams = new RenderParams(godrayMaterial);
        renderParams.worldBounds = new Bounds(mainLight.transform.position, 6400*Vector3.one); // use tighter bounds
        renderParams.matProps = new MaterialPropertyBlock();
        renderParams.matProps.SetBuffer("vertexBuffer", vertexBuffer);
        renderParams.matProps.SetBuffer("indexBuffer", indexBuffer);
        //renderParams.layer = layer;
        //renderParams.renderingLayerMask = 0;
        //renderParams.renderingLayerMask = 8;
    }
    void Update()
    {
        MSUpdate();

        Vector3 lightPos = mainLight.transform.position;
        renderParams.worldBounds = new Bounds(lightPos, 200*Vector3.one); // use tighter bounds

        Vector3 lightdir = mainLight.transform.forward;
        Vector3 zyPlane = (lightdir - Vector3.Dot(lightdir, Vector3.right) * Vector3.right).normalized;
        Vector3 xyPlane = (lightdir - Vector3.Dot(lightdir, Vector3.forward) * Vector3.forward).normalized;


        Vector3 scale = new Vector3(cloudManager.CloudTileSize.x/(-xyPlane.y), 1.0f, cloudManager.CloudTileSize.y/(-zyPlane.y));
        renderParams.matProps.SetVector(posOffsetID, lightPos);
        renderParams.matProps.SetVector(scaleID, scale);
        renderParams.matProps.SetVector(lightDirID, lightdir);
         
        Graphics.RenderMeshIndirect(renderParams, quadMesh, indirectDrawBuffer);
    }

    void MSUpdate()
    {
        // 1) Clear the Offset array:
        int dispatchCount = Mathf.CeilToInt(maximumCellIds/512.0f); //ceil not necessary here
        marchingSquaresShader.SetInt(totalCellsID, maximumCellIds);
        marchingSquaresShader.SetBuffer(0, offsetsID, offsetBuffer); 
        marchingSquaresShader.Dispatch(0, dispatchCount, 1, 1); 

        // 2) Mark Cells:
        int dispatchDim = Mathf.CeilToInt((chunkSize + 1)/32.0f);
        marchingSquaresShader.SetInt(chunkSizeID, chunkSize);
        marchingSquaresShader.SetBuffer(1, cellDataID, cellDataBuffer);
        marchingSquaresShader.SetBuffer(1, offsetsID, offsetBuffer);
        marchingSquaresShader.SetBuffer(1, numLineTableID, numLinesTable);
        marchingSquaresShader.Dispatch(1, dispatchDim, dispatchDim, 1);

        /*
        uint[] offsets = new uint[2*maximumCellIds];
        offsetBuffer.GetData(offsets);
        for (int i = 0; i < maximumCellIds; i++)
        {
            Debug.Log("("+ offsets[2*i] +","+ offsets[2*i+1]  +")");
        }*/


        // 3) Scan:

        //clear sum buffer:
        prefixSumScanShader.SetBuffer(0, sumsOutID, sumsBuffer);
        prefixSumScanShader.Dispatch(0, 1024/512, 1, 1);

        //initial block sum:
        int offsetBlockCount = maximumCellIds/1024;
        prefixSumScanShader.SetBuffer(1, offsetsID, offsetBuffer);
        prefixSumScanShader.SetBuffer(1, sumsOutID, sumsBuffer);
        prefixSumScanShader.Dispatch(1, offsetBlockCount, 1, 1);

        //sum the blocks:
        // Prefix sum scan on the sums buffer
        prefixSumScanShader.SetBuffer(2, offsetsID, sumsBuffer);
        prefixSumScanShader.SetBuffer(2, indArgsID, indirectDrawBuffer); //final sums buffer
        prefixSumScanShader.Dispatch(2, 1, 1, 1); 
        
        

        // Apply the sums back into the offsets buffer:
        if (offsetBlockCount > 1)
        {
            prefixSumScanShader.SetBuffer(3, offsetsID, offsetBuffer);
            prefixSumScanShader.SetBuffer(3, sumsInID, sumsBuffer); 
            prefixSumScanShader.Dispatch(3, offsetBlockCount - 1, 1, 1); 
        }

        /*
        uint[] offsets = new uint[2*maximumCellIds];
        offsetBuffer.GetData(offsets);
        for (int i = 0; i < maximumCellIds; i++)
        {
            Debug.Log("("+ offsets[2*i] +","+ offsets[2*i+1]  +")");
        }*/

        /*
        GraphicsBuffer.IndirectDrawIndexedArgs[] indirectDrawData2 = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        indirectDrawBuffer.GetData(indirectDrawData2);
        Debug.Log("index count: " +  indirectDrawData2[0].indexCountPerInstance);
        Debug.Log("instance count: " +  indirectDrawData2[0].instanceCount);
        Debug.Log("startIndex: " +  indirectDrawData2[0].startIndex);
        Debug.Log("startInstance: " +  indirectDrawData2[0].startInstance);
        Debug.Log("baseVertexIndex: " +  indirectDrawData2[0].baseVertexIndex);*/


        // 4) Generate verts and index buffer:
        marchingSquaresShader.SetConstantBuffer(edgeLUTsID, MSEdgeLUTsBuffer, 0, MSTables.EdgeLUTs.Size());
        marchingSquaresShader.SetBuffer(2, cellDataID, cellDataBuffer);
        marchingSquaresShader.SetBuffer(2, offsetsID, offsetBuffer);
        marchingSquaresShader.SetBuffer(2, lineConnectTableID, lineConnectTable);
        marchingSquaresShader.SetBuffer(2, vertBufferID, vertexBuffer);
        marchingSquaresShader.SetBuffer(2, indBufferID, indexBuffer);
        marchingSquaresShader.Dispatch(2, dispatchDim, dispatchDim, 1);

        
        /*Vert[] verts = new Vert[3 * cellCount];
        vertexBuffer.GetData(verts);
        for (int i = 0; i < maximumCellIds; i++)
        {
            Debug.Log(verts[i].position);
        }
        
        uint[] ids = new uint[3 * 5 * cellCount];
        indexBuffer.GetData(ids);
        for (int i = 0; i < 3 * 2 * maximumCellIds; i+=3)
        {
            Debug.Log("(" + ids[i] + ", " + ids[i+1] + ", " + ids[i+2] + ")");
        }*/


    }

    



    private void OnDestroy() 
    {
        cellDataBuffer?.Release();

        offsetBuffer?.Release();
        sumsBuffer?.Release();
        indirectDrawBuffer?.Release();

        vertexBuffer?.Release();
        indexBuffer?.Release();

        MSEdgeLUTsBuffer?.Release();
        lineConnectTable?.Release();
        numLinesTable?.Release();
    }







    /*
    void OnDrawGizmosSelected()
    {
        if (mainCamera == null) {return;}
        mainLight = GetComponent<Light>();
        volume = GetComponent<Volume>();
        GodrayVolumeComponent godraySettings;
        volume.profile.TryGet<GodrayVolumeComponent>(out godraySettings);
        if (godraySettings == null) {return;}

        int samples = (int)godraySettings.samples;
        float start = (float)godraySettings.start;
        float end = (float)godraySettings.end;

        Color tempColor = Gizmos.color;
        Matrix4x4 tempMat = Gizmos.matrix;

        //Drawing camera frustum:
        float cameraHeight = mainCamera.orthographicSize * 2.0f;
        float cameraWidth =  cameraHeight * mainCamera.aspect;

        Vector3 frustumScale = new Vector3(cameraWidth, cameraHeight, mainCamera.farClipPlane - mainCamera.nearClipPlane);
        Vector3 frustumCenter = (mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, mainCamera.farClipPlane)) 
                                + mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, mainCamera.nearClipPlane))) / 2;

        Gizmos.matrix = Matrix4x4.TRS(frustumCenter, mainCamera.transform.rotation, Vector3.one);
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(Vector3.zero, frustumScale);
        
        Vector3 lightDir = mainLight.transform.forward;
        Vector3 tangent = Vector3.Cross(lightDir, -mainCamera.transform.forward); 
        Vector3 normal = Vector3.Cross(tangent, lightDir).normalized; 

        float planeSeparation = (end - start) * (mainCamera.farClipPlane - mainCamera.nearClipPlane)/samples;
        float planeOffset = start * (mainCamera.farClipPlane - mainCamera.nearClipPlane) + mainCamera.nearClipPlane;

        Quaternion rotation = Quaternion.LookRotation(normal);
        Vector3 planeCenter = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, planeOffset));
        Vector3 planeScale = new Vector3(
            1f/Mathf.Abs(Vector3.Dot((normal - Vector3.Dot(normal, mainCamera.transform.up) * mainCamera.transform.up).normalized, mainCamera.transform.forward)), 
            1f/Mathf.Abs(Vector3.Dot((normal - Vector3.Dot(normal, mainCamera.transform.right) * mainCamera.transform.right).normalized, mainCamera.transform.forward)), 
            1f 
        );
        
        Gizmos.color = Color.red;
        for (int i = 0; i < samples; i++)
        {
            Gizmos.matrix = Matrix4x4.TRS(planeCenter + mainCamera.transform.forward * (planeSeparation * i), rotation, planeScale);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(cameraWidth, cameraHeight, 0.01f));
            Gizmos.DrawLine(new Vector3(-cameraWidth/2,-cameraHeight/2, 0), new Vector3(cameraWidth/2,cameraHeight/2, 0));
        }

        Gizmos.color = tempColor;
        Gizmos.matrix = tempMat;
        
    }*/
}
