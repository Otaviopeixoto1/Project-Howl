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
				threadInfo.callback(threadInfo.meshData);
			}
		}
    }

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
