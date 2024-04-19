using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class DetailChunk 
{
    private Dictionary<Biomes, TerrainDetailSettings> biomeDetails;
    public Material material;
    public Vector2 atlasSize;

    //Each chunk must have a different property block so that they dont share the same meshPropertiesBuffer
    public MaterialPropertyBlock propertyBlock;
    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;
    private Mesh mesh;
    private Bounds bounds;
    private QuadChunk subChunk;

    private bool hasDetails = true;

    
    // Mesh Properties struct to be read from the GPU.
    private struct DetailMeshProperties 
    {
        public Matrix4x4 mat;
        public Vector4 offsetScale;
        public Vector3 normal;
        public Vector2 atlasUV;


        // Size() is a convenience funciton which returns the stride of the struct.
        public static int Size() {
            return
                sizeof(float) * 4 * 4 + // matrix;
                sizeof(float) * 4 +     // offsetScale;
                sizeof(float) * 3 +     // normal;
                sizeof(float) * 2;      // atlasUVs;

        }
    }
    
    public DetailChunk(Material material, Vector2 atlasSize, QuadChunk subChunk, Dictionary<Biomes, TerrainDetailSettings> biomeDetails)
    {
        this.material = material;
        this.atlasSize = atlasSize;
        this.mesh = MeshData.CreateQuad(0.6f, 0.6f);
        this.subChunk = subChunk;
        this.bounds = subChunk.Bounds;
        this.propertyBlock = new MaterialPropertyBlock();
        this.biomeDetails = biomeDetails;

        if (subChunk.IsReady())
        {
            //subChunk should contain the chunk data tree. USE IT TO ELIMINATE OCCUPIED POSITIONS
            InitializeBuffers(GetDetailsSettings(subChunk, biomeDetails));
        }
    }





    //Currently only gets a 2x2 grid of details. Improve this: 
    // -Store every biome present in chunk in list of unique biomes
    // -Create a list of every possible terrain detail setting based on the present biomes

    //
    // NULL REF ON BIOME MAP. SEE QuadChunk class
    //
    private List<TerrainDetailSettings> GetDetailsSettings(QuadChunk subChunk, Dictionary<Biomes, TerrainDetailSettings> biomeDetails)
    {
        Biomes[] biomeMap = subChunk.GetBiomeMap();

        List<TerrainDetailSettings> detailList = new List<TerrainDetailSettings>();
        
        for (int i = 0; i < 4; i++)
        {
            if (biomeDetails[biomeMap[i]] != null)
            {
                detailList.Add(biomeDetails[biomeMap[i]]);
            }
        }

        return detailList;
    }

    //pass in the subchunk and all data necessary to sample different biomes as well
    private void InitializeBuffers(List<TerrainDetailSettings> detailSettings) 
    {
        if(detailSettings.Count == 0)
        {
            hasDetails = false;
            return;
        }

        QuadNode headNode = subChunk.GetDataTree().head; // must set data tree of the chunk
        List<QuadNode> emptyNodes = new List<QuadNode>();
        Queue<QuadNode> nodeQueue = new Queue<QuadNode>();

        if (headNode.children != null)
        {
            nodeQueue.Enqueue(headNode);
        }
        else if(headNode.IsEmpty())
        {
            emptyNodes.Add(headNode);
        }
        while (nodeQueue.Count > 0)
        {
            QuadNode currentNode = nodeQueue.Dequeue();
            foreach (QuadNode node in currentNode.children)
            {
                // Look at the entire tree and get only the empty leaf nodes
                if (headNode.children != null)
                {
                    nodeQueue.Enqueue(node);
                }
                else if(node.IsEmpty())
                {
                    emptyNodes.Add(node);
                }
            }

        }


        List<DetailMeshProperties> meshProperties = new List<DetailMeshProperties>();
        int subChunkSize = subChunk.ChunkSize;

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // on GPU
        // ->SHIP THE POSITIONS TO THE GPU AND SAMPLE THE BIOME TEXTURE THERE. Then we can decide what detail will be used
        // -unify all detail settings into one buffer and send to this compute (? maybe cpu is better for random functions)
        // -We only send positions and atlas offset into the draw call
        // -variable density will still be a problem to solve !
        //
        // or on CPU
        // -use one specific density for every possible biome inside the quad (this will be based on the sampling of
        // biomes in each quad corner, similar to marching squares)
        // -sample the quad with this density and only add details where the biome matches
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        foreach (QuadNode emptyNode in emptyNodes)
        {
            float step = 1/(subChunkSize * detailSettings[0].density);

            Vector2 atlasOffset = detailSettings[0].atlasOffset/atlasSize;
            Vector2 size = detailSettings[0].size/atlasSize;
            int variants = detailSettings[0].numVariants;

            //this is the bounds relative to the parent chunk
            Bounds detailRegion = emptyNode.GetFractionalBounds();

            float startX = detailRegion.center.x - detailRegion.extents.x;
            float startY = detailRegion.center.y - detailRegion.extents.y;
            float finalX = detailRegion.center.x + detailRegion.extents.x;
            float finalY = detailRegion.center.y + detailRegion.extents.y;

            // sample the maps with: 0 < x,y < 1
            for (float y = startY; y < finalY; y += step)
            {
                for (float x = startX; x < finalX; x += step)
                {
                    //sample biome map check on detailSettings
                    DetailMeshProperties props = new DetailMeshProperties();
                    float sampleX = x + Random.Range(-step * 0.3f, step * 0.3f);
                    float sampleY = y + Random.Range(-step * 0.3f, step * 0.3f);
                    
                    Vector3 position = subChunk.SamplePosition(sampleX,sampleY); 
                    Vector3 terrainNormal = subChunk.SampleNormal(sampleX,sampleY);
                    Vector2 atlasUV = subChunk.SampleAtlasUV(sampleX,sampleY);

                    Quaternion rotation = Quaternion.FromToRotation(Vector3.up, terrainNormal) * Quaternion.Euler(15, 0, 0);
                    Vector3 scale = Vector3.one;

                    props.mat = Matrix4x4.TRS(position, rotation, scale);
                    int variant = Random.Range(0,variants);
                    props.offsetScale = new Vector4(atlasOffset.x + size.x * variant, atlasOffset.y, size.x, size.y);
                    props.normal = terrainNormal;
                    props.atlasUV = atlasUV;

                    meshProperties.Add(props);
                }
            }
        }

        int population = meshProperties.Count;
        if (population == 0)
        {
            hasDetails = false;
            return;
        }

        // Argument buffer used by DrawMeshInstancedIndirect.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)population;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        meshPropertiesBuffer = new ComputeBuffer(population, DetailMeshProperties.Size());
        meshPropertiesBuffer.SetData(meshProperties);
        propertyBlock.SetBuffer("_Properties", meshPropertiesBuffer);
    }

    public void Draw()
    {   
        if (!hasDetails)
        {
            return;
        }
        if (meshPropertiesBuffer != null && argsBuffer != null)
        {
            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer, properties:propertyBlock, layer:6);
        }
        else if (subChunk.IsReady())
        {
            InitializeBuffers(GetDetailsSettings(subChunk, biomeDetails));
        }
    }

    public void Clear() 
    {
        meshPropertiesBuffer?.Release();
        meshPropertiesBuffer = null;

        argsBuffer?.Release();
        argsBuffer = null;
    }
}
