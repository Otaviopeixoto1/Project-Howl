using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum Biomes
{

}



/////////////////////////////////////////////////////////////////////////////////////////////////////////
//  These classes store data that gets serialized into json files for saving and loading baked map data
/////////////////////////////////////////////////////////////////////////////////////////////////////////
[Serializable]
public struct BiomeData
{
    public int id;
    public string name;
    public string heightMapPath;
    public string biomeMapPath;
    public Color displayColor;

    public BiomeData(int id, string name,string heightMapPath, string biomeMapPath, Color displayColor)
    {
        this.id = id;
        this.name = name;
        this.heightMapPath = heightMapPath;
        this.biomeMapPath = biomeMapPath;
        this.displayColor = displayColor;
    }

}

[Serializable]
public struct BiomeMapData
{
    public int biomeGridSize;
    public BiomeData fullbiomeMapData;
    public BiomeData[] biomeMaps;
    public BiomeMapData(int biomeGridSize,BiomeData fullbiomeMapData, BiomeData[] biomeMaps)
    {
        this.biomeGridSize = biomeGridSize;
        this.fullbiomeMapData = fullbiomeMapData;
        this.biomeMaps = biomeMaps;
    }
}
////////////////////////////////////////////////////////////////////////////////////////////////////////