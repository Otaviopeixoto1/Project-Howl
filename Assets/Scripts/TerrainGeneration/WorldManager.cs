using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class used to manage the biome generation and assigning each biome cell a unique BiomeSampler
/// </summary>

//Change name: WorldManager
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

    private WorldSampler worldSampler;


    public WorldSampler GetWorldSampler()
    {
        return worldSampler;
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
        biomeMapGenerator.cellularJitter = 0.5f; //use the WorldGenerationSettings
        biomeMapGenerator.ApplySettings();

        BiomeSampler biomeIdSampler = BiomeMapBaker.BakeBiomeCellIds(biomeMapGenerator);
        List<BiomeSampler> biomeSamplers = BiomeMapBaker.BakeSingleBiomes(biomeMapGenerator, biomeIdSampler, 1.2f);

        worldSampler = new WorldSampler(biomeIdSampler, biomeSamplers ,worldGenerationSettings, worldTopographyGenerator);

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

            worldSampler = new WorldSampler(biomeIdSampler, biomeSamplers, biomeLinks, worldGenerationSettings);
            //Check for missing data
            //if missing, generate again. If cant generate, return false


            return true;
        }
        else
        {
            Debug.Log("Error Loading BiomeSamplers");      
            return false; 
        }
        
    }


    /// <summary>
    /// Saves the previously Baked map data. Returns true if the saving was successful and false otherwise
    /// </summary>
    public bool Save()
    {
        BiomeSampler biomeIdSampler = worldSampler.biomeIdSampler;
        List<BiomeSampler> biomeSamplers = worldSampler.biomeSamplers;

        if (biomeSamplers == null || biomeIdSampler == null)
        {
            return false;
        }
        Debug.Log("Saving Data");
        BiomeMapBaker.SaveBaked(worldSampler.biomeGridSize, biomeIdSampler, biomeSamplers, biomeLinks: worldSampler.biomeLinks);
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
