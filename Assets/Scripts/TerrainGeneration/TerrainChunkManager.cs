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
    public readonly int lodBias;

    public SamplerThreadData (WorldSampler sampler,Vector2 gridPosition, int chunkSize, float chunkScale, int lodBias)
    {
        this.sampler = sampler;
        this.chunkPosition = gridPosition * chunkSize;
        this.chunkSize = chunkSize;
        this.chunkScale = chunkScale;
        this.lodBias = lodBias;
    }
}



public class TerrainChunk
{
    GameObject chunkObject; // all of these variables should be readonly
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    int chunkSize; //size refers to the mesh size (number of mesh vertices along x and y - 1)
    float scale;
    Vector2Int position;
    Vector2 worldPosition;
    Bounds bounds;

    Texture2D debugTexture;

    

    public TerrainChunk(Vector2Int coord, SamplerThreadData mapData, Transform parent, Material material, TerrainChunkManager chunkManager)
    {
        this.chunkSize = mapData.chunkSize;
        this.scale = mapData.chunkScale;

        worldPosition = ((Vector2)coord) * chunkSize * scale;
        bounds = new Bounds(worldPosition, Vector2.one * chunkSize * scale);

        chunkObject = new GameObject("Terrain Chunk");
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer.material = material; 

        debugTexture = CreateDebugTexture(mapData.sampler);

        Vector3 worldPositionV3 = new Vector3(worldPosition.x, 0, worldPosition.y);

        chunkObject.transform.position = worldPositionV3;
        chunkObject.transform.parent = parent;

        //SetVisible(false);

        chunkManager.RequestMeshData(mapData, OnMeshDataReceived);
    }



    
    private void OnMeshDataReceived(MeshData meshData)  
    {                                           
        meshFilter.mesh = meshData.CreateMesh();
        meshRenderer.material.SetTexture("_BaseMap", debugTexture);
    }

    public void Update(Vector2 viewerWorldPos, float maxViewDistance) 
    {
        //viewer distance from the edge of the chunk:
        float viewerDistance = Mathf.Sqrt(bounds.SqrDistance(viewerWorldPos)); 
        
        bool visible = viewerDistance <= maxViewDistance * scale;
        SetVisible(visible); //Only destroy the chunks that are actualy very far
                            // (probably wont be rendered)
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
public class TerrainChunkManager : MonoBehaviour
{
    private Dictionary<Vector2Int, TerrainChunk> terrainChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private List<TerrainChunk> visibleChunksOnLastUpdate = new List<TerrainChunk>();

    //queue containing the mesh data processed inside the threads:
    private Queue<MeshThreadInfo> meshDataThreadInfoQueue = new Queue<MeshThreadInfo>();
    
    private ChunkThreadManager chunkThreadManager;

    private WorldSampler worldSampler;


    [SerializeField]
    private Transform viewer;
    [HideInInspector]
    public Vector2 viewerWorldPos;
    private Vector2 lastViewerPos;
    [SerializeField]
    private float thresholdForChunkUpdate = 0.1f;


    [SerializeField]
    private Material testMaterial;
    [SerializeField]
    [Range(10,240)]
    private int chunkSize = 240;
    [SerializeField]
    private float chunkScale = 1f;
    private float maxViewDistance = 300; //useless Add the chunkVisibilityRadius in chunks directly
    private int chunkVisibilityRadius; //set to public and set manualy.



    void Start()
    {   
        chunkVisibilityRadius = Mathf.RoundToInt(maxViewDistance/chunkSize);
        worldSampler = GetComponent<WorldSampler>();

        //UpdateVisibleChunks(); // send a signal after the world sampler has all the data needed
    }

    void OnValidate()
    {   
        if (chunkScale < 0.01f)
        {
            chunkScale = 0.01f;
        }
        if (thresholdForChunkUpdate < 0.01f)
        {
            thresholdForChunkUpdate = 0.01f;
        }

        chunkVisibilityRadius = Mathf.RoundToInt(maxViewDistance/chunkSize);
    }




    void Update()
    {
        viewerWorldPos = new Vector2(viewer.position.x, viewer.position.z);



        //for the current case, it doesnt make sense to update the chunks unless the chunk coordinate has changed
        // (the player moved from one chunk to another)
        //update the current chunks only if the player has changed from one chunk to another 
        //(its a topdown view). the condition could also be changed (even dynamicaly)
        if (Vector3.Distance(viewerWorldPos, lastViewerPos) > thresholdForChunkUpdate * chunkSize * chunkScale)
        {
            lastViewerPos = viewerWorldPos;
            UpdateVisibleChunks();
        }
        
        
        
        if (meshDataThreadInfoQueue.Count > 0) 
        {
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) 
            {
				MeshThreadInfo threadInfo = meshDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.meshData);
			}
		}
    }


    public void UpdateVisibleChunks() 
    {
        //////////////////////////////Clearing all chunks/////////////////////////// 
        for (int i = 0; i < visibleChunksOnLastUpdate.Count; i++)
        {
            visibleChunksOnLastUpdate[i].SetVisible(false);
        }
        visibleChunksOnLastUpdate.Clear();
        ////////////////////////////////////////////////////////////////////////////
        

        int currentChunkX = Mathf.RoundToInt(viewerWorldPos.x/(chunkSize * chunkScale));
        int currentChunkY = Mathf.RoundToInt(viewerWorldPos.y/(chunkSize * chunkScale));

        //Looping through all chunks that should be visible in this frame
        for (int dy = -chunkVisibilityRadius; dy <= chunkVisibilityRadius; dy++)
        {
            for (int dx = -chunkVisibilityRadius; dx <= chunkVisibilityRadius; dx++)
            {
                Vector2Int viewChunkCoord = new Vector2Int(currentChunkX + dx, currentChunkY + dy);

                if (terrainChunks.ContainsKey(viewChunkCoord))
                {
                    terrainChunks[viewChunkCoord].Update(viewerWorldPos,maxViewDistance);
                    if(terrainChunks[viewChunkCoord].IsVisible())
                    {
                        visibleChunksOnLastUpdate.Add(terrainChunks[viewChunkCoord]);
                    }
                }
                else
                {
                    SamplerThreadData mapData = new SamplerThreadData(worldSampler, viewChunkCoord, chunkSize, chunkScale, 0);
                    terrainChunks.Add(viewChunkCoord, new TerrainChunk(viewChunkCoord, mapData, this.transform, testMaterial, this));
                    visibleChunksOnLastUpdate.Add(terrainChunks[viewChunkCoord]);
                }


            }
        }
    }





////////////////////////////////// Threading the mesh data calculations ////////////////////////////////////////////
    
    public void RequestMeshData(SamplerThreadData mapData, Action<MeshData> callback) {
		ThreadStart threadStart = delegate{
			MeshDataThread(mapData, callback);
		};

		new Thread(threadStart).Start();
	}

	private void MeshDataThread(SamplerThreadData mapData, Action<MeshData> callback) {
		MeshData meshData = MeshGenerator.GenerateTerrainFromSampler(mapData.sampler, 
                                                                    mapData.chunkSize + 1, 
                                                                    mapData.chunkSize + 1, 
                                                                    mapData.chunkScale, 
                                                                    mapData.chunkPosition,
                                                                    mapData.lodBias,
                                                                    true
                                                                    );


		lock (meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue(new MeshThreadInfo(callback, meshData));
		}
	}





    /// <summary>
    /// struct used to send the calculated chunk mesh data from other threads to the main thread
    /// </summary>
    private struct MeshThreadInfo {
		public readonly Action<MeshData> callback;
		public readonly MeshData meshData;

		public MeshThreadInfo (Action<MeshData> callback, MeshData meshData)
		{
			this.callback = callback;
			this.meshData = meshData;
		}
		
	}


}
