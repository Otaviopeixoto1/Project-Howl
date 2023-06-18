using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using UnityEngine;



public struct MapData 
{
    public readonly MapGenerator sampler;
    public readonly int chunkSize;
    public readonly float chunkScale;
    public readonly int lodBias;
    //Add offset and texture

    public MapData (MapGenerator sampler, int chunkSize, float chunkScale, int lodBias)
    {
        this.sampler = sampler;
        this.chunkSize = chunkSize;
        this.chunkScale = chunkScale;
        this.lodBias = lodBias;
    }
}




[RequireComponent(typeof(MapComposer))]
public class TerrainGenerator : MonoBehaviour
{
    //Create a chunk manager class that deals with these
    //the chunk manager should also destroy far chunks
    private Dictionary<Vector2Int, TerrainChunk> terrainChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private List<TerrainChunk> visibleChunksOnLastUpdate = new List<TerrainChunk>();



    //queue containing the mesh data processed inside the threads
    private Queue<MeshThreadInfo> meshDataThreadInfoQueue = new Queue<MeshThreadInfo>();


    

    [SerializeField]
    private Transform viewer;
    [SerializeField]
    private Material testMaterial;
    public static Vector2 viewerWorldPos;

    [SerializeField]
    private HeightMapGenerator heightMapGenerator;

    [SerializeField]
    [Range(10,240)]
    private int chunkSize = 240;

    [SerializeField]
    [Range(0.01f,10)]
    private float chunkScale = 1f;
    private const float maxViewDistance = 300;
    private int chunkVisibilityRadius;



    void Start()
    {   
        chunkVisibilityRadius = Mathf.RoundToInt(maxViewDistance/chunkSize);
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
        chunkVisibilityRadius = Mathf.RoundToInt(maxViewDistance/chunkSize);
    }

    //the current class is only supposed to generate the chunks and take car of threading and chunk variables
    //. the update logic should go on terrainchunkmanager
    public void UpdateVisibleChunks() //add this to the chunk manager
    {
        MapData mapData = new MapData(heightMapGenerator, chunkSize, chunkScale, 8);

        //////////////////////////////Clearing all chunks/////////////////////////// 
        for (int i = 0; i < visibleChunksOnLastUpdate.Count; i++)
        {
            visibleChunksOnLastUpdate[i].SetVisible(false);
        }
        visibleChunksOnLastUpdate.Clear();
        ////////////////////////////////////////////////////////////////////////////
        

        int currentChunkX = Mathf.RoundToInt(viewerWorldPos.x/chunkSize);
        int currentChunkY = Mathf.RoundToInt(viewerWorldPos.y/chunkSize);

        for (int dy = -chunkVisibilityRadius; dy <= chunkVisibilityRadius; dy++)
        {
            for (int dx = -chunkVisibilityRadius; dx <= chunkVisibilityRadius; dx++)
            {
                Vector2Int viewChunkCoord = new Vector2Int(currentChunkX + dx, currentChunkY + dy);

                if (terrainChunks.ContainsKey(viewChunkCoord))
                {
                    terrainChunks[viewChunkCoord].Update();// call update with the arguments
                    if(terrainChunks[viewChunkCoord].IsVisible())
                    {
                        visibleChunksOnLastUpdate.Add(terrainChunks[viewChunkCoord]);
                    }
                }
                else
                {
                    terrainChunks.Add(viewChunkCoord, 
                                        new TerrainChunk(viewChunkCoord, mapData, this.transform, testMaterial, this));
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
                                                                    mapData.chunkSize, 
                                                                    mapData.chunkSize, 
                                                                    mapData.chunkScale, 
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

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////







    //define terrainChunk outside of this class. Also define a terrain chunk manager to deal with the threading
    public class TerrainChunk
    {
        GameObject meshObject;
        MeshRenderer meshRenderer;
		MeshFilter meshFilter;

        int size; //size refers to the mesh size (number of mesh vertices along x and y)
        float scale;
        Vector2Int position;
        Vector2 worldPosition;
        Bounds bounds;

        

        public TerrainChunk(Vector2Int coord, MapData mapData, Transform parent, Material material, TerrainGenerator terrainGenerator)
        {
            this.size = mapData.chunkSize;
            this.scale = mapData.chunkScale;

            worldPosition = ((Vector2)coord) * size * scale;
            bounds = new Bounds(worldPosition, Vector2.one * size);

            meshObject = new GameObject("Terrain Chunk");
			meshRenderer = meshObject.AddComponent<MeshRenderer>();
			meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer.material = material; //Request material and mesh

            Vector3 worldPositionV3 = new Vector3(worldPosition.x, 0, worldPosition.y);

            meshObject.transform.position = worldPositionV3;
            meshObject.transform.parent = parent;

            SetVisible(false);

            terrainGenerator.RequestMeshData(mapData, OnMeshDataReceived); //create a function to call this on terrain chunk manager
        }


        void OnMeshDataReceived(MeshData meshData) {
			meshFilter.mesh = meshData.CreateMesh();
		}




        public void Update() //add maxViewDistance and viewerWorldPos as arguments
        {
            float viewerDistance = Mathf.Sqrt(bounds.SqrDistance(viewerWorldPos)); //viewer distance from edge of chunk
            bool visible = viewerDistance <= maxViewDistance;
            SetVisible(visible); //Only destroy the ones that are actualy 
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

    }


}
