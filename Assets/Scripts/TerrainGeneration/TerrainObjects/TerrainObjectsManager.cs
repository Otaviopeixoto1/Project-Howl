using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainObjectsManager
{
    private TerrainManager terrainManager;
    private WorldSampler worldSampler;//this will contain the generation settings that will contain the detail settings

    private Vector2Int currentChunkPos;
    private Vector2Int currentSubChunkPos; //used to check if the player moved 

    private const int subChunkSubdivision = 3;

    private DetailChunk[] detailChunks;

    private Material testMaterial;
    

    public TerrainObjectsManager(TerrainManager terrainManager, WorldSampler worldSampler, Material detailTestMaterial)
    {
        this.terrainManager = terrainManager;
        this.worldSampler = worldSampler;


        this.testMaterial = detailTestMaterial;
        this.detailChunks = new DetailChunk[9];
    }




    //Called every frame to draw the terrain details and check if we need to generate new chunks
    public void UpdateObjectChunks(Vector2 viewerWorldPos, Dictionary<Vector2Int,TerrainChunk> terrainChunks)
    {
        Vector2Int chunkPos = terrainManager.WorldToChunkCoords(viewerWorldPos);
        TerrainChunk currentChunk = terrainChunks[chunkPos];
        Vector2Int subChunkPos = currentChunk.WorldToGlobalSubChunkCoords(viewerWorldPos, subChunkSubdivision);
        
        DrawDetails();

        if (subChunkPos == currentSubChunkPos)
        {
            return;
        }   
        //If both tests passed, the player has moved enough and the chunks being rendered have to be updated
        
        Vector2Int globalSubChunkDisplacement = subChunkPos - currentSubChunkPos;

        currentChunkPos = chunkPos;
        currentSubChunkPos = subChunkPos;

        UpdateDetailChunks(globalSubChunkDisplacement,terrainChunks);
    }
    


    private void UpdateDetailChunks(Vector2Int subChunkDisplacement, Dictionary<Vector2Int,TerrainChunk> terrainChunks)
    {
        int startX = subChunkDisplacement.x >= 0 ? -1 : 1;
        int startY = subChunkDisplacement.y >= 0 ? -1 : 1;

        for (int y = startY; y != - 2 * startY; y -= startY)
        {
            for (int x = startX; x != - 2 * startX; x -= startX)
            {
                int index = (x+1) + 3 * (y+1);

                //the chunk that would be replaced by the curren chunk
                int newX = x - subChunkDisplacement.x;
                int newY = y - subChunkDisplacement.y;

                if (newX < -1 || newX > 1 || newY < -1 || newY > 1)
                {
                    detailChunks[index]?.Clear();
                }

                //the chunk that would replace the current chunk
                int oldX = x + subChunkDisplacement.x;
                int oldY = y + subChunkDisplacement.y;

                //detail chunk pooling:
                //the chunks that are still in the viewer range are kept and not regenerated
                if (oldX >= -1 && oldX <= 1 && oldY >= -1 && oldY <= 1)
                {
                    int oldIndex = (oldX+1) + 3 * (oldY+1);

                    if (detailChunks[oldIndex] != null)
                    {
                        detailChunks[index] = detailChunks[oldIndex];
                        continue;
                    }
                }

                Vector2Int nSubChunkPos = new Vector2Int(currentSubChunkPos.x + x, currentSubChunkPos.y + y);
                Vector2Int nChunkPos = TerrainChunk.GlobalSubChunkToChunkCoords(nSubChunkPos, subChunkSubdivision);
                
                TerrainChunk nChunk = terrainChunks[nChunkPos];

                detailChunks[index] = new DetailChunk(testMaterial, nChunk.GetSubChunkFromGlobalPos(nSubChunkPos, subChunkSubdivision));
                
            }
        }
    }



    private void DrawDetails()
    {
        foreach (DetailChunk detailChunk in detailChunks)
        {
            detailChunk?.Draw();
        }
    }


    public void ClearObjects()
    {
        foreach (DetailChunk detailChunk in detailChunks)
        {
            detailChunk?.Clear();
        }
    }

}
