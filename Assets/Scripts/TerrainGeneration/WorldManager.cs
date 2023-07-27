using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class used to manage the world generation 
/// </summary>

//Manage the setup for generation settings as well as weather (clouds), world events, etc ...

public class WorldManager : MonoBehaviour
{
    public delegate void LoadEvent();
    public static event LoadEvent OnBiomeMapGeneration;
    public static event LoadEvent OnSave;
    public static event LoadEvent OnSuccessfulLoad;


    [SerializeField]
    private WorldGenerationSettings worldGenerationSettings;
    [SerializeField]
    private WorldTopographyGenerator worldTopographyGenerator; 

    private WorldGenerator worldGenerator;


    public WorldGenerator GetWorldGenerator()
    {
        return worldGenerator;
    }

    /// <summary>
    /// Instances a BiomeMapGenerator and bakes the cell distance fields into separate BiomeSamplers
    /// </summary>
    public void GenerateBiomeMap()
    {
        Debug.Log("Generating Biome Map");
        BiomeMapGenerator biomeMapGenerator = ScriptableObject.CreateInstance<BiomeMapGenerator>();
        biomeMapGenerator.cellularSeed = WorldGenerationSettings.worldSeed;
        biomeMapGenerator.gridDimension = worldGenerationSettings.biomeGridSize; 
        biomeMapGenerator.cellularJitter = worldGenerationSettings.biomeCellJitter; 
        biomeMapGenerator.ApplySettings();

        BiomeSampler biomeIdSampler = BiomeMapBaker.BakeBiomeCellIds(biomeMapGenerator);
        List<BiomeSampler> biomeSamplers = BiomeMapBaker.BakeSingleBiomes(biomeMapGenerator, biomeIdSampler, 1.2f);

        //the biomes should be assigned here !!!
        AssignBiomes(biomeSamplers, worldGenerationSettings.biomeGridSize);
        


        worldGenerator = new WorldGenerator(biomeIdSampler, biomeSamplers, worldGenerationSettings);

    }

    /// <summary>
    /// Assigns the biomes and heightmaps of each of the previously generated BiomeSamplers
    /// </summary>
    public void AssignBiomes(List<BiomeSampler> biomeSamplers, int biomeGridSize)
    {
        worldGenerationSettings.ApplyGenerationSettings();

        if (biomeSamplers.Count == (biomeGridSize + 1) * (biomeGridSize + 1))
        {
            for (int i = 0; i < biomeSamplers.Count; i++)
            {
                TopographySettings topographySettings = worldGenerationSettings.GetTopographySettings(i);

                biomeSamplers[i].biomeType = topographySettings.biomeType;
                biomeSamplers[i].heightMap = worldTopographyGenerator.GetHeightMapGenerator(topographySettings);
            }
        }
        else
        {
            Debug.Log("biomeGridSize is incompatible with the number of biome samplers");
        }

    }


    /// <summary>
    /// Loads the previously saved map data. Returns true if the loading was successful and false otherwise
    /// </summary>
    public bool Load()
    {
        Debug.Log("Loading Data");
        BiomeMapBaker.BiomeSamplersData loadedSamplerData = BiomeMapBaker.LoadBaked(); 

        if (loadedSamplerData != null && loadedSamplerData.singleBiomeSamplers.Count > 1)
        {
            BiomeSampler biomeIdSampler = loadedSamplerData.biomeIdSampler;
            List<BiomeSampler> biomeSamplers = loadedSamplerData.singleBiomeSamplers;
            //biomeGridSize = loadedSamplerData.gridSize;
            BiomeLinks biomeLinks = loadedSamplerData.biomeLinks;

            worldGenerator = new WorldGenerator(biomeIdSampler, biomeSamplers, biomeLinks, worldGenerationSettings);
            //Check for missing data
            //if missing, generate again. If cant generate, return false


            return true;
        }
        else
        {
            Debug.Log("Unable to load map data");      
            return false; 
        }
        
    }


    /// <summary>
    /// Saves the previously Baked map data. Returns true if the saving was successful and false otherwise
    /// </summary>
    public bool Save()
    {
        BiomeSampler biomeIdSampler = worldGenerator.biomeIdSampler;
        List<BiomeSampler> biomeSamplers = worldGenerator.biomeSamplers;

        if (biomeSamplers == null || biomeIdSampler == null)
        {
            return false;
        }
        Debug.Log("Saving Data");
        BiomeMapBaker.SaveBaked(worldGenerator.biomeGridSize, biomeIdSampler, biomeSamplers, biomeLinks: worldGenerator.biomeLinks);
        return true;
    }



    

    void Start()
    {
        if(Load())
        {
            Debug.Log("Successful Load");
            OnSuccessfulLoad();
        }
        else
        {
            //look for backups or:
            GenerateBiomeMap();
            //OnBiomeMapGeneration();
            Save();
            //OnSave();
            Load(); // has to be loaded again due to bug
            OnSuccessfulLoad();
        }
    }
}
