using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this class will either be the parent or it will contain the terrain detail manager
//it will be used for managing the positions occupied by the terrain objects
//this is necessary so that we dont draw terrain details inside other details or objects

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

    private TerrainDetailsManager terrainDetailsManager;
    

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
    public TerrainObjectsManager(TerrainManager terrainManager, WorldSampler worldSampler)
    {
        this.terrainManager = terrainManager;
        this.worldSampler = worldSampler;
        this.terrainDetailsManager = new TerrainDetailsManager();
    }


    /*
    private IEnumerator WaitForChunkGeneration()
    {

    }*/

    public void UpdateObjects()
    {
        //Get the subchunks with data on the objects on them and the details that can be spawned

        //THIS HAS TO BE ASYNC, we wait until the object has a mesh before getting the vertices
        //TerrainManager.StartCoroutine(WaitForChunkGeneration());
        //List<Vector3> positions = terrainManager.GetCurrentChunk().GetVertices();
        

        //terrainDetailsManager.UpdateDetails(positions);
    }

}
