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
    private SubChunk subChunk;

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
    

    //Use a terrainDetailsSettings to initialize this.
    /*
    DETAIL SETTING SHOULD ONLY AFFECT THE BUFFER INITIALIZATION, unless we are overriding the detail material !! 
    
    EVERYTHING ELSE SHOULD WORK THE SAME
    */
    public DetailChunk(Material material, Vector2 atlasSize, SubChunk subChunk, Dictionary<Biomes, TerrainDetailSettings> biomeDetails)
    {
        this.material = material;
        this.atlasSize = atlasSize;
        this.mesh = CreateQuad(0.6f, 0.6f);
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
    // -Sample and interpolate the original biome map when build subchunk and instance details based
    //  on the biome sampled
    // -THE SUBCHUNK SHOULD CONTAIN BIOME INFO AS WELL
    private List<TerrainDetailSettings> GetDetailsSettings(SubChunk subChunk, Dictionary<Biomes, TerrainDetailSettings> biomeDetails)
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
        
        QuadNode headNode = subChunk.GetDataTree().head;

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

        /////////////////////////////////////////////////////////////////////////////////////////////////
        // Search for biomes with bfs and add all biome chunks in a dict
        // Then fill each biome patch individually
        /////////////////////////////////////////////////////////////////////////////////////////////////

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


    // Create a quad mesh
    private Mesh CreateQuad(float width = 1f, float height = 1f) {
        
        var mesh = new Mesh();
        float w = width * .5f;
        float h = height * .5f;
        var vertices = new Vector3[4] {
            new Vector3(-w, 0, 0),
            new Vector3(w, 0, 0),
            new Vector3(-w, 2*h, 0),
            new Vector3(w, 2*h, 0)
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
