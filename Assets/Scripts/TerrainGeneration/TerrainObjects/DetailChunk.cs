using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DetailChunk 
{
    private Dictionary<Biomes, TerrainDetailSettings> biomeDetails;
    public Material material;

    //Each chunk must have a different property block so that they dont share the same meshPropertiesBuffer
    public MaterialPropertyBlock propertyBlock;
    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;
    private Mesh mesh;
    private Bounds bounds;
    private SubChunk subChunk;

    private bool hasDetails = true;



    // Mesh Properties struct to be read from the GPU.
    private struct DetailMeshProperties 
    {
        public Matrix4x4 mat;
        public Vector4 color;


        // Size() is a convenience funciton which returns the stride of the struct.
        public static int Size() {
            return
                sizeof(float) * 4 * 4 + // matrix;
                sizeof(float) * 4;      // color;
        }
    }


    //Use a terrainDetailsSettings to initialize this.
    /*
    DETAIL SETTING SHOULD ONLY AFFECT THE BUFFER INITIALIZATION, unless we are overriding the detail material !! 
    
    EVERYTHING ELSE SHOULD WORK THE SAME
    */
    public DetailChunk(Material material, SubChunk subChunk, Dictionary<Biomes, TerrainDetailSettings> biomeDetails)
    {
        this.material = material;
        this.mesh = CreateQuad();
        this.subChunk = subChunk;
        this.bounds = subChunk.GetBounds();
        this.propertyBlock = new MaterialPropertyBlock();
        this.biomeDetails = biomeDetails;

        if (subChunk.IsReady())
        {
            //subChunk should contain the chunk data tree. USE IT TO ELIMINATE OCCUPIED POSITIONS
            InitializeBuffers(subChunk.GetDetailsSettings(biomeDetails));
        }
        

    }


    //pass in the subchunk and all data necessary to sample different biomes as well
    private void InitializeBuffers(List<TerrainDetailSettings> detailSettings) 
    {
        if(detailSettings.Count == 0)
        {
            hasDetails = false;
            return;
        }
        
        //List<Vector3> positions = subChunk.GetVertices();
        //int population = positions.Count;

        QuadNode headNode = subChunk.GetDataTree().head;

        List<QuadNode> emptyNodes = new List<QuadNode>();
        Queue<QuadNode> nodeQueue = new Queue<QuadNode>();


        if (headNode.children != null)
        {
            nodeQueue.Enqueue(headNode);
        }
        else if(headNode.IsEmpty())
        {
            //ASSUMING ONLY THE LEAF NODES ARE THE ONES THAT CONTAIN OBJECTS
            emptyNodes.Add(headNode);
        }

        
        while (nodeQueue.Count > 0)
        {
            QuadNode currentNode = nodeQueue.Dequeue();
            foreach (QuadNode node in currentNode.children)
            {
                if (headNode.children != null)
                {
                    nodeQueue.Enqueue(node);
                }
                else if(node.IsEmpty())
                {
                    //ASSUMING ONLY THE LEAF NODES ARE THE ONES THAT CONTAIN OBJECTS
                    emptyNodes.Add(node);
                }
            }

        }
        

        //assign the positions for each detail
        List<DetailMeshProperties> meshProperties = new List<DetailMeshProperties>();

        int subChunkSize = subChunk.GetSize();

        foreach (QuadNode emptyNode in emptyNodes)
        {
            //loop over details again
            float step = 1/(subChunkSize * detailSettings[0].density * 2);
            //this is the bounds relative to the parent chunk
            Bounds detailRegion = emptyNode.GetFractionalBounds();

            float startX = detailRegion.center.x - detailRegion.extents.x;
            float startY = detailRegion.center.y - detailRegion.extents.y;
            float finalX = detailRegion.center.x + detailRegion.extents.x;
            float finalY = detailRegion.center.y + detailRegion.extents.y;


            for (float y = startY; y < finalY; y += step)
            {
                for (float x = startX; x < finalX; x += step)
                {
                    DetailMeshProperties props = new DetailMeshProperties();
                    Vector3 position = subChunk.SamplePosition(x,y); // with  0 < x,y < 1
                    Quaternion rotation = Quaternion.Euler(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180));
                    Vector3 scale = Vector3.one;

                    props.mat = Matrix4x4.TRS(position, rotation, scale);
                    props.color = Color.Lerp(Color.red, Color.blue, Random.value);

                    meshProperties.Add(props);
                    //subChunk.SamplePosition(x,y); // with  0 < x,y < 1
                }
            }


        }

        int population = meshProperties.Count;



////////////////////////////////////////////-WIP-///////////////////////////////////////////////////////////////////
        /*
            Use the generation settings to get the grid of points 
        */
        
        //sample the points based on a density and the bounds of a given tree node
        //subChunk.SamplePosition(x,y); with  0 < x,y < 1

        //////////////////////////////////////////////////////////////////////////////////////////////////////////
        //use the subchunk biome map to draw the specific details belonging to each biome.
        //INCREASE THE BIOME MAP DENSITY
        //-add all the details to one single atlas. add a biome parameter inside the detail shader to select the 
        //appropriate tile. 
        //-if a biome has nothing in the atlas, just clip and draw nothing !!
        //-add the density parameter to properly add these details with proper spacing 
        /////////////////////////////////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        

        if (population == 0)
        {
            return;
        }


        /*
        // Initialize buffer with the given population.
        DetailMeshProperties[] properties = new DetailMeshProperties[population];

        for (int i = 0; i < population; i++) {
            DetailMeshProperties props = new DetailMeshProperties();
            Vector3 position = positions[i];
            Quaternion rotation = Quaternion.Euler(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180));
            Vector3 scale = Vector3.one;

            props.mat = Matrix4x4.TRS(position, rotation, scale);
            props.color = Color.Lerp(Color.red, Color.blue, Random.value);

            properties[i] = props;
        }*/



        // Argument buffer used by DrawMeshInstancedIndirect.
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)population;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        

        meshPropertiesBuffer = new ComputeBuffer(population, DetailMeshProperties.Size());
        meshPropertiesBuffer.SetData(meshProperties);
        propertyBlock.SetBuffer("_Properties", meshPropertiesBuffer);
        //material.SetBuffer("_Properties", meshPropertiesBuffer);
    }

    public void Draw()
    {   
        if (!hasDetails)
        {
            return;
        }


        if (meshPropertiesBuffer != null && argsBuffer != null)
        {
            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer, properties:propertyBlock);
        }
        else if (subChunk.IsReady())
        {
            InitializeBuffers(subChunk.GetDetailsSettings(biomeDetails));
        }
        
        
    }

    public void Clear() 
    {
        if (meshPropertiesBuffer != null) {
            meshPropertiesBuffer.Release();
        }
        meshPropertiesBuffer = null;

        if (argsBuffer != null) {
            argsBuffer.Release();
        }
        argsBuffer = null;
    }


    // Create a quad mesh
    private Mesh CreateQuad(float width = 1f, float height = 1f) {
        
        var mesh = new Mesh();
        float w = width * .5f;
        float h = height * .5f;
        var vertices = new Vector3[4] {
            new Vector3(-w, -h, 0),
            new Vector3(w, -h, 0),
            new Vector3(-w, h, 0),
            new Vector3(w, h, 0)
        };

        var tris = new int[6] {
            // lower left triangle
            0, 2, 1,
            // lower right triangle
            2, 3, 1
        };

        var normals = new Vector3[4] {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
        };

        var uv = new Vector2[4] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.normals = normals;
        mesh.uv = uv;

        return mesh;
    }



}
