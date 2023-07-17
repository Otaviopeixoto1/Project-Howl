using System.Collections;
using System.Collections.Generic;
using UnityEngine;


////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//the chunk will request all the information about objects during the threaded calculations
//in the thread we will generate the quad tree containing all necessary data 



//Grass can be added based of the subchunk information and we can cull it more easily
//each subchunk can be used to get the terrain data for grass.
//for denser grass, the subchunk vertex positions can be used to interpolate the chunk coordinates and normals

//Add a slow update function in a coroutine to check a non visible chunk array for distance, if the chunk is too far
//destroy it ! (Remember to remove it from the dictionary)

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



public class ChunkDataTree // Quadtree to store the chunk objects information as well as all other relevant data
{

    //Save the data tree to a save file as well
}

//subchunk will contain data for interpolating and generating dense point clouds for a specific terrain sections
public class SubChunk
{
    
    public SubChunk()
    {

    }
}


public class TerrainChunk
{
    private GameObject chunkObject; 
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private int chunkSize; //chunkSize refers to the number of mesh vertices -1 along x and y 
    private float scale;
    private Vector2Int position;
    private Vector2 worldPosition;
    private Bounds bounds;

    //private Texture2D debugTexture;

    

    public TerrainChunk(Vector2Int coord, SamplerThreadData mapData, Transform parent, Material material, ChunkThreadManager threadManager)
    {
        this.chunkSize = mapData.chunkSize;
        this.scale = mapData.chunkScale;

        worldPosition = ((Vector2)coord) * chunkSize * scale;
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
        

        //Request Chunk data !!!
        threadManager.RequestMeshData(mapData, OnMeshDataReceived);
    }



    
    private void OnMeshDataReceived(MeshData meshData, MeshData colliderData)  
    {                  
        //chunkMesh = meshData.CreateMesh();                         
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
    }

    public bool IsVisible()
    {
        return chunkObject.activeSelf;
    }

    public List<Vector3> GetVertices()
    {
        List<Vector3> vertices = new List<Vector3>();
        meshFilter.mesh.GetVertices(vertices);
        return vertices;
    }


    //this is called to add a new terrain object into this chunk
    public void AddTerrainObject()
    {

    }


    //gets a subchunk based on world position of this chunk and the provided world position 
    //the subchunk will contain data interpolated from the current chunk's data
    public SubChunk GetSubChunk(int subdivision, int size)
    {


        return new SubChunk();
    }




    public Texture2D CreateDebugTexture(WorldSampler sampler)
    {
        Texture2D tex = new Texture2D(chunkSize + 1, chunkSize + 1);
        Color[] colors = new Color[(chunkSize + 1) * (chunkSize + 1)];

        for (int y = 0; y <= chunkSize; y += 1)
        {
            for (int x = 0; x <= chunkSize; x += 1)
            {
                colors[x + y * (chunkSize + 1)] = sampler.SampleColor((x + worldPosition.x)/scale, (y + worldPosition.y)/scale);
            }
        }
        tex.SetPixels(colors);
        tex.Apply();

        return tex;
    }

}


