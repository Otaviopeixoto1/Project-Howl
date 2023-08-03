using System.Collections;
using System.Collections.Generic;
using UnityEngine;



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
        //return GetObjectsCount() == 0;
        return objects.Count == 0;
    }
    /*
    public bool IsFilled()
    {
        //if the entire node is filled, we wont need to store information on its children
        //so the children list will be null
        return (children == null) && !IsEmpty();
    }*/

}

public class ChunkDataTree // Quadtree to store the chunk objects information as well as all other relevant data
{
    public QuadNode head;
    
    //All the subchunk nodes that are created when the tree is generated
    //private QuadNode[] subChunkNodes;

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



    //Save the data tree to a save file as well;
    //Only the relevant data (modifications done by the player)

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


public class QuadChunk
{
    protected List<Vector3> vertices;
    protected int chunkSize;
    protected float scale;
    protected Vector2 worldPosition;
    protected Bounds bounds;
    protected bool isReady = false;

    protected ChunkDataTree dataTree;
    protected Biomes[] biomeMap;


    //gets a subchunk based on world position of this chunk and the provided world position 
    //the subchunk will contain data interpolated from the current chunk's data
    public SubChunk GetSubChunk(Vector2 worldPos, int subdivision)
    {
        Vector2Int subChunkPos = WorldToSubChunkCoords(worldPos, subdivision);
        int subChunkSize = chunkSize/MathMisc.TwoPowX(subdivision);


        return new SubChunk(subChunkPos, subChunkSize, subdivision, this);
    }
    public SubChunk GetSubChunk(Vector3 worldPos, int subdivision)
    {
        Vector2Int subChunkPos = WorldToSubChunkCoords(worldPos, subdivision);
        int subChunkSize = chunkSize/MathMisc.TwoPowX(subdivision);


        return new SubChunk(subChunkPos, subChunkSize, subdivision, this);
    }

    /*
    public SubChunk GetSubChunkFromGlobalPos(Vector2Int globalPos, int subdivision)
    {
        
        int subChunkSize = chunkSize/MathMisc.TwoPowX(subdivision);

        Vector2 worldPos = (globalPos - (MathMisc.TwoPowX(subdivision - 1) - 0.5f) *  Vector2.one) * subChunkSize;
        
        Vector2Int subChunkPos = WorldToSubChunkCoords(worldPos, subdivision);
    
        return new SubChunk(subChunkPos, subChunkSize, subdivision, this);
    }*/



    public ChunkDataTree GetSubDataTree(Vector2Int subChunkPos, int subdivision)
    {
        Vector2Int startPos = subChunkPos/MathMisc.TwoPowX(subdivision - 1);
        int startIndex = startPos.x + 2 * startPos.y;

        //Debug.Log(startIndex);
        QuadNode currentNode = dataTree.head.children[startIndex];
        //this is the index of the first node on the tree (a child of the head node)

        for (int i = 1; i <= subdivision - 1; i++)
        {
            Vector2Int newPos = subChunkPos/MathMisc.TwoPowX(subdivision - 1 - i);
            int newIndex = (newPos.x - 2 * startPos.x) + 2 * (newPos.y - 2 * startPos.y);
            //Debug.Log(newIndex);
            startPos = newPos;

            currentNode = currentNode.children[newIndex];

        }

        return new ChunkDataTree(currentNode);
    }



    public virtual Biomes[] GetBiomeMap()
    {
        return biomeMap;
    }

    public virtual ChunkDataTree GetDataTree()
    {
        return dataTree;
    }

    
    public virtual List<Vector3> GetVertices()
    {
        return vertices;
    }

    public virtual bool IsReady()
    {
        return isReady;
    }

    public int GetSize()
    {
        return chunkSize;
    }
    public float GetScale()
    {
        return scale;
    }

    public Vector2 GetWorldPosition()
    {
        return worldPosition;
    }

    public Bounds GetBounds()
    {
        return bounds;
    }

    public Vector2Int WorldToSubChunkCoords(Vector2 wPosition, int subdivision)
    {
        Vector2 relativePos = wPosition - worldPosition + Vector2.one * chunkSize * scale/2; 
        Vector2Int subChunkPos = Vector2Int.zero;

        int subChunkSize = chunkSize/MathMisc.TwoPowX(subdivision);

        subChunkPos.x = Mathf.FloorToInt((relativePos.x/subChunkSize));
        subChunkPos.y = Mathf.FloorToInt((relativePos.y/subChunkSize));
        return subChunkPos;
    }

    public Vector2Int WorldToSubChunkCoords(Vector3 wPosition, int subdivision)
    {
        Vector2 relativePos = new Vector2(wPosition.x, wPosition.z) - worldPosition + Vector2.one * chunkSize * scale/2; 
        Vector2Int subChunkPos = Vector2Int.zero;

        int subChunkSize = chunkSize/MathMisc.TwoPowX(subdivision);

        subChunkPos.x = Mathf.FloorToInt((relativePos.x/subChunkSize));
        subChunkPos.y = Mathf.FloorToInt((relativePos.y/subChunkSize));
        return subChunkPos;
    }

    public Vector2Int WorldToGlobalSubChunkCoords(Vector2 wPosition, int subdivision)
    {
        Vector2 position = wPosition + Vector2.one * chunkSize * scale/2; 
        Vector2Int subChunkPos = Vector2Int.zero;

        int subChunkSize = chunkSize/MathMisc.TwoPowX(subdivision);

        subChunkPos.x = Mathf.FloorToInt((position.x/subChunkSize));
        subChunkPos.y = Mathf.FloorToInt((position.y/subChunkSize));
        return subChunkPos;
    }

    public Vector2Int WorldToGlobalSubChunkCoords(Vector3 wPosition, int subdivision)
    {
        Vector2 position = new Vector2(wPosition.x, wPosition.z) + Vector2.one * chunkSize * scale/2; 
        Vector2Int subChunkPos = Vector2Int.zero;

        int subChunkSize = chunkSize/MathMisc.TwoPowX(subdivision);

        subChunkPos.x = Mathf.FloorToInt((position.x/subChunkSize));
        subChunkPos.y = Mathf.FloorToInt((position.y/subChunkSize));
        return subChunkPos;
    }

    public static Vector2Int GlobalSubChunkToChunkCoords(Vector2Int globalSubChunkCoords, int subdivision)
    {
        Vector2Int chunkCoords = globalSubChunkCoords / MathMisc.TwoPowX(subdivision);

        return chunkCoords;
    }
}



//subchunks will contain data for interpolating and generating dense point clouds for specific terrain sections
public class SubChunk : QuadChunk
{
    private QuadChunk parentChunk;

    //the position of this subchunk inside the main chunk:
    private Vector2Int position; // 0 < x,y < 8

    //world position of this chunk relative to the center(origin) of the parent chunk
    private Vector3 relativePos3D;

    private int subdivision;


    public SubChunk(Vector2Int position, int size, int subdivision, QuadChunk parentChunk)
    {
        this.subdivision = subdivision;
        this.parentChunk = parentChunk;
        this.position = position;
        this.chunkSize = size;


        int parentChunkSize = parentChunk.GetSize();

        this.scale = parentChunk.GetScale();

        Vector2 relativePos = ((Vector2)position + Vector2.one * 0.5f ) * size * scale 
                            - Vector2.one * parentChunkSize * scale/2;

        this.relativePos3D = new Vector3(relativePos.x, 0, relativePos.y);

        this.worldPosition = relativePos + parentChunk.GetWorldPosition();

        this.bounds = new Bounds(new Vector3(worldPosition.x, 0, worldPosition.y), 
                                    new Vector3(1,50,1) * chunkSize * scale);
    }
    public override ChunkDataTree GetDataTree()
    {
        if (dataTree == null)
        {
            dataTree = parentChunk.GetSubDataTree(position, subdivision);
            return dataTree;
        }
        else
        {
            return dataTree;
        }
    }

    public override List<Vector3> GetVertices()
    {
        if (vertices != null)
        {
            return vertices;
        }
        
        vertices = new List<Vector3>();
        List<Vector3> mainVertices = parentChunk.GetVertices();
        //VERTEX POSITIONS ARE GIVEN RELATIVE TO THE PARENT CHUNK ORIGIN
        //they have to be converted to be relative to the subchunk origin:

        for (int y = position.y * chunkSize; y < (position.y + 1) * chunkSize; y++)
        {
            for (int x = position.x * chunkSize; x < (position.x + 1) * chunkSize; x++)
            {
                Vector3 pos = mainVertices[x + (parentChunk.GetSize() + 1) * y] - relativePos3D;
                this.vertices.Add(pos);
            }
        }
        return vertices;
    }

    public override Biomes[] GetBiomeMap()
    {
        if (!IsReady())
        {
            return null;
        }

        Biomes[] biomeMap = parentChunk.GetBiomeMap();
        Biomes[] subBiomeMap = new Biomes[4];

        int subChunkCount = MathMisc.TwoPowX(subdivision);

        for (int j = 0; j < 2; j++)
        {
            for (int i = 0; i < 2; i++)
            { 
                int index = (position.x + i) + (subChunkCount + 1) * (position.y + j);
                subBiomeMap[i + 2 * j] = biomeMap[index];
            }
        }
        return subBiomeMap;
    }

    public List<TerrainDetailSettings> GetDetailsSettings(Dictionary<Biomes, TerrainDetailSettings> biomeDetails)
    {
        if (!IsReady())
        {
            return null;
        }

        List<TerrainDetailSettings> details = new List<TerrainDetailSettings>();
        Biomes[] biomeMap = parentChunk.GetBiomeMap();

        int subChunkCount = MathMisc.TwoPowX(subdivision);

        //Debug.Log(position);

        for (int j = 0; j < 2; j++)
        {
            for (int i = 0; i < 2; i++)
            { 
                int index = (position.x + i) + (subChunkCount + 1 ) * (position.y + j);
                if (index < 0 || index >= biomeMap.Length)
                {
                    return details;
                }
                //Debug.Log(position.x + i + ", " + (position.y + j) + ": " + biomeMap[index] );
                TerrainDetailSettings detailSettings = biomeDetails[biomeMap[index]];
                //Debug.Log(biomeMap[index]);

                if (detailSettings != null && !details.Contains(detailSettings))
                {
                    details.Add(detailSettings);
                }
            }
        }


        return details;
    }


    public override bool IsReady()
    {
        return parentChunk.IsReady();
    }

    public static Vector2 GlobalSubChunkToWorldCoords(Vector2 gSubChunkCoords, int subdivision, int subChunkSize)
    {
        return (gSubChunkCoords - (MathMisc.TwoPowX(subdivision - 1) - 0.5f) *  Vector2.one) * subChunkSize;
    }

    //Interpolate vertex positions based on the input fractional coordinates 
    public Vector3 SamplePosition(float x, float y)
    {
        //vertices start at position * chunkSize and end at (position + Vector2Int.one) * chunkSize
        //the input position will be translated to chunk position by doing: 
        //chunkPos = (position + fracPos) * 30 (as long as fracPos is between 0 and 1)

        List<Vector3> chunkVertices = parentChunk.GetVertices();

        int subChunkLength = MathMisc.TwoPowX(subdivision);

        float _x = ( x) * chunkSize * subChunkLength;
        float _y = ( y) * chunkSize * subChunkLength;

        int x0 = Mathf.FloorToInt(_x);
        int y0 = Mathf.FloorToInt(_y);


        Vector3 v00 = chunkVertices[x0 + (parentChunk.GetSize() + 1) * y0];
        Vector3 v10 = chunkVertices[(x0 + 1) + (parentChunk.GetSize() + 1) * y0];
        Vector3 v01 = chunkVertices[x0 + (parentChunk.GetSize() + 1) * (y0 + 1)];
        Vector3 v11 = chunkVertices[(x0 + 1) + (parentChunk.GetSize() + 1) * (y0 + 1)];
        

        float wx = _x - x0;
        float wy = _y - y0;
        
        return (1-wy)*(1-wx)*v00 + (1-wy)*(wx)*v10 + (wy)*(1-wx)*v01 + (wy)*(wx)*v11 - relativePos3D;
    }

    //Interpolate the terrain normal based on the input fractional coordinates 
    public Vector3 SampleNormal(float x, float y)
    {
        return Vector3.zero;
    }


}




public class TerrainChunk : QuadChunk
{
    private GameObject chunkObject; 
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    

    public TerrainChunk(Vector2Int position, ChunkGenerationThreadData mapData, Transform parent, Material material, ChunkThreadManager threadManager)
    {
        this.chunkSize = mapData.chunkSize;
        this.scale = mapData.chunkScale;

        worldPosition = ((Vector2)position) * chunkSize * scale;
        bounds = new Bounds(worldPosition, Vector2.one * chunkSize * scale);

        chunkObject = new GameObject("Terrain Chunk");
        chunkObject.layer = 6; //terrain layer

        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshCollider = chunkObject.AddComponent<MeshCollider>();
        meshCollider.cookingOptions = MeshColliderCookingOptions.UseFastMidphase;
        meshRenderer.material = material; 

        //debugTexture = CreateDebugTexture(mapData.sampler);

        Vector3 worldPositionV3 = new Vector3(worldPosition.x, 0, worldPosition.y);

        chunkObject.transform.position = worldPositionV3;
        chunkObject.transform.parent = parent;

        //SetVisible(false);
        

        //RequestChunkData* 
        threadManager.RequestChunkData(mapData, OnChunkDataReceived);
    }



    
    private void OnChunkDataReceived(ChunkData chunkData)  
    {                  
        isReady = true;                         
        meshFilter.mesh = chunkData.meshData.CreateMesh();
        meshCollider.sharedMesh = chunkData.colliderData.CreateMesh(skipNormals:true);

        this.dataTree = chunkData.dataTree;
        this.biomeMap = chunkData.biomeMap;

        //meshRenderer.material.SetTexture("_BaseMap", debugTexture);
    }

    public void Update(Vector2 viewerWorldPos, float maxViewDistance) 
    {
        //viewer distance from the edge of the chunk:
        float viewerDistance = GetBoundsDistance(viewerWorldPos); 
        
        bool visible = viewerDistance <= maxViewDistance * scale;
        SetVisible(visible); 
        //Only destroy the chunks that are actualy very far (probably wont be rendered)
    }

    public float GetBoundsDistance(Vector2 viewerWorldPos)
    {
        return Mathf.Sqrt(bounds.SqrDistance(viewerWorldPos));
    }


    public void SetVisible(bool visible)
    {
        chunkObject.SetActive(visible);
        if (!visible)
        {
            vertices?.Clear();
            vertices = null;
        }
    }

    public bool IsVisible()
    {
        return chunkObject.activeSelf;
    }

    public override List<Vector3> GetVertices()
    {
        if (vertices == null)
        {
            vertices = new List<Vector3>();
            meshFilter.mesh.GetVertices(vertices);
        }
        
        return vertices;
    }

    




    public Texture2D CreateDebugTexture(WorldGenerator sampler)
    {
        Texture2D tex = new Texture2D(chunkSize + 1, chunkSize + 1);
        Color[] colors = new Color[(chunkSize + 1) * (chunkSize + 1)];

        for (int y = 0; y <= chunkSize; y += 1)
        {
            for (int x = 0; x <= chunkSize; x += 1)
            {
                colors[x + y * (chunkSize + 1)] = sampler.GetColor((x + worldPosition.x)/scale, (y + worldPosition.y)/scale);
            }
        }
        tex.SetPixels(colors);
        tex.Apply();

        return tex;
    }

}


