using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class used to manage the biome generation and assigning each biome cell a unique BiomeSampler
/// </summary>

//Change name: WorldManager
public class BiomeManager : MonoBehaviour
{
    [SerializeField]
    private WorldGenerationSettings worldGenerationSettings;
    [SerializeField]
    private WorldTopographyGenerator worldTopographyGenerator; 


    private BiomeSampler biomeIdSampler;

    [NonReorderable]
    [SerializeField]
    private List<BiomeSampler> biomeSamplers; //convert to array

    
    //Amount of biome cells on x and y. Default value = 4
    public const int biomeGridSize = 4;

    //Graph structure used to store information about neighbouring biomes (maybe blending between them):
    private BiomeLinks biomeLinks; 
    





    /// <summary>
    /// Instances a BiomeMapGenerator and bakes the cell distance fields into separate BiomeSamplers
    /// </summary>
    public void GenerateBiomeMap()
    {
        Debug.Log("Generating Biome Map");
        BiomeMapGenerator biomeMapGenerator = ScriptableObject.CreateInstance<BiomeMapGenerator>();
        biomeMapGenerator.cellularSeed = WorldGenerationSettings.worldSeed;
        biomeMapGenerator.gridDimension = biomeGridSize; //use the one from WorldGenerationSettings
        biomeMapGenerator.cellularJitter = 0.5f; //use the WorldGenerationSettings
        biomeMapGenerator.ApplySettings();

        biomeIdSampler = BiomeMapBaker.BakeBiomeCellIds(biomeMapGenerator);

                                                        //use the strech from WorldGenerationSettings
        biomeSamplers = BiomeMapBaker.BakeSingleBiomes(biomeMapGenerator, biomeIdSampler, 1.2f);
        Debug.Log(biomeGridSize + " " + biomeSamplers.Count);

    }


    /// <summary>
    /// Assigns the biomes and heightmaps of each of the previously generated BiomeSamplers
    /// </summary>
    public void AssingBiomes()
    {
        Debug.Log("Assigning Biomes");
        worldGenerationSettings.biomeGridSize = biomeGridSize; //use the one fro WorldGenSettings
        worldGenerationSettings.Apply();
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

        biomeLinks = new BiomeLinks(biomeGridSize);
        biomeLinks.GenerateLinksFromGrid(); 
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
            biomeSamplers = loadedSamplerData.singleBiomeSamplers;
            biomeIdSampler = loadedSamplerData.biomeIdSampler;
            //biomeGridSize = loadedSamplerData.gridSize;
            biomeLinks = loadedSamplerData.biomeLinks;


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
        if (biomeSamplers == null || biomeIdSampler == null)
        {
            return false;
        }
        Debug.Log("Saving Data");
        BiomeMapBaker.SaveBaked(biomeGridSize, biomeIdSampler, biomeSamplers, biomeLinks:biomeLinks);
        return true;
    }


    /// <summary>
    /// Returns the indexes of all the biome cells linked (neighbouring) to the cell of index: id  
    /// </summary>
    public int[] GetNeighbours(int id)
    {
        return biomeLinks.GetLinks(id);
    }


    /// <summary>
    /// Returns the Baked sampler with the information of the full biome map and the cell ids of each biome cell
    /// </summary>
    public BiomeSampler GetBiomeIdSampler()
    {
        return biomeIdSampler;
    }


    /// <summary>
    /// Returns the sampler of a biome cell with index: id
    /// </summary>
    public BiomeSampler GetBiomeSampler(int id)
    {
        return biomeSamplers[id];   
    }

    

    void Start()
    {
        //check if a mapdata.json exists and if not. create the map from scratch (or look for a backup)
        //if the data doesnt exist, try to generate using bake and save methods from BiomeBaker
        
        
        if(Load())
        {
            // add some signal or callback
        }
        else
        {
            //look for backups or:
            GenerateBiomeMap();
            AssingBiomes();
            Save();
            Load(); // has to be loaded again due to bug
        }
    }
}
