using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkManager : MonoBehaviour
{
    private TerrainGenerator terrainGenerator;
    /*
    private Dictionary<Vector2Int, TerrainChunk> terrainChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private List<TerrainChunk> visibleChunksOnLastUpdate = new List<TerrainChunk>();

    //queue containing the mesh data processed inside the threads
    private Queue<MeshThreadInfo> meshDataThreadInfoQueue = new Queue<MeshThreadInfo>();
    */

    void Start()
    {
        terrainGenerator = GetComponent<TerrainGenerator>();
    }
    void Update()
    {
    }
}
