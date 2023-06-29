using System;
using System.Threading;
using System.Collections.Generic;

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

    public void RequestMeshData(SamplerThreadData mapData, Action<MeshData, MeshData> callback) {
		ThreadStart threadStart = delegate{
			MeshDataThread(mapData, callback);
		};

		new Thread(threadStart).Start();
	}

	private void MeshDataThread(SamplerThreadData mapData, Action<MeshData, MeshData> callback) {
		MeshData meshData = MeshGenerator.GenerateTerrainFromSampler(mapData.sampler, 
                                                                    mapData.chunkSize + 1, 
                                                                    mapData.chunkSize + 1, 
                                                                    mapData.chunkScale, 
                                                                    mapData.chunkPosition,
                                                                    mapData.meshLodBias,
                                                                    true
                                                                    );

		MeshData colliderData = MeshGenerator.GenerateTerrainFromSampler(mapData.sampler, 
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
