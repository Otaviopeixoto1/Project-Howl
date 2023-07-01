using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using UnityEngine;


/// <summary>
/// Struct used to pass the world sampler and other data to the chunk generation threads
/// </summary>
public struct SamplerThreadData
{
    public readonly WorldSampler sampler;
    public readonly Vector2 chunkPosition;
    public readonly int chunkSize;
    public readonly float chunkScale;
    public readonly int meshLodBias;
    public readonly int colliderLodBias;

    public SamplerThreadData (WorldSampler sampler,Vector2 gridPosition, int chunkSize, float chunkScale, int meshLodBias, int colliderLodBias)
    {
        this.sampler = sampler;
        this.chunkPosition = gridPosition * chunkSize;
        this.chunkSize = chunkSize;
        this.chunkScale = chunkScale;
        this.meshLodBias = meshLodBias;
        this.colliderLodBias = colliderLodBias;
    }
}



public class TerrainChunk
{
    GameObject chunkObject; // all of these variables should be readonly
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    int chunkSize; //size refers to the mesh size (number of mesh vertices along x and y - 1)
    float scale;
    Vector2Int position;
    Vector2 worldPosition;
    Bounds bounds;

    Texture2D debugTexture;

    

    public TerrainChunk(Vector2Int coord, SamplerThreadData mapData, Transform parent, Material material, ChunkThreadManager threadManager)
    {
        this.chunkSize = mapData.chunkSize;
        this.scale = mapData.chunkScale;

        worldPosition = ((Vector2)coord) * chunkSize * scale;
        bounds = new Bounds(worldPosition, Vector2.one * chunkSize * scale);

        chunkObject = new GameObject("Terrain Chunk");
        chunkObject.layer = 6;
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

        threadManager.RequestMeshData(mapData, OnMeshDataReceived);
    }



    
    private void OnMeshDataReceived(MeshData meshData, MeshData colliderData)  
    {                                           
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







[RequireComponent(typeof(WorldSampler))]
public class TerrainManager : MonoBehaviour
{  
    private Dictionary<Vector2Int, TerrainChunk> terrainChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private List<TerrainChunk> visibleChunks = new List<TerrainChunk>();
    private ChunkThreadManager chunkThreadManager;



    private WorldSampler worldSampler;
    private WorldManager worldManager;


    [SerializeField]
    private Transform viewer;
    [HideInInspector]
    public Vector2 viewerWorldPos;
    private Vector2 lastViewerPos;

    [SerializeField]
    [Tooltip("distance (in normalized units) the viewer has to move before update")]
    private float thresholdForMeshUpdate = 0.1f; //distance the viewer has to move before update in normalized units 
                                                  //actual distance is thresholdForChunkUpdate * chunkSize * scale
                                                  //also add:
                                                  //THE DISTANCE BETWEEN THE viewer AND THE EDGE OF THE CHUNK
    [SerializeField]
    private float thresholdForColliderUpdate = 0.1f; //distance the viewer and the chunk edge before updating nearby colliders
                                                  

    [SerializeField]
    private Material testMaterial;
    [SerializeField]
    [Range(10,240)]
    private int chunkSize = 240;
    [SerializeField]
    private float chunkScale = 1f;
    [SerializeField]
    private int chunkMeshLodBias = 0;
    [SerializeField]
    private int chunkColliderLodBias = 6;
    [SerializeField]
    private float normalizedViewDist = 1.2f;

    void Awake()
    {
        chunkThreadManager = new ChunkThreadManager();
        worldSampler = GetComponent<WorldSampler>();
        worldManager = GetComponent<WorldManager>();
    }

    void Start()
    {   
        //Assuming that biomeMapSize = chunkSize (= 240 currently) 
        testMaterial.SetFloat("_atlasScale", 1/worldSampler.GetBiomeMapScale() - 1);
    }

    void OnValidate()
    {   
        if (chunkScale < 0.01f)
        {
            chunkScale = 0.01f;
        }
        if (thresholdForMeshUpdate < 0.01f)
        {
            thresholdForMeshUpdate = 0.01f;
        }

    }
    void OnEnable()
    {
        WorldManager.OnSuccessfulLoad += FirstChunkUpdate;
    }


    void OnDisable()
    {
        WorldManager.OnSuccessfulLoad -= FirstChunkUpdate;
    }


    private void FirstChunkUpdate()
    {
        //assign the world map atlas texture to the material
        viewerWorldPos = new Vector2(viewer.position.x, viewer.position.z);

        Vector2Int chunkCoords = WorldToChunkCoords(viewerWorldPos);
        
        UpdateVisibleChunks(chunkCoords.x, chunkCoords.y);
        WorldManager.OnSuccessfulLoad -= FirstChunkUpdate;
    }



    void Update()
    {
        viewerWorldPos = new Vector2(viewer.position.x, viewer.position.z);


        Vector2Int chunkCoords = WorldToChunkCoords(viewerWorldPos);

        //for the current case, it doesnt make sense to update the chunks unless the chunk coordinate has changed
        // (the player moved from one chunk to another)
        //update the current chunks only if the player has changed from one chunk to another 
        //(its a topdown view). the condition could also be changed (even dynamicaly)

        if (Vector3.Distance(viewerWorldPos, lastViewerPos) > thresholdForMeshUpdate * chunkSize * chunkScale)
        {
            lastViewerPos = viewerWorldPos;
            UpdateVisibleChunks(chunkCoords.x, chunkCoords.y);
        }
        
        
        
        chunkThreadManager.CheckThreads();
    }


    public void UpdateVisibleChunks(int currentChunkX, int currentChunkY) 
    {
        //All visible chunks end on this list. here we update all of them to check if they are still visible
        for (int i = visibleChunks.Count - 1; i >= 0; i--)
        {
            visibleChunks[i].Update(viewerWorldPos, chunkSize * normalizedViewDist);
            if (!visibleChunks[i].IsVisible())
            {
                //chunks that just turned invisible are removed from visibleChunks
                visibleChunks.RemoveAt(i);
            }
        }

        int chunkVisibilityRadius = Mathf.FloorToInt(normalizedViewDist);

        //Looping through all chunks that should be visible in this frame
        for (int dy = -chunkVisibilityRadius; dy <= chunkVisibilityRadius; dy++)
        {
            for (int dx = -chunkVisibilityRadius; dx <= chunkVisibilityRadius; dx++)
            {
                Vector2Int viewChunkCoord = new Vector2Int(currentChunkX + dx, currentChunkY + dy);

                if (terrainChunks.ContainsKey(viewChunkCoord))
                {
                    if(!terrainChunks[viewChunkCoord].IsVisible())
                    {
                        terrainChunks[viewChunkCoord].SetVisible(true);
                        //chunks that turn visible should also be added back to the visibleChunks list
                        visibleChunks.Add(terrainChunks[viewChunkCoord]);
                    }
                }
                else
                {
                    SamplerThreadData mapData = new SamplerThreadData(worldSampler, viewChunkCoord, chunkSize, chunkScale, chunkMeshLodBias, chunkColliderLodBias);
                    terrainChunks.Add(viewChunkCoord, new TerrainChunk(viewChunkCoord, mapData, this.transform, testMaterial, chunkThreadManager));
                    //all chunks start as visible
                    visibleChunks.Add(terrainChunks[viewChunkCoord]);
                }


            }
        }
    }

    //Currently NOT WORKING
    public Vector3 GetNormal(Vector2 worldPos)
    {
        if (worldSampler == null)
        {
            return Vector3.up;
        }
        Vector2Int gridCoords = WorldToGridCoords(worldPos);
        float u = worldSampler.SampleHeight(gridCoords.x,gridCoords.y + 1);
        float d = worldSampler.SampleHeight(gridCoords.x,gridCoords.y - 1);
        float l = worldSampler.SampleHeight(gridCoords.x - 1,gridCoords.y);
        float r = worldSampler.SampleHeight(gridCoords.x + 1,gridCoords.y);

        Vector3 t1 = new Vector3(0,u-d,1); 
        Vector3 t2 = new Vector3(1,r-l,0); 
        return (Vector3.Cross(t1,t2)).normalized;
    }

    //Currently NOT WORKING
    public Vector3 GetNormal(Vector3 worldPos)
    {
        if (worldSampler == null)
        {
            return Vector3.up;
        }
        Vector2Int gridCoords = WorldToGridCoords(worldPos);
        float u = worldSampler.SampleHeight(gridCoords.x,gridCoords.y + 1);
        float d = worldSampler.SampleHeight(gridCoords.x,gridCoords.y - 1);
        float l = worldSampler.SampleHeight(gridCoords.x - 1,gridCoords.y);
        float r = worldSampler.SampleHeight(gridCoords.x + 1,gridCoords.y);

        Vector3 t1 = new Vector3(0,u-d,1); 
        Vector3 t2 = new Vector3(1,r-l,0); 
        return (Vector3.Cross(t1,t2)).normalized;
    }


    public Vector2Int WorldToGridCoords(Vector2 worldPos)
    {
        int currentChunkX = Mathf.RoundToInt(worldPos.x/(chunkScale));
        int currentChunkY = Mathf.RoundToInt(worldPos.y/(chunkScale));
        return new Vector2Int(currentChunkX, currentChunkY);
    }
    public Vector2Int WorldToGridCoords(Vector3 worldPos)
    {
        int currentChunkX = Mathf.RoundToInt(worldPos.x/(chunkScale));
        int currentChunkY = Mathf.RoundToInt(worldPos.z/(chunkScale));
        return new Vector2Int(currentChunkX, currentChunkY);
    }
    public Vector2Int WorldToChunkCoords(Vector2 worldPos)
    {
        int currentChunkX = Mathf.RoundToInt(worldPos.x/(chunkSize * chunkScale));
        int currentChunkY = Mathf.RoundToInt(worldPos.y/(chunkSize * chunkScale));
        return new Vector2Int(currentChunkX, currentChunkY);
    }
    public Vector2Int WorldToChunkCoords(Vector3 worldPos)
    {
        int currentChunkX = Mathf.RoundToInt(worldPos.x/(chunkSize * chunkScale));
        int currentChunkY = Mathf.RoundToInt(worldPos.z/(chunkSize * chunkScale));
        return new Vector2Int(currentChunkX, currentChunkY);
    }

    //return unscaled chunk world coordinate
    public Vector2 SnapToChunkCoordinates(Vector3 worldPos)
    {
        Vector2Int chunkCoords = WorldToChunkCoords(worldPos);
        return chunkCoords * chunkSize;
    }


}
