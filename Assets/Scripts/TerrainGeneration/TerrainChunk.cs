using System.Collections;
using System.Collections.Generic;
using UnityEngine;



//each node must have the data necessary for a subchunk to generate all vertex positions belonging to that node
public class ChunkTreeNode
{
    public int depth;

    //if the node has no children, it means it will be a totaly free node (can be used to place details)
    public ChunkTreeNode[] children = new ChunkTreeNode[4];

    public ChunkTreeNode()
    {

    }

    public bool IsEmpty()
    {
        return true;
    }

    //returns a list containing fractional positions of all vertices belonging to the children of this node at a 
    //certain relative depth. A relative depth of 0 gives the vertices of the current node
    public List<Vector2> GetPositions(int relatDepth)
    {
        return null;
    }

}

public class ChunkDataTree // Quadtree to store the chunk objects information as well as all other relevant data
{
    private ChunkTreeNode head;

    //private terrain objects list (a list containing all objects on this tree. they should all be parented to the)
    //main chunk
    //the tree data is only deleted when the terrain chunk is deleted, so it will never generate duplicated objects
    

    public ChunkDataTree(ChunkTreeNode head = null)
    {
        if (head != null)
        {
            this.head = head;
        }
        else
        {
            this.head = new ChunkTreeNode();
        }
    }


    //Save the data tree to a save file as well;
    //Only the relevant data (modifications done by the player)

    //Get a data tree that starts at a particular node from this tree
    public ChunkDataTree GetSubTree(int index)
    {
        return null;
        //return new ChunkDataTree();
    }

    public void AddObject(Vector3 relativePosition)
    {

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


    public List<ChunkTreeNode> GetFreeNodes()
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

    public QuadChunk()
    {

    }


    //gets a subchunk based on world position of this chunk and the provided world position 
    //the subchunk will contain data interpolated from the current chunk's data
    public SubChunk GetSubChunk(Vector2 worldPos, int subdivision)
    {
        Vector2Int subChunkPos = WorldToSubChunkCoords(worldPos, subdivision);
        int subChunkSize = chunkSize/MathMisc.TwoPowX(subdivision);

        return new SubChunk(subChunkPos, subChunkSize, this);
    }
    public SubChunk GetSubChunk(Vector3 worldPos, int subdivision)
    {
        Vector2Int subChunkPos = WorldToSubChunkCoords(worldPos, subdivision);
        int subChunkSize = chunkSize/MathMisc.TwoPowX(subdivision);

        return new SubChunk(subChunkPos, subChunkSize, this);
    }


    public SubChunk GetSubChunkFromGlobalPos(Vector2Int globalPos, int subdivision)
    {
        
        int subChunkSize = chunkSize/MathMisc.TwoPowX(subdivision);

        Vector2 worldPos = (globalPos - (MathMisc.TwoPowX(subdivision - 1) - 0.5f) *  Vector2.one) * subChunkSize;
        
        Vector2Int subChunkPos = WorldToSubChunkCoords(worldPos, subdivision);
    
        return new SubChunk(subChunkPos, subChunkSize, this);
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
    private Vector2Int position;

    //world position of this chunk relative to the center(origin) of the parent chunk
    private Vector2 relativePos;

    public SubChunk(Vector2Int position, int size, QuadChunk parentChunk)
    {
        this.parentChunk = parentChunk;
        this.position = position;
        this.chunkSize = size;

        int parentChunkSize = parentChunk.GetSize();

        this.scale = parentChunk.GetScale();

        this.relativePos = ((Vector2)position + Vector2.one * 0.5f ) * size * scale 
                            - Vector2.one * parentChunkSize * scale/2;

        this.worldPosition = relativePos + parentChunk.GetWorldPosition();

        this.bounds = new Bounds(new Vector3(worldPosition.x,0,worldPosition.y), 
                                    new Vector3(1,50,1) * chunkSize * scale);
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
        Vector3 relativePos3D = new Vector3(relativePos.x, 0, relativePos.y);

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


    public override bool IsReady()
    {
        return parentChunk.IsReady();
    }

    public static Vector2 GlobalSubChunkToWorldCoords(Vector2 gSubChunkCoords, int subdivision, int subChunkSize)
    {
        return (gSubChunkCoords - (MathMisc.TwoPowX(subdivision - 1) - 0.5f) *  Vector2.one) * subChunkSize;
    }

    //Interpolate vertex positions based on the input fractional coordinates 
    public Vector3 SamplePosition(Vector2 fracPos)
    {
        //vertices start at position * chunkSize and end at (position + Vector2Int.one) * chunkSize
        //the input position will be translated to chunk position by doing: 
        //chunkPos = (position + fracPos) * 30 (as long as fracPos is between 0 and 1)
        //that can be converted into index by doing:
        //index = x + (parentChunk.GetSize() + 1) * y
        return Vector3.zero;
    }

    //Interpolate the terrain normal based on the input fractional coordinates 
    public Vector3 SampleNormal(Vector2 fracPos)
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
        threadManager.RequestMeshData(mapData, OnMeshDataReceived);
    }



    
    private void OnMeshDataReceived(MeshData meshData, MeshData colliderData)  
    {                  
        //chunkMesh = meshData.CreateMesh();
        isReady = true;                         
        meshFilter.mesh = meshData.CreateMesh();
        meshCollider.sharedMesh = colliderData.CreateMesh(skipNormals:true);
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


