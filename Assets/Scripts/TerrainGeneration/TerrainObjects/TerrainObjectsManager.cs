using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainObjectsManager
{
    //The terrain manager will be used to get the current player position and the chunk data
    private TerrainManager terrainManager;
    //Manage the terrain object data in a thread, together with the terrain mesh. we add object data for each chunk
    //the object data will store the terrain objects positions and additional information (size) to avoid spawning
    //objects on top of eachother

    //Maybe the world sampler is not necessary. Just use the data stored on the chunks instead.
    
    //TAKE ADVANTAGE OF THREADING !

    //the chunks can use the same quadtree to store both objects and biome information
    //otherwise, we can just use the world sampler to sample what biome is in the current position
    private WorldSampler worldSampler;

    //the object manager wont only place details but also GameObjects (prefabs). 
    //The details are just handled specially by gpu instancing

    private DetailChunk testChunk;

    private Vector2Int previousChunkPos;
    private Vector2Int previousSubChunkPos; //used to check if the player moved 


    private int subChunkSubdivision = 3;


    private DetailChunk[] detailChunks = new DetailChunk[9];
    //private Dictionary<Vector2Int, DetailChunk> detailChunks = new Dictionary<Vector2Int, DetailChunk>();

    
    private Material testMaterial;
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////

    //the object data will be stored in a quadtree. the objects will occupy a certain number of 2d standard sized tiles
    //the empty tiles will be all merged as a single tree branch while the tree will keep branching until the entire 
    //object is contained on its branches. This way, each leaf will consist of empty positions that can be used or 
    //occupied positions where we have an object. Objects have to be carefully placed according to a standard: 

    //-Never place them in the center of one of the big tree branch quads

    //-the tree must have a maximum depth and at the maximum depth, the quad must be of the minimum size allowed for 
    //an object (use the size of a grass sprite as standard for minimum)

    //-to improve the system even more, we can make the rendered subchunks have the same size and center position
    //as one of the quadtree's quad at a cetain depth (2 or 3 ?)

    //add support for large structure objects ! (these objects will occupy big quads like a quad of depth 2 or 3)
    //these dont have to branch !

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////


    

    //this will store and call update on all the specific object managers after deciding what positions should be 
    //updated !
    public TerrainObjectsManager(TerrainManager terrainManager, WorldSampler worldSampler, Material detailTestMaterial)
    {
        this.terrainManager = terrainManager;
        this.worldSampler = worldSampler;


        this.testMaterial = detailTestMaterial;

        //the object manager will be the one to actually manage the chunks 
        //the detail manager will just receive data and update all the details that are being drawn
        //this.testChunk = new DetailChunk(detailTestMaterial);
    }




    //Called every frame to check if we need to generate a new subchunk
    public void UpdateObjectChunks(Vector2 viewerWorldPos, Dictionary<Vector2Int,TerrainChunk> terrainChunks)
    {
        Vector2Int chunkPos = terrainManager.WorldToChunkCoords(viewerWorldPos);
        TerrainChunk currentChunk = terrainChunks[chunkPos];

        if (!currentChunk.hasMesh)
        {
            return;
        } 
        //draw the terrain details
        testChunk?.Draw();
        //DrawDetails();

        Vector2Int subChunkPos = currentChunk.WorldToSubChunkCoords(viewerWorldPos, subChunkSubdivision); 
 
        if (subChunkPos == previousSubChunkPos && chunkPos == previousChunkPos)
        {
            return;
        }   
        
        //if both tests passed, the player has moved enough so that the chunks being rendered have to be updated


        Vector2Int subChunkDisplacement = subChunkPos - previousSubChunkPos;

        previousChunkPos = chunkPos;
        previousSubChunkPos = subChunkPos;

        

        DetailChunk[] newDetailChunks = new DetailChunk[9];
        List<int> reusedChunks = new List<int>();

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                int index = x + 3 * y;

                int oldX = x + subChunkDisplacement.x;
                int oldY = y + subChunkDisplacement.y;

                if (oldX >= 0 && oldX <= 2 && oldY >= 0 && oldY <= 2)
                {
                    int oldIndex = oldX + 3 * oldY;

                    if (detailChunks[oldIndex] != null)
                    {
                        reusedChunks.Add(oldIndex);
                        newDetailChunks[index] = detailChunks[oldIndex];
                    }
                    else
                    {
                        //newDetailChunks[index] = new DetailChunk(testMaterial);
                    }
                }
                else
                {
                    //newDetailChunks[index] = new DetailChunk(testMaterial);
                }


            }
        }

        for (int i = 0; i < detailChunks.Length; i++)
        {
            if (!reusedChunks.Contains(i))
            {
                detailChunks[i]?.Clear();
            }
        }

        detailChunks = newDetailChunks;


        //Take the subchunk as argument
        testChunk?.Clear();
        testChunk = new DetailChunk(testMaterial, terrainManager.GetCurrentSubChunk(subChunkSubdivision));
        //testChunk.SetupDetails(terrainManager.GetCurrentChunk().GetBounds(), positions);
        
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
        testChunk.Clear();
    }

}
