using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;




//each node must have the data necessary for a subchunk to generate all vertex positions belonging to that node
public class QuadNode
{
    private int index;
    private Vector2Int position;
    private int depth;
    public QuadNode[] children;


    //all the objects that FILL this node
    private List<TerrainObject> objects = new List<TerrainObject>();

    public QuadNode(Vector2Int position, int index, int depth, QuadNode[] children = null)
    {   
        this.position = position;
        this.index = index;
        this.depth = depth;
        this.children = children;
    }

    public void Subdivide()
    {
        this.children = new QuadNode[4]
        {
            new QuadNode(Vector2Int.zero  + 2 * position, 0, depth+1),
            new QuadNode(Vector2Int.right + 2 * position, 1, depth+1),
            new QuadNode(Vector2Int.up    + 2 * position, 2, depth+1),
            new QuadNode(Vector2Int.one   + 2 * position, 3, depth+1)
        };
    }

    //bounds of this subchunk in fractional coordinates relative to the main chunk
    public Bounds GetFractionalBounds() 
    {
        Vector2 center = (position + 0.5f * Vector2.one) / MathMisc.TwoPowX(depth);
        Vector2 size = Vector2.one / MathMisc.TwoPowX(depth);

        return new Bounds(center, size);
    }

    public void AddObject(TerrainObject terrainObject)
    {
        //Change the TerrainObject position
        //terrainObject.position = (position + 0.5f * Vector2.one) / MathMisc.TwoPowX(depth);
        objects.Add(terrainObject);
    }

    //returns the total count of objects inside this node or its children
    public int GetObjectsCount()
    {
        int count = objects.Count;
        if (children == null)
        {
            return count;
        }

        foreach(QuadNode child in children)
        {
            count += (child == null)? 0 : child.GetObjectsCount();
        }

        return count;
    }

    public QuadNode[] GetChildren()
    {
        return children;
    }

    public bool IsEmpty()
    {
        return objects.Count == 0;
    }

}


// Quadtree to store the chunk objects information as well as all other relevant data
public class ChunkDataTree 
{
    public QuadNode head;

    //the tree data is only deleted when the terrain chunk is deleted, so it will never generate duplicated objects
    public ChunkDataTree(QuadNode head)
    {
        this.head = head;
    }

    public ChunkDataTree(int startDepth = 0)
    {
        this.head = new QuadNode(Vector2Int.zero, 0, 0);

        Queue<QuadNode> startQueue = new Queue<QuadNode>();
        startQueue.Enqueue(head);


        for (int i = 0; i < startDepth; i++)
        {
            Queue<QuadNode> newQueue = new Queue<QuadNode>();
            
            while(startQueue.Count > 0)
            {
                QuadNode currentNode = startQueue.Dequeue();
                currentNode.Subdivide();

                foreach (QuadNode child in currentNode.children)
                {
                    newQueue.Enqueue(child);
                }
            }

            startQueue = newQueue;
        }

        //subChunkNodes = startQueue.ToArray();

        startQueue.Clear();
    }

    //Get a data tree that starts at a particular node from this tree
    public ChunkDataTree GetSubTree(int index)
    {
        return null;
        //return new ChunkDataTree();
    }

    //Convert a position of a tree node to a 3d position relative to the chunk origin
    public Vector3 TreeIdToRelativePos()
    {
        return Vector3.zero;
    }

    //Convert a position relative to the main chunk origin to a tree node
    public int RelativePosToTreeId()
    {
        return 0;
    }

    public List<QuadNode> GetFreeNodes()
    {
        //Check what subchunks are empty and return their vertices in a single (non-sorted) list
        //SubChunk(position, int size, int chunkSize, List<Vector3> mainVertices);
        return null;
    }
}


//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////





public class QuadChunk
{
    protected ChunkDataTree dataTree; //still needs to be implemented properly
    protected QuadChunk rootChunk;
    public Vector2Int Position {get; protected set;}
    private int level;

    //vertex cache:
    protected Vector3[] vertices;
    //normal cache:
    protected Vector3[] normals;
    //uv map cache:
    protected Vector2[] atlasUVs; 
    //biome map cache:
    protected Biomes[] biomeMap;

    public int ChunkSize {get; protected set;}
    public float Scale {get; protected set;}
    public Vector2 WorldPosition {get; protected set;}
    private Vector3 relativePos3D;
    public Bounds Bounds {get; protected set;}
    protected bool isReady = false;
    

    public QuadChunk(Vector2Int Position, int chunkSize, float chunkScale)
    {
        this.Position = Position;
        this.rootChunk = null;
        this.level = 0;

        this.ChunkSize = chunkSize;
        this.Scale = chunkScale;

        this.relativePos3D = Vector3.zero;
    }

    public QuadChunk(Vector2Int Position, QuadChunk rootChunk, int level)
    {
        this.Position = Position;
        this.rootChunk = rootChunk;
        this.level = level;

        if (rootChunk != null)
        {
            this.ChunkSize = rootChunk.ChunkSize/MathMisc.TwoPowX(level);
            this.Scale = rootChunk.Scale;

            Vector2 relativePos = ((Vector2)Position + Vector2.one * 0.5f) * ChunkSize * Scale 
                                - Vector2.one * rootChunk.ChunkSize * Scale * 0.5f;
            this.relativePos3D = new Vector3(relativePos.x, 0, relativePos.y);
            
            this.WorldPosition = relativePos + rootChunk.WorldPosition;
            this.Bounds = new Bounds(new Vector3(WorldPosition.x, 0, WorldPosition.y), new Vector3(1,50,1) * ChunkSize * Scale);
        }
    }
    
    public QuadChunk GetSubChunk(Vector2Int subChunkPos, int level)
    {
        if (rootChunk != null)
        {
            return new QuadChunk(subChunkPos, rootChunk, level);
        }

        return new QuadChunk(subChunkPos, this, level);
    }

    public QuadChunk GetSubChunk(Vector2 worldPos, int level)
    {
        Vector2Int subChunkPos = WorldToSubChunkCoords(worldPos, level);

        if (rootChunk != null)
        {
            return new QuadChunk(subChunkPos, rootChunk, level);
        }

        return new QuadChunk(subChunkPos, this, level);
    }

    public ChunkDataTree GetSubDataTree(Vector2Int subChunkId, int level)
    {
        Vector2Int startPos = subChunkId/MathMisc.TwoPowX(level - 1);
        int startIndex = startPos.x + 2 * startPos.y;

        QuadNode currentNode = dataTree.head.children[startIndex];

        for (int i = 1; i <= level - 1; i++)
        {
            Vector2Int newPos = subChunkId/MathMisc.TwoPowX(level - 1 - i);
            int newIndex = (newPos.x - 2 * startPos.x) + 2 * (newPos.y - 2 * startPos.y);
            startPos = newPos;

            currentNode = currentNode.children[newIndex];
        }

        return new ChunkDataTree(currentNode);
    }
    public virtual ChunkDataTree GetDataTree()
    {
        if (!IsReady())
        {
            return null;
        }
        if (dataTree == null && rootChunk != null)
        {
            dataTree = rootChunk.GetSubDataTree(Position, level);
        }

        return dataTree;
    }

    public virtual Biomes[] GetBiomeMap()
    {
        if (!IsReady())
        {
            return null;
        }

        Biomes[] subBiomeMap = new Biomes[4];

        int subChunkCount = MathMisc.TwoPowX(level);

        for (int j = 0; j < 2; j++)
        {
            for (int i = 0; i < 2; i++)
            { 
                int ind = (Position.x + i) + (subChunkCount + 1) * (Position.y + j);
                //Debug.Log(rootChunk.biomeMap[ind]);
                subBiomeMap[i + 2 * j] = rootChunk.biomeMap[ind];
            }
        }
        return subBiomeMap;
    }

    public virtual Vector3[] GetVertices()
    {
        /*if (vertices != null)
        {
            return vertices;
        }
        
        vertices = new List<Vector3>();
        List<Vector3> mainVertices = parentChunk.GetVertices();
        //VERTEX POSITIONS ARE GIVEN RELATIVE TO THE PARENT CHUNK ORIGIN
        //they have to be converted to be relative to the subchunk origin:

        for (int y = position.y * ChunkSize; y < (position.y + 1) * ChunkSize; y++)
        {
            for (int x = position.x * ChunkSize; x < (position.x + 1) * ChunkSize; x++)
            {
                Vector3 pos = mainVertices[x + (parentChunk.ChunkSize + 1) * y] - relativePos3D;
                this.vertices.Add(pos);
            }
        }*/
        return vertices;
    }
    
    public virtual Vector3[] GetNormals()
    {
        return normals;
    }

    public virtual Vector2[] GetAtlasUVs()
    {
        /*if (atlasUVs != null)
        {
            return atlasUVs;
        }
        
        atlasUVs = new List<Vector2>();
        List<Vector2> aUVs = parentChunk.GetAtlasUVs();

        for (int y = position.y * ChunkSize; y < (position.y + 1) * ChunkSize; y++)
        {
            for (int x = position.x * ChunkSize; x < (position.x + 1) * ChunkSize; x++)
            {
                Vector2 aUV = aUVs[x + (parentChunk.ChunkSize + 1) * y];
                this.atlasUVs.Add(aUV);
            }
        }*/
        return atlasUVs;
    }

    //Interpolate vertex positions at the (x, y) fractional coordinates (0 < x,y < 1)
    public Vector3 SamplePosition(float x, float y)
    {
        int subChunkLength = MathMisc.TwoPowX(level);

        float _x = x * ChunkSize * subChunkLength;
        float _y = y * ChunkSize * subChunkLength;

        int x0 = Mathf.Min(rootChunk.ChunkSize - 1, Mathf.Max(Mathf.FloorToInt(_x), 0));
        int y0 =  Mathf.Min(rootChunk.ChunkSize - 1, Mathf.Max(Mathf.FloorToInt(_y), 0));

        /////////////////////////////////////////////////////////////////////////////////////////
        // WHEN CHUNKS BECOME INVISIBLE, THEIR VERTEX DATA GETS DELETED AND THIS CAUSES NULL REFS
        //////////////////////////////////////////////////////////////////////////////////////////

        Vector3 v00 = rootChunk.vertices[x0 + (rootChunk.ChunkSize + 1) * y0];
        Vector3 v10 = rootChunk.vertices[(x0 + 1) + (rootChunk.ChunkSize + 1) * y0];
        Vector3 v01 = rootChunk.vertices[x0 + (rootChunk.ChunkSize + 1) * (y0 + 1)];
        Vector3 v11 = rootChunk.vertices[(x0 + 1) + (rootChunk.ChunkSize + 1) * (y0 + 1)];
        

        float wx = _x - x0;
        float wy = _y - y0;
        //VERTEX POSITIONS ARE GIVEN RELATIVE TO THE PARENT CHUNK ORIGIN (they have to be converted by using the chunk coord)
        return (1-wy)*(1-wx)*v00 + (1-wy)*(wx)*v10 + (wy)*(1-wx)*v01 + (wy)*(wx)*v11 - relativePos3D;
    }

    //Interpolate the terrain normal at the (x, y) fractional coordinates (0 < x,y < 1)
    public Vector3 SampleNormal(float x, float y)
    {
        int subChunkLength = MathMisc.TwoPowX(level);

        float _x = x * ChunkSize * subChunkLength;
        float _y = y * ChunkSize * subChunkLength;

        int x0 = Mathf.Min(rootChunk.ChunkSize - 1, Mathf.Max(  Mathf.FloorToInt(_x), 0 ));
        int y0 =  Mathf.Min(rootChunk.ChunkSize - 1, Mathf.Max(  Mathf.FloorToInt(_y), 0 ));


        Vector3 v00 = rootChunk.normals[x0 + (rootChunk.ChunkSize + 1) * y0];
        Vector3 v10 = rootChunk.normals[(x0 + 1) + (rootChunk.ChunkSize + 1) * y0];
        Vector3 v01 = rootChunk.normals[x0 + (rootChunk.ChunkSize + 1) * (y0 + 1)];
        Vector3 v11 = rootChunk.normals[(x0 + 1) + (rootChunk.ChunkSize + 1) * (y0 + 1)];
        

        float wx = _x - x0;
        float wy = _y - y0;
        
        Vector3 sampledNormal = (1-wy)*(1-wx)*v00 + (1-wy)*(wx)*v10 + (wy)*(1-wx)*v01 + (wy)*(wx)*v11;
        sampledNormal = sampledNormal.normalized;

        return sampledNormal;
    }

    //interpolate the terrain atlas UV at the (x, y) fractional coordinates (0 < x,y < 1)
    public Vector3 SampleAtlasUV(float x, float y)
    {
        int subChunkLength = MathMisc.TwoPowX(level);

        float _x = x * ChunkSize * subChunkLength;
        float _y = y * ChunkSize * subChunkLength;

        int x0 = Mathf.Min(rootChunk.ChunkSize - 1, Mathf.Max(  Mathf.FloorToInt(_x), 0 ));
        int y0 =  Mathf.Min(rootChunk.ChunkSize - 1, Mathf.Max(  Mathf.FloorToInt(_y), 0 ));


        Vector2 v00 = rootChunk.atlasUVs[x0 + (rootChunk.ChunkSize + 1) * y0];
        Vector2 v10 = rootChunk.atlasUVs[(x0 + 1) + (rootChunk.ChunkSize + 1) * y0];
        Vector2 v01 = rootChunk.atlasUVs[x0 + (rootChunk.ChunkSize + 1) * (y0 + 1)];
        Vector2 v11 = rootChunk.atlasUVs[(x0 + 1) + (rootChunk.ChunkSize + 1) * (y0 + 1)];
        

        float wx = _x - x0;
        float wy = _y - y0;
        
        Vector2 sampledAUV = (1-wy)*(1-wx)*v00 + (1-wy)*(wx)*v10 + (wy)*(1-wx)*v01 + (wy)*(wx)*v11;

        return sampledAUV;
    }

    public virtual bool IsReady()
    {
        if (rootChunk != null)
        {
            return rootChunk.isReady;
        }
        return isReady;
    }
    
    public Vector2Int WorldToSubChunkCoords(Vector2 wPosition, int level)
    {
        Vector2 relativePos = wPosition - WorldPosition + Vector2.one * ChunkSize * Scale * 0.5f; 
        Vector2Int subChunkPos = Vector2Int.zero;

        int subChunkSize = ChunkSize/MathMisc.TwoPowX(level);

        subChunkPos.x = Mathf.FloorToInt((relativePos.x/subChunkSize));
        subChunkPos.y = Mathf.FloorToInt((relativePos.y/subChunkSize));
        return subChunkPos;
    }
}







public class TerrainChunk : QuadChunk
{
    private GameObject chunkObject; 
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;


    public TerrainChunk(Vector2Int position, ChunkGenerationThreadData mapData, Transform parent, Material material, ChunkThreadManager threadManager) 
    : base(Vector2Int.zero, mapData.chunkSize, mapData.chunkScale)
    {
        this.Position = position;
        this.WorldPosition = ((Vector2)position) * ChunkSize * Scale;
        this.Bounds = new Bounds(WorldPosition, Vector2.one * ChunkSize * Scale);

        chunkObject = new GameObject("Terrain Chunk");
        chunkObject.layer = 6; //terrain layer

        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshCollider = chunkObject.AddComponent<MeshCollider>();
        meshCollider.cookingOptions = MeshColliderCookingOptions.UseFastMidphase;
        meshRenderer.material = material; 

        Vector3 worldPositionV3 = new Vector3(WorldPosition.x, 0, WorldPosition.y);

        chunkObject.transform.position = worldPositionV3;
        chunkObject.transform.parent = parent;
        
        threadManager.RequestChunkData(mapData, OnChunkDataReceived);
    }
    
    private void OnChunkDataReceived(ChunkData chunkData)  
    {                  
        isReady = true;                         
        meshFilter.mesh = chunkData.meshData.CreateMesh();
        vertices = chunkData.meshData.vertices;
        normals = meshFilter.mesh.normals;
        atlasUVs = chunkData.meshData.atlasUvs;

        //only set chunk collider for the closest subchunks to the player. Never set it like this
        meshCollider.sharedMesh = chunkData.colliderData.CreateMesh(skipNormals:true);

        this.dataTree = chunkData.dataTree;
        this.biomeMap = chunkData.biomeMap;
    }

    public void Update(Vector2 viewerWorldPos, float maxViewDistance) 
    {
        //viewer distance from the edge of the chunk:
        float viewerDistance = GetBoundsDistance(viewerWorldPos); 
        
        bool visible = viewerDistance <= maxViewDistance * Scale;
        SetVisible(visible); 
        //Only destroy the chunks that are actualy very far (probably wont be rendered)
    }

    public float GetBoundsDistance(Vector2 viewerWorldPos)
    {
        return Mathf.Sqrt(Bounds.SqrDistance(viewerWorldPos));
    }

    public void SetVisible(bool visible)
    {
        chunkObject.SetActive(visible);
        if (!visible)
        {
            //BUGGED NULL REF
            //Array.Clear(vertices, 0, vertices.Length);
            //Array.Clear(normals, 0, normals.Length);
            //Array.Clear(atlasUVs, 0, atlasUVs.Length);
            vertices = null;
            normals = null;
            atlasUVs = null;
        }
        else if (isReady && vertices == null)
        {
            vertices = meshFilter.mesh.vertices;
            normals = meshFilter.mesh.normals;
            List<Vector2> auvs = new List<Vector2>();
            meshFilter.mesh.GetUVs(3, auvs); 
            atlasUVs = auvs.ToArray();
        }
    }

    public bool IsVisible()
    {
        return chunkObject.activeSelf;
    }

    public override Vector3[] GetVertices()
    {
        if (vertices == null)
        {
            vertices = meshFilter.mesh.vertices;
        }
        
        return vertices;
    }

    public override Vector3[] GetNormals()
    {
        if (normals == null)
        {
            normals = meshFilter.mesh.normals;
        }
        
        return normals;
    }

    public override Vector2[] GetAtlasUVs()
    {
        if (atlasUVs == null)
        {
            atlasUVs = meshFilter.mesh.uv3;
            //meshFilter.mesh.GetUVs(3, atlasUVs);
        }
        
        return atlasUVs;
    }
}
