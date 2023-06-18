using System.Collections;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEngine;




[Serializable]
public class BiomeSampler
{
    public readonly int id; //id must be unique. Create a static variable that takes care of that
    public string name;

    private string heightMapPath;
    public HeightMapGenerator heightMap;
    private string biomeMapPath;
    private Texture2D biomeMap;
    
    public Color displayColor;


    public BiomeSampler(int id, Texture2D biomeMap)
    {
        this.id = id;
        this.biomeMap = biomeMap;

        UnityEngine.Random.InitState(Mathf.FloorToInt(id)); 
        float red = UnityEngine.Random.value;
        float green = UnityEngine.Random.value;
        float blue = UnityEngine.Random.value;
            
        this.displayColor = new Color(red, green, blue);
    }

    public BiomeSampler(BiomeData bData)
    {
        this.id = bData.id;

        heightMapPath = bData.heightMapPath;

        //this.heightMap = heightMap; //check if file exists before assign map



        biomeMapPath = bData.biomeMapPath;

        Texture2D tex = null;
        byte[] fileData;

        if (System.IO.File.Exists(Application.dataPath + biomeMapPath))
        {
            fileData = System.IO.File.ReadAllBytes(Application.dataPath + biomeMapPath);
            tex = new Texture2D(2, 2); //texture dimensions are resized.
            tex.LoadImage(fileData); 
        }
        else
        {
            Debug.Log("missing texture for biome: " + id);
        }
        
        this.biomeMap = tex;

        this.name = bData.name;
        this.displayColor = bData.displayColor;
        
    }

    public Color SampleBiome(float x, float y)
    {
        return biomeMap.GetPixel(Mathf.RoundToInt(x),Mathf.RoundToInt(y));
    }
    public Color SampleBiomeNearest(float x, float y)
    {
        return biomeMap.GetPixel(Mathf.RoundToInt(x),Mathf.RoundToInt(y));
    }

    public float SampleHeight(float x, float y)
    {
        return heightMap.SampleMap(x,y);
    }

    public Texture2D GetMap()
    {
        return biomeMap;
    }

    public int GetSize()
    {
        return biomeMap.width;
    }

    public BiomeData Save()
    {
        if (name == null)
        {
            name = "map" + id;
        }
        if (biomeMap != null)
        {
            biomeMapPath = "/Map/BiomeMaps/map" + id + ".png";
            System.IO.File.WriteAllBytes(Application.dataPath + biomeMapPath, biomeMap.EncodeToPNG());
        }
        else
        {
            Debug.Log("Null Map texture for sampler id = " + id);
        }

        BiomeData bData = new BiomeData(id, name, heightMapPath, biomeMapPath, displayColor);
        return bData;
    }

    /*
    public string SerializeToJSON()
    {
        BiomeData bData = new BiomeData(id, name, heightMapPath, biomeMapPath, displayColor);

        string jsonData = JsonUtility.ToJson(bData);
        return jsonData;
    }*/
    
}



public class BiomeSamplerData
{
    public readonly int gridSize;
    public readonly BiomeSampler fullBiomeMapSampler;
    public readonly List<BiomeSampler> singleBiomeSamplers;

    public BiomeSamplerData(int gridSize, BiomeSampler fullBiomeMapSampler, List<BiomeSampler> singleBiomeSamplers)
    {
        this.gridSize = gridSize;
        this.fullBiomeMapSampler = fullBiomeMapSampler;
        this.singleBiomeSamplers = singleBiomeSamplers;
    }


}

//biome baker should be a simple static class with satic methods

public static class BiomeBaker
{
    public static BiomeSamplerData LoadBaked()
    {
        if (!System.IO.File.Exists(Application.dataPath + "/Map/BiomeMaps/mapdata.json"))
        {
            return null;
        }

        string json = File.ReadAllText(Application.dataPath + "/Map/BiomeMaps/mapdata.json");
        BiomeMapData biomeMapData = JsonUtility.FromJson<BiomeMapData>(json);

        int gridSize = biomeMapData.biomeGridSize;
        BiomeSampler fullBiomeMapSampler = new BiomeSampler(biomeMapData.fullbiomeMapData);
        List<BiomeSampler> singleBiomeSamplers = new List<BiomeSampler>();


        foreach (BiomeData biomeData in biomeMapData.biomeMaps)
        {
            singleBiomeSamplers.Add(new BiomeSampler(biomeData));
        }


        return new BiomeSamplerData(gridSize, fullBiomeMapSampler, singleBiomeSamplers);
    }


    public static BiomeSampler BakeBiomeCells(BiomeMapGenerator biomeMapGenerator)
    {
        Texture2D fullMap = biomeMapGenerator.GetBiomeIndexMap();


        BiomeSampler fullMapSampler = new BiomeSampler(-1,fullMap);

        return fullMapSampler;
    }

    public static List<BiomeSampler> BakeSingleBiomes(BiomeMapGenerator biomeMapGenerator, BiomeSampler fullBiomeMap, float biomeStretch)
    {
        List<BiomeSampler> bakedBiomes = new List<BiomeSampler>();
        int size = biomeMapGenerator.gridDimension;
        for (int i = 0; i <= size * (size + 2); i++)
        {
            Texture2D singleMap = biomeMapGenerator.GetSingleBiomeMap(i, biomeStretch, fullBiomeMap);
            bakedBiomes.Add(new BiomeSampler(i, singleMap));
        }
        
        return bakedBiomes;
        
    }

    public static void SaveBaked(int biomeGridSize, BiomeSampler fullBiomeMap, List<BiomeSampler> bakedBiomes)
    {
        BiomeData fullBiomeMapData;

        if (fullBiomeMap != null)
        {
            fullBiomeMapData = fullBiomeMap.Save();
        }
        else
        {
            Debug.Log("Full biome map is null");
            return;
        }

        BiomeData[] biomeMaps = new BiomeData[bakedBiomes.Count];

        for (int i = 0; i < (bakedBiomes.Count); i++)
        {
            biomeMaps[i] = bakedBiomes[i].Save();
        }
        
        BiomeMapData bmd = new BiomeMapData(biomeGridSize,fullBiomeMapData,biomeMaps);

        File.WriteAllText(Application.dataPath + "/Map/BiomeMaps/mapdata.json", JsonUtility.ToJson(bmd,true));


    }

}
