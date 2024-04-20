using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TerrainObject
{
    
}



public class TerrainObjectsManager
{
    private TerrainChunk currentChunk;
    private Vector2Int currentSubChunkPos; 
    private Vector2 curretViewerPos;//used to check if the player moved 
    int subChunkSize;

    private int subChunkLevel;

    private DetailChunk[] detailChunks;

    private Material detailMaterial;
    Vector2 atlasSize;

    private Dictionary<Biomes, TerrainDetailSettings> biomeDetails;
    

    public TerrainObjectsManager(DetailGenerationSettings detailGenerationSettings, Texture2D atlasTexture)
    {
        this.subChunkLevel = detailGenerationSettings.subChunkLevel;
        this.detailMaterial = detailGenerationSettings.defaultDetailMaterial;
        
        this.detailMaterial.SetTexture("_MapAtlas", atlasTexture);
        
        Texture2D detailAtlas = detailGenerationSettings.detailAtlas;
        atlasSize = new Vector2(detailAtlas.width, detailAtlas.height);
        this.detailMaterial.SetTexture("_SpriteAtlas", detailAtlas);
        
        this.biomeDetails = detailGenerationSettings.biomeDetails;
        this.detailChunks = new DetailChunk[9];
    }




    //Called every frame to draw the terrain details and check if we need to generate new chunks
    public void UpdateObjectChunks(TerrainChunk currentChunk, Vector2 viewerWorldPos, Dictionary<Vector2Int,TerrainChunk> terrainChunks)
    {   
        this.currentChunk = currentChunk;
        subChunkSize = currentChunk.ChunkSize / MathMisc.TwoPowX(subChunkLevel);

        DrawDetails();

        Vector2 displacement = viewerWorldPos - curretViewerPos;
        Vector2Int subChunkDisplacement = Vector2Int.zero;
        
        subChunkDisplacement.x = Mathf.FloorToInt(displacement.x/subChunkSize);
        subChunkDisplacement.y = Mathf.FloorToInt(displacement.y/subChunkSize);

        if (subChunkDisplacement == Vector2Int.zero)
        {
            return;
        }   
        curretViewerPos.x = Mathf.FloorToInt(viewerWorldPos.x/subChunkSize) * subChunkSize; //snap to subchunk coords
        curretViewerPos.y = Mathf.FloorToInt(viewerWorldPos.y/subChunkSize) * subChunkSize;

        currentSubChunkPos = currentChunk.WorldToSubChunkCoords(viewerWorldPos, subChunkLevel);

        UpdateDetailChunks(subChunkDisplacement,terrainChunks);
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

                //the chunk that would be replaced by the current chunk
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
                Vector2Int nChunkDisplacement = Vector2Int.zero;
                nChunkDisplacement.x = Mathf.FloorToInt(nSubChunkPos.x / (float)MathMisc.TwoPowX(subChunkLevel));
                nChunkDisplacement.y = Mathf.FloorToInt(nSubChunkPos.y / (float)MathMisc.TwoPowX(subChunkLevel));
                
                TerrainChunk nChunk = terrainChunks[currentChunk.Position + nChunkDisplacement];
                
                Vector2Int nSubChunkLocalPos = nSubChunkPos - nChunkDisplacement * MathMisc.TwoPowX(subChunkLevel);
                Debug.Log(nSubChunkLocalPos);
                QuadChunk nSubChunk = nChunk.GetSubChunk(nSubChunkLocalPos, subChunkLevel);
                
                detailChunks[index] = new DetailChunk(detailMaterial, atlasSize, nSubChunk, biomeDetails);
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
