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
    public readonly Vector2Int chunkPosition;
    public readonly int chunkSize;
    public readonly float chunkScale;
    public readonly int meshLodBias;
    public readonly int colliderLodBias;

    public ChunkGenerationThreadData (WorldGenerator worldGenerator,Vector2Int gridPosition, int chunkSize, float chunkScale, int meshLodBias, int colliderLodBias)
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
    private Queue<ChunkThreadResult> meshDataThreadInfoQueue = new Queue<ChunkThreadResult>();


    public void CheckThreads()
    {
        if (meshDataThreadInfoQueue.Count > 0) 
        {
			for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) 
            {
				ChunkThreadResult threadInfo = meshDataThreadInfoQueue.Dequeue();
				threadInfo.callback(threadInfo.chunkData);
			}
		}
    }

    public void RequestChunkData(ChunkGenerationThreadData mapData, Action<ChunkData> callback) 
    {
		ThreadStart threadStart = delegate{
			ChunkDataThread(mapData, callback);
		};

		new Thread(threadStart).Start();
	}

	private void ChunkDataThread(ChunkGenerationThreadData mapData, Action<ChunkData> callback) 
    {
		ChunkData chunkData = ChunkGenerator.GenerateTerrainChunk(mapData.worldGenerator, 
                                                                mapData.chunkSize, 
                                                                mapData.chunkScale, 
                                                                mapData.chunkPosition,
                                                                mapData.meshLodBias,
                                                                mapData.colliderLodBias
                                                                );
		


		lock (meshDataThreadInfoQueue) 
        {
			meshDataThreadInfoQueue.Enqueue(new ChunkThreadResult(callback, chunkData));
		}
	}
    
    /// <summary>
    /// struct used to send the calculated chunk mesh data from other threads to the main thread
    /// </summary>
    private struct ChunkThreadResult {
		public readonly Action<ChunkData> callback;
		public readonly ChunkData chunkData;

		public ChunkThreadResult (Action<ChunkData> callback, ChunkData chunkData)
		{
			this.callback = callback;
			this.chunkData = chunkData;
		}
		
	}
}
