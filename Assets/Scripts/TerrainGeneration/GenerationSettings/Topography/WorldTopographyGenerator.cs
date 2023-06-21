using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//controls how the BiomeSettings Topography data is used for generating biomes
public class WorldTopographyGenerator : ScriptableObject
{
    public int seed = 1;
    public virtual HeightMapGenerator GetHeightMapGenerator(TopographySettings topographySettings)
    {
        return null;
    }
}
