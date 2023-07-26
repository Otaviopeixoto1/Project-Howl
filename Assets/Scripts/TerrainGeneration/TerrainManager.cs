using System.Collections;
using System.Collections.Generic;
using System;
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






[RequireComponent(typeof(WorldManager))]
public class TerrainManager : MonoBehaviour
{  
    private Dictionary<Vector2Int, TerrainChunk> terrainChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private List<TerrainChunk> visibleChunks = new List<TerrainChunk>();
    private ChunkThreadManager chunkThreadManager;

    private TerrainObjectsManager terrainObjectsManager;


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
    private Material terrainMaterial;
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





/////////////////////- JUST FOR TESTING -///////////////////////////
    [SerializeField]
    private Material detailTestMaterial;
////////////////////////////////////////////////////////////////////





    void Awake()
    {
        chunkThreadManager = new ChunkThreadManager();
        worldManager = GetComponent<WorldManager>();
    }

    void Start()
    {   
        
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
        terrainObjectsManager.ClearObjects();
    }


    private void FirstChunkUpdate()
    {
        worldSampler = worldManager.GetWorldSampler();
        terrainMaterial.SetFloat("_atlasScale", 1/worldSampler.GetBiomeMapScale());



        viewerWorldPos = new Vector2(viewer.position.x, viewer.position.z);

        

        Vector2Int viewerChunkCoords = WorldToChunkCoords(viewerWorldPos);
        
        UpdateVisibleChunks(viewerChunkCoords.x, viewerChunkCoords.y);
        
        terrainObjectsManager = new TerrainObjectsManager(this, worldSampler, detailTestMaterial);


        WorldManager.OnSuccessfulLoad -= FirstChunkUpdate;
    }



    void Update()
    {
        viewerWorldPos.x = viewer.position.x;
        viewerWorldPos.y = viewer.position.z;

        Vector2Int viewerChunkCoords = WorldToChunkCoords(viewerWorldPos);

        //for the current case, it doesnt make sense to update the chunks unless the chunk coordinate has changed
        // (the player moved from one chunk to another)
        //update the current chunks only if the player has changed from one chunk to another 
        //(its a topdown view). the condition could also be changed (even dynamicaly)

        if (Vector3.Distance(viewerWorldPos, lastViewerPos) > thresholdForMeshUpdate * chunkSize * chunkScale)
        {
            lastViewerPos = viewerWorldPos;
            UpdateVisibleChunks(viewerChunkCoords.x, viewerChunkCoords.y);
        }
        
        //Update all terrain details
        terrainObjectsManager.UpdateObjectChunks(viewerWorldPos, terrainChunks);
        
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
                    terrainChunks.Add(viewChunkCoord, new TerrainChunk(viewChunkCoord, mapData, this.transform, terrainMaterial, chunkThreadManager));
                    //all chunks start as visible
                    visibleChunks.Add(terrainChunks[viewChunkCoord]);
                }


            }
        }
    }




    public TerrainChunk GetCurrentChunk()
    {
        Vector2Int viewerChunkCoords = WorldToChunkCoords(viewer.position);
        return terrainChunks[viewerChunkCoords];
    }

    public SubChunk GetCurrentSubChunk(int subdivision)
    {
        Vector2Int viewerChunkCoords = WorldToChunkCoords(viewer.position);
        return terrainChunks[viewerChunkCoords].GetSubChunk(viewer.position, subdivision);
    }


    public List<TerrainChunk> GetVisibleChunks()
    {
        return visibleChunks;
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
