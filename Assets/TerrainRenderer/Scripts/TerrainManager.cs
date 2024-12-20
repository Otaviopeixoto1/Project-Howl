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
    [Range(0.01f,10)]
    [Tooltip("Radius of visibility for the chunks in normalized units (1/chunkSize)")]
    private float viewRadius = 1.2f;

    private static readonly float sqrt2 = (float)Math.Sqrt(2);



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
        terrainObjectsManager.UpdateObjectChunks(terrainChunks[viewerChunkCoords], viewerWorldPos, terrainChunks);
        
        chunkThreadManager.CheckThreads();
    }


    public void UpdateVisibleChunks(Vector2Int currentChunkPos) 
    {
        Vector2Int startChunk = Vector2Int.zero;
        startChunk.x = Mathf.FloorToInt(viewerWorldPos.x/(chunkSize * chunkScale));
        startChunk.y = Mathf.FloorToInt(viewerWorldPos.y/(chunkSize * chunkScale));
        
        //All visible chunks end on this list. here we update all of them and remove the non visible ones
        for (int i = visibleChunks.Count - 1; i >= 0; i--)
        {
            visibleChunks[i].Update((startChunk + Vector2.one * sqrt2 * 0.5f) * chunkSize * chunkScale, chunkSize * chunkScale * sqrt2 * 0.5f * viewRadius);
            if (!visibleChunks[i].IsVisible())
            {
                //chunks that just turned invisible are removed from visibleChunks
                visibleChunks.RemoveAt(i);
            }
        }
        
        int chunkViewRange = Mathf.FloorToInt(viewRadius);

        //Looping through all chunks that HAVE to be visible in this frame
        for (int dy = 1 - chunkViewRange; dy <= chunkViewRange; dy++)
        {
            for (int dx = 1 - chunkViewRange; dx <= chunkViewRange; dx++)
            {
                
                Vector2Int viewChunkCoord = new Vector2Int(startChunk.x + dx, startChunk.y + dy);

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
