using System.Collections;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEngine;



/// <summary>
/// Class used to store data of biome cells as well as provide functions to sample data based on 2D Map/Texture Coordinates
/// </summary>
[Serializable]
public class BiomeSampler
{
    public readonly int id; //id must be unique
    public string name;
    public Biomes biomeType = Biomes.Forest;
    public Color displayColor;


    private string heightMapPath;
    public HeightMapGenerator heightMap;
    private string biomeMapPath;

    [NonSerialized]
    private Texture2D biomeMap; // Remove the texture, just use the array

    [NonSerialized]
    public Color[] biomeMapThreaded;

    [NonSerialized]
    public int mapSize;


    public BiomeSampler(int id, Texture2D biomeMap)
    {
        this.id = id;
        this.biomeMap = biomeMap;
        this.mapSize = biomeMap.width;
        this.biomeMapThreaded = biomeMap.GetPixels();

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

        if (System.IO.File.Exists(Application.dataPath + heightMapPath))
        {
            string json = File.ReadAllText(Application.dataPath + heightMapPath);
            heightMap = ScriptableObject.CreateInstance<HeightMapGenerator>();
            JsonUtility.FromJsonOverwrite(json, heightMap);
        }
        else if (id > 0)
        {
            Debug.Log("missing heightMap for biome: " + id);
        }


        biomeMapPath = bData.biomeMapPath;

        Texture2D tex = null;
        byte[] texData;

        if (System.IO.File.Exists(Application.dataPath + biomeMapPath))
        {
            texData = System.IO.File.ReadAllBytes(Application.dataPath + biomeMapPath);
            tex = new Texture2D(2, 2); //texture dimensions are resized on load.
            tex.LoadImage(texData); 
        }
        else
        {
            Debug.Log("missing map texture for biome: " + id);
        }
        
        this.biomeMapThreaded = tex.GetPixels();
        this.mapSize = tex.width;


        this.biomeMap = tex; //remove this property

        this.name = bData.name;
        this.biomeType = bData.biomeType;
        this.displayColor = bData.displayColor;
        
    }

    /// <summary>
    /// Samples data encoded into a Color from the BiomeSampler's biome map. Uses Bilinear filtering
    /// </summary>
    public Color SampleBiome(float x, float y)
    {
        int x0 = Mathf.FloorToInt(x);
        int x1 = x0;
        if (x0 + 1 < mapSize)
        {
           x1 += 1; 
        }
        
        int y0 =  Mathf.FloorToInt(y);
        int y1 = y0;
        if (y0 + 1 < mapSize)
        {
           y1 += 1; 
        }

        Color s00 = biomeMapThreaded[ x0 + y0 * mapSize];
        Color s10 = biomeMapThreaded[ x1 + y0 * mapSize];
        Color s01 = biomeMapThreaded[ x0 + y1 * mapSize];
        Color s11 = biomeMapThreaded[ x1 + y1 * mapSize];

        float w00 = (x1 - x) * (y1 - y);
        float w10 = (x - x0) * (y1 - y);
        float w01 = (x1 - x) * (y - y0);
        float w11 = (x - x0) * (y - y0);

        return w00 * s00 + w01 * s01 + w10 * s10 + w11 * s11;
    }

    /// <summary>
    /// Samples data encoded into a Color from the BiomeSampler's biome map. Uses Nearest/Point filtering
    /// </summary>
    public Color SampleBiomeNearest(float x, float y)
    {
        //return biomeMap.GetPixel(Mathf.RoundToInt(x),Mathf.RoundToInt(y));
        return biomeMapThreaded[Mathf.RoundToInt(x) + Mathf.RoundToInt(y) * mapSize];
    }

    /// <summary>
    /// Samples the HeightMap generated dynamicaly by the BiomeSampler's HeightMapGenerator 
    /// </summary>
    public float SampleHeight(float x, float y)
    {
        return heightMap.SampleMap(x,y);
    }

    public Texture2D GetMap()
    {
        return biomeMap;
    }

    /// <summary>
    /// Returns the width (same as height) in pixels of the BiomeSampler's biome map
    /// </summary>
    public int GetSize()
    {
        return mapSize;
    }

    /// <summary>
    /// Serializes the HeightMap into JSON named /Map/HeightMaps/heightgen{id}.json and BiomeMap into a texture named /Map/BiomeMaps/map{id}.png
    /// </summary>
    public BiomeData Save(bool saveHeightMap = true)
    {
        if (name == null)
        {
            name = "map" + id;
        }
        if (biomeMap != null)
        {
            biomeMapPath = "/Map/BiomeMaps/map" + id + ".png";
            File.WriteAllBytes(Application.dataPath + biomeMapPath, biomeMap.EncodeToPNG());
        }
        else
        {
            Debug.Log("Null Map texture for sampler id = " + id);
        }

        if (saveHeightMap && heightMap != null)
        {
            heightMapPath = "/Map/HeightMaps/heightgen" + id + ".json";
            File.WriteAllText(Application.dataPath + heightMapPath, JsonUtility.ToJson(heightMap,true));
        }
        else if (saveHeightMap)
        {
            Debug.Log("Null height map generator reference for sampler id = " + id);
        }


        BiomeData bData = new BiomeData(id, name, biomeType, heightMapPath, biomeMapPath, displayColor);
        return bData;
    }
}



/// <summary>
/// Static class responsible for baking the biome map data and saving all necessary data and filepaths into the /Map/BiomeMaps/mapdata.json
/// </summary>
public static class BiomeMapBaker
{
    /// <summary>
    /// Static class responsible for passing the loaded data from the mapdata.json file to the map generation pipeline
    /// </summary>
    public class BiomeSamplersData
    {
        public readonly int gridSize;
        public readonly BiomeSampler biomeIdSampler;
        public readonly List<BiomeSampler> singleBiomeSamplers;
        public readonly BiomeLinks biomeLinks;

        public BiomeSamplersData(int gridSize, BiomeSampler biomeIdSampler, List<BiomeSampler> singleBiomeSamplers, BiomeLinks biomeLinks)
        {
            this.gridSize = gridSize;
            this.biomeIdSampler = biomeIdSampler;
            this.singleBiomeSamplers = singleBiomeSamplers;
            this.biomeLinks = biomeLinks;
        }

    }

    /// <summary>
    /// Load data from /Map/BiomeMaps/mapdata.json and returns a BiomeSamplersData struct 
    /// </summary>
    public static BiomeSamplersData LoadBaked()
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


        return new BiomeSamplersData(gridSize, fullBiomeMapSampler, singleBiomeSamplers, biomeMapData.biomeLinks);
    }

    /// <summary>
    /// Bakes cell ids information of the entire biome map into one single BiomeSampler
    /// </summary>
    public static BiomeSampler BakeBiomeCellIds(BiomeMapGenerator biomeMapGenerator)
    {
        Texture2D fullMap = biomeMapGenerator.GetBiomeIndexMap();


        BiomeSampler fullMapSampler = new BiomeSampler(-1,fullMap);

        return fullMapSampler;
    }

    /// <summary>
    /// Bakes the biome cell information, including the map region occupied by that cell (stored as a distance field).
    /// Aditonally, the Baking process can control how much the individual biome regions are streched streched in order to cause more overlapping between them
    /// </summary>

    public static List<BiomeSampler> BakeSingleBiomes(BiomeMapGenerator biomeMapGenerator, BiomeSampler fullBiomeMap, float biomeStretch)
    {
        List<BiomeSampler> bakedBiomes = new List<BiomeSampler>();
        int size = biomeMapGenerator.gridDimension;
        for (int i = 0; i <= size * (size + 2); i++)
        {
            Texture2D singleMap = biomeMapGenerator.GetSingleBiomeMap(i, biomeStretch, fullBiomeMap, size);
            bakedBiomes.Add(new BiomeSampler(i, singleMap));
        }
        
        return bakedBiomes;
        
    }

    /// <summary>
    /// Saves all baked information into the mapdata.json file
    /// </summary>
    public static void SaveBaked(int biomeGridSize, BiomeSampler fullBiomeMap, List<BiomeSampler> bakedBiomes, BiomeLinks biomeLinks = null, bool saveHeights = true)
    {
        BiomeData fullBiomeMapData;

        if (biomeLinks == null)
        {
            Debug.Log("Biome links is null");
        }

        if (fullBiomeMap != null)
        {
            fullBiomeMapData = fullBiomeMap.Save(false);
        }
        else
        {
            Debug.Log("Full biome map is null");
            return;
        }

        BiomeData[] biomeMaps = new BiomeData[bakedBiomes.Count];

        for (int i = 0; i < (bakedBiomes.Count); i++)
        {
            biomeMaps[i] = bakedBiomes[i].Save(saveHeights);
        }
        
        BiomeMapData bmd = new BiomeMapData(biomeGridSize,fullBiomeMapData,biomeMaps, biomeLinks);

        File.WriteAllText(Application.dataPath + "/Map/BiomeMaps/mapdata.json", JsonUtility.ToJson(bmd,true));


    }

}
