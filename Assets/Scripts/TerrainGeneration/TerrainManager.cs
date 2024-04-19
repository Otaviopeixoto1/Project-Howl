using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;


public class TerrainManager : MonoBehaviour
{  
    private Dictionary<Vector2Int, TerrainChunk> terrainChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private List<TerrainChunk> visibleChunks = new List<TerrainChunk>();
    private ChunkThreadManager chunkThreadManager;

    private TerrainObjectsManager terrainObjectsManager;

    private WorldGenerator worldGenerator;


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



    void Awake()
    {
        enabled = false;
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
    }


    void OnDisable()
    {
        terrainObjectsManager?.ClearObjects();
    }


    public void Setup( WorldGenerator worldGenerator, DetailGenerationSettings detailGenerationSettings)
    {
        this.chunkThreadManager = new ChunkThreadManager();
        this.worldGenerator = worldGenerator;
        this.viewerWorldPos = new Vector2(viewer.position.x, viewer.position.z);
        
        Vector2Int viewerChunkCoords = WorldToChunkCoords(viewerWorldPos);
        
        UpdateVisibleChunks(viewerChunkCoords);
        
        //automatically loading MapAtlas into the detail material
        Texture2D tex = null;
        byte[] texData;
        if (System.IO.File.Exists(Application.dataPath + WorldGenerator.atlasPath))
        {
            texData = System.IO.File.ReadAllBytes(Application.dataPath + WorldGenerator.atlasPath);
            tex = new Texture2D(2, 2); //texture dimensions are resized on load.
            tex.LoadImage(texData); 
        }

        this.terrainMaterial.SetTexture("_MainTex", tex);
        
        terrainObjectsManager = new TerrainObjectsManager(detailGenerationSettings, tex);

        enabled = true;
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
            UpdateVisibleChunks(viewerChunkCoords);
        }
        
        //Update all terrain details
        Vector2Int chunkPos = WorldToChunkCoords(viewerWorldPos);
        terrainObjectsManager.UpdateObjectChunks(chunkPos, viewerWorldPos, terrainChunks);
        
        chunkThreadManager.CheckThreads();
    }


    public void UpdateVisibleChunks(Vector2Int currentChunkPos) 
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

        //load chunks in 2x2 grid:
        // (1,0) (1.1)
        // (0,0) (1,0)
        //we still store them in the world coords into the visibleChunks dict but the chunks have to be checked differently
        
        // Use floorTOInt on viewerWorldPos and we get the bottom left chunk. Then just sum with the offsets to get the neighbors on the 2x2 grid

        for (int dy = -chunkVisibilityRadius; dy <= chunkVisibilityRadius; dy++)
        {
            for (int dx = -chunkVisibilityRadius; dx <= chunkVisibilityRadius; dx++)
            {
                
                Vector2Int viewChunkCoord = new Vector2Int(currentChunkPos.x + dx, currentChunkPos.y + dy);

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
                    ChunkGenerationThreadData chunkData = new ChunkGenerationThreadData(worldGenerator, viewChunkCoord, chunkSize, chunkScale, chunkMeshLodBias, chunkColliderLodBias);
                    terrainChunks.Add(viewChunkCoord, new TerrainChunk(viewChunkCoord, chunkData, this.transform, terrainMaterial, chunkThreadManager));
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

    public Vector2 SnapToChunk(Vector3 worldPos)
    {
        Vector2Int chunkCoords = WorldToChunkCoords(worldPos);
        return chunkCoords * chunkSize;
    }


}
