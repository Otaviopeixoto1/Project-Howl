using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Struct used to pass the world sampler and other data to the chunk generation threads
/// </summary>
public struct ChunkGenerationThreadData
{
    public readonly WorldGenerator worldGenerator;
    public readonly Vector2 chunkPosition;
    public readonly int chunkSize;
    public readonly float chunkScale;
    public readonly int meshLodBias;
    public readonly int colliderLodBias;

    public ChunkGenerationThreadData (WorldGenerator worldGenerator,Vector2 gridPosition, int chunkSize, float chunkScale, int meshLodBias, int colliderLodBias)
    {
        this.worldGenerator = worldGenerator;
        this.chunkPosition = gridPosition * chunkSize;
        this.chunkSize = chunkSize;
        this.chunkScale = chunkScale;
        this.meshLodBias = meshLodBias;
        this.colliderLodBias = colliderLodBias;
    }
}


public class ChunkThreadManager
{
    private Queue<MeshThreadInfo> meshDataThreadInfoQueue = new Queue<MeshThreadInfo>();


    public void CheckThreads()
    {
        if (meshDataThreadInfoQueue.Count > 0) 
        {
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) 
            {
				MeshThreadInfo threadInfo = meshDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.meshData, threadInfo.colliderData);
			}
		}
    }

    public void RequestMeshData(ChunkGenerationThreadData mapData, Action<MeshData, MeshData> callback) {
		ThreadStart threadStart = delegate{
			MeshDataThread(mapData, callback);
		};

		new Thread(threadStart).Start();
	}

	private void MeshDataThread(ChunkGenerationThreadData mapData, Action<MeshData, MeshData> callback) {
		MeshData meshData = MeshGenerator.GenerateTerrainChunk(mapData.worldGenerator, 
                                                                mapData.chunkSize + 1, 
                                                                mapData.chunkSize + 1, 
                                                                mapData.chunkScale, 
                                                                mapData.chunkPosition,
                                                                mapData.meshLodBias,
                                                                true
                                                                );
		
		// Use a different function for the collider mesh
		MeshData colliderData = MeshGenerator.GenerateTerrainChunk(mapData.worldGenerator, 
                                                                    mapData.chunkSize + 1, 
                                                                    mapData.chunkSize + 1, 
                                                                    mapData.chunkScale, 
                                                                    mapData.chunkPosition,
                                                                    mapData.colliderLodBias,
                                                                    true
                                                                    );


		lock (meshDataThreadInfoQueue) {
			meshDataThreadInfoQueue.Enqueue(new MeshThreadInfo(callback, meshData, colliderData));
		}
	}
    
    /// <summary>
    /// struct used to send the calculated chunk mesh data from other threads to the main thread
    /// </summary>
    private struct MeshThreadInfo {
		public readonly Action<MeshData,MeshData> callback;
		public readonly MeshData meshData;
		public readonly MeshData colliderData;

		public MeshThreadInfo (Action<MeshData, MeshData> callback, MeshData meshData, MeshData colliderData)
		{
			this.callback = callback;
			this.meshData = meshData;
			this.colliderData = colliderData;
		}
		
	}
}
