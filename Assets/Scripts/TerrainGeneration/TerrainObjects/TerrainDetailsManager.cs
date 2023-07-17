using System.Collections;
using System.Collections.Generic;
using UnityEngine;




//try to use the least amount of draw calls for gpu instancing (try using single big chunks instead of multiple small ones)
//the DetailManager and ObjectManager will need information from world sampler and world manager as well as from
//the terrain manager. Find a way to request and recieve all this data in a nice way


public class TerrainDetailsManager 
{
    //ADD SUPPORT FOR MULTIPLE DETAIL TYPES !
    //ADD A CLASS FOR EACH DETAIL THAT GENERATES THE CORRESPONDING BUFFERS FOR THEM
    public TerrainDetailsManager()
    {

    }

    public void UpdateDetails(List<Vector3> positions)
    {
        
    }
}
