using System;
using System.Collections.Generic;
using UnityEngine;

public enum WorldMapType
{
    Default, //biomeGridSize x biomeGridSize voronoi grid with HighMountais biome on the edges
    Random, //Randomly assigns biomes in a biomeGridSize x biomeGridSize grid
}


public struct GlobalGenerationSettings
{
    public readonly Material defaultDetailMaterial;
    public readonly int subChunkSubdivision;
    public GlobalGenerationSettings(Material defaultDetailMaterial, int subChunkSubdivision)
    {
        this.defaultDetailMaterial = defaultDetailMaterial;
        this.subChunkSubdivision = subChunkSubdivision;
    }
}


//Stores the information about the map style, as well as information for generating heightmaps of 
//each biome as well as the terrain structures and terrain details (grass, rocks, ...)

[CreateAssetMenu(fileName = "WorldGenerationSettings", menuName = "ScriptableObjects/WorldGeneration/WorldGenerationSettings", order = 0)] 
public class WorldGenerationSettings : ScriptableObject
{
    public static int worldSeed = 340;
    
    public WorldMapType worldMapType = WorldMapType.Default;


    [Header("Biome Settings")]
    public int biomeGridSize = 4; // only makes sense if its a grid world type (WorldMapType.Default)

    public int biomeMapSize = 240;

    [Range(0.01f,1)]
    public float biomeMapScale = 0.05f;

    [Range(0,1)]
    public float biomeCellJitter = 0.5f;


    
    public List<BiomeSettings> biomeGenerationSettings = new List<BiomeSettings>();
    private BiomeSettings[] worldSettings;

    [Header("Global Generation Settings")]
    public int subChunkSubdivision = 3;
    public Material defaultDetailMaterial;



    public Dictionary<Biomes,BiomeSettings> GetAvailableBiomes() // get a list of all biomes in biomeGenerationSettings
    {
        Dictionary<Biomes,BiomeSettings> availableBiomes = new Dictionary<Biomes,BiomeSettings>();
        foreach (BiomeSettings bSettings in biomeGenerationSettings)
        {
            if (!availableBiomes.ContainsKey(bSettings.biome))
            {
                availableBiomes.Add(bSettings.biome,bSettings);
            }
        }
        return availableBiomes;
    }

    //always apply before requesting for world generation data
    public void ApplyGenerationSettings()
    {
        UnityEngine.Random.InitState(worldSeed);
        Dictionary<Biomes,BiomeSettings> availableBiomes = GetAvailableBiomes();
        Debug.Log("assingning new biomes. Seed = " + worldSeed);

        switch (worldMapType)
        {
            case WorldMapType.Default:
                worldSettings = new BiomeSettings[(biomeGridSize + 1) * (biomeGridSize + 1)];

                BiomeSettings edgeBiomeSettings;
                //get all non-edge biomes
                List<Biomes> centralBiomes = new List<Biomes>(availableBiomes.Keys); 

                if (availableBiomes.ContainsKey(Biomes.HighMountains))
                {
                    edgeBiomeSettings = availableBiomes[Biomes.HighMountains];
                    centralBiomes.RemoveAll(x => x == Biomes.HighMountains);
                }
                else
                {
                    Debug.Log("HighMountains Settings definition not found, changing to default");
                    //create a hardcoded (default) highmoutains setting
                    edgeBiomeSettings = BiomeSettings.HighMountains;
                }
                if (centralBiomes.Count < 1)
                {
                    //Catch this inside the world generation call
                    throw new Exception("Missing BiomeSettings definitions!");
                }

                //assign the edge biome
                for (int i = 0; i <= biomeGridSize; i++)
                {
                    worldSettings[i] = edgeBiomeSettings;
                    worldSettings[i * (biomeGridSize + 1)] = edgeBiomeSettings;
                    worldSettings[biomeGridSize + i * (biomeGridSize + 1)] = edgeBiomeSettings;
                    worldSettings[i + biomeGridSize * (biomeGridSize + 1)] = edgeBiomeSettings;
                }

                //assign the central biomes
                int n = centralBiomes.Count - 1;
                for (int y = 1; y < biomeGridSize; y++)
                {
                    for (int x = 1; x < biomeGridSize; x++)
                    {
                        Biomes randomBiome = RandomStackSelect(centralBiomes, n);
                        worldSettings[x + y * (biomeGridSize + 1)] = availableBiomes[randomBiome];
                        n -= 1;
                        if (n < 0)
                        {
                            n = centralBiomes.Count - 1;
                        }
                    }
                }

                return;
            case WorldMapType.Random:
                worldSettings = new BiomeSettings[(biomeGridSize + 1) * (biomeGridSize + 1)];
                List<Biomes> allBiomes = new List<Biomes>(availableBiomes.Keys); 

                if (allBiomes.Count < 1)
                {
                    //Catch this inside the world generation call
                    throw new Exception("Missing BiomeSettings definitions!");
                }

                int t = allBiomes.Count - 1;
                for (int y = 0; y <= biomeGridSize; y++)
                {
                    for (int x = 0; x <= biomeGridSize; x++)
                    {
                        Biomes randomBiome = RandomStackSelect(allBiomes, t);
                        worldSettings[x + y * (biomeGridSize + 1)] = availableBiomes[randomBiome];
                        t -= 1;
                        if (t < 0)
                        {
                            t = allBiomes.Count - 1;
                        }
                    }
                }
                return;

        } 
    }

    //selects a random element from the list and moves int to the back;
    private Biomes RandomStackSelect(List<Biomes> list, int end, int start = 0)
    {
        if (start < 0)
        {
            start = 0;
        }
        if (end > list.Count - 1)
        {
            end = list.Count- 1;
        }
        int randomIndex = UnityEngine.Random.Range(start, end);
        Biomes randomElement = list[randomIndex];

        Biomes tmp = list[list.Count - 1];
        list[list.Count - 1] = randomElement;
        list[randomIndex] = tmp;

        return randomElement;
    }

    public GlobalGenerationSettings GetGlobalGenerationSettings()
    {
        return new GlobalGenerationSettings(defaultDetailMaterial, subChunkSubdivision);
    }

    
    //gets the topography settings based on cell index. Only call after apply
    public TopographySettings GetTopographySettings(int index)
    {
        //return the topography settings. we need to apply the settings to generate the link between
        //the cell index and the biome settings
        return worldSettings[index].GetTopographySettings();
    }

    public PrimaryGenerationSettings GetPrimarySettings(int index)
    {
        return worldSettings[index].GetPrimarySettings();
    }

    public TerrainDetailSettings GetDetailSettings(int index)
    {
        return worldSettings[index].terrainDetailSettings;
    }

    public Dictionary<Biomes, TerrainDetailSettings> GetAllDetailSettings()
    {
        Dictionary<Biomes,TerrainDetailSettings> detailGenSettings = new Dictionary<Biomes, TerrainDetailSettings>();

        foreach(BiomeSettings settings in biomeGenerationSettings)
        {
            detailGenSettings[settings.biome] = settings.terrainDetailSettings;
        }
        return detailGenSettings;
    }


    private bool IsOnEdge(int index)
    {
        return false;
    }




    void OnValidate()
    {

    }

}
