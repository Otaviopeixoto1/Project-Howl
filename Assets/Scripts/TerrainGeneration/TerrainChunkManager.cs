using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using UnityEngine;



public struct MapData //Change name of this class
{
    public readonly WorldSampler sampler;
    public readonly Vector2 chunkPosition;
    public readonly int chunkSize;
    public readonly float chunkScale;
    public readonly int lodBias;

    public MapData (WorldSampler sampler,Vector2 gridPosition, int chunkSize, float chunkScale, int lodBias)
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
    GameObject meshObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;

    int chunkSize; //size refers to the mesh size (number of mesh vertices along x and y - 1)
    float scale;
    Vector2Int position;
    Vector2 worldPosition;
    Bounds bounds;

    Texture2D debugTexture;

    

    public TerrainChunk(Vector2Int coord, MapData mapData, Transform parent, Material material, TerrainChunkManager chunkManager)
    {
        this.chunkSize = mapData.chunkSize;
        this.scale = mapData.chunkScale;

        worldPosition = ((Vector2)coord) * chunkSize * scale;
        bounds = new Bounds(worldPosition, Vector2.one * chunkSize * scale);

        meshObject = new GameObject("Terrain Chunk");
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshRenderer.material = material; 

        debugTexture = CreateDebugTexture(mapData.sampler);

        Vector3 worldPositionV3 = new Vector3(worldPosition.x, 0, worldPosition.y);

        meshObject.transform.position = worldPositionV3;
        meshObject.transform.parent = parent;

        SetVisible(false);

        chunkManager.RequestMeshData(mapData, OnMeshDataReceived);
    }



    //called on the main thread, when mesh data calculations are finished
    void OnMeshDataReceived(MeshData meshData)  
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
        meshObject.SetActive(visible);
    }

    public bool IsVisible()
    {
        return meshObject.activeSelf;
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
        //meshRenderer.material.SetTexture("debugTerrainTexture", tex);
    }

}







[RequireComponent(typeof(WorldSampler))]
public class TerrainChunkManager : MonoBehaviour
{
    private Dictionary<Vector2Int, TerrainChunk> terrainChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private List<TerrainChunk> visibleChunksOnLastUpdate = new List<TerrainChunk>();
    //queue containing the mesh data processed inside the threads
    private Queue<MeshThreadInfo> meshDataThreadInfoQueue = new Queue<MeshThreadInfo>();


    [SerializeField]
    private Transform viewer;
    [SerializeField]
    private Material testMaterial;
    public static Vector2 viewerWorldPos;

    private WorldSampler worldSampler;

    [SerializeField]
    [Range(10,240)]
    private int chunkSize = 240;
    [SerializeField]
    private float chunkScale = 1f;
    private float maxViewDistance = 300;
    private int chunkVisibilityRadius;



    void Start()
    {   
        chunkVisibilityRadius = Mathf.RoundToInt(maxViewDistance/chunkSize);
        worldSampler = GetComponent<WorldSampler>();
    }

    void Update()
    {
        viewerWorldPos = new Vector2(viewer.position.x, viewer.position.z);

        UpdateVisibleChunks();
        
        
        if (meshDataThreadInfoQueue.Count > 0) 
        {
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
				MeshThreadInfo threadInfo = meshDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.meshData);
			}
		}
    }

    void OnValidate()
    {   
        if (chunkScale < 0.01f)
        {
            chunkScale = 0.01f;
        }
        chunkVisibilityRadius = Mathf.RoundToInt(maxViewDistance/chunkSize);
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

        for (int dy = -chunkVisibilityRadius; dy <= chunkVisibilityRadius; dy++)
        {
            for (int dx = -chunkVisibilityRadius; dx <= chunkVisibilityRadius; dx++)
            {
                Vector2Int viewChunkCoord = new Vector2Int(currentChunkX + dx, currentChunkY + dy);

                if (terrainChunks.ContainsKey(viewChunkCoord))
                {
                    terrainChunks[viewChunkCoord].Update(viewerWorldPos,maxViewDistance);// call update with the arguments
                    if(terrainChunks[viewChunkCoord].IsVisible())
                    {
                        visibleChunksOnLastUpdate.Add(terrainChunks[viewChunkCoord]);
                    }
                }
                else
                {
                    MapData mapData = new MapData(worldSampler, viewChunkCoord, chunkSize, chunkScale, 0);
                    terrainChunks.Add(viewChunkCoord, new TerrainChunk(viewChunkCoord, mapData, this.transform, testMaterial, this));
                }


            }
        }
    }





////////////////////////////////// Threading the mesh data calculations ////////////////////////////////////////////
    
    public void RequestMeshData(MapData mapData, Action<MeshData> callback) {
		ThreadStart threadStart = delegate{
			MeshDataThread(mapData, callback);
		};

		new Thread(threadStart).Start();
	}

	private void MeshDataThread(MapData mapData, Action<MeshData> callback) {
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
