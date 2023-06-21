using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BiomeManager : MonoBehaviour
{
    [SerializeField]
    private WorldGenerationSettings worldGenerationSettings;
    [SerializeField]
    private WorldTopographyGenerator worldTopographyGenerator; 


    private BiomeSampler biomeIdSampler;

    [NonReorderable]
    [SerializeField]
    private List<BiomeSampler> biomeSamplers;

    
    [HideInInspector]
    //Amount of biome cells on x and y. Default value = 4
    public int biomeGridSize = 4;

    private BiomeLinks biomeLinks; // serialize and save the biome links in a file






    public void GenerateBiomeMap()
    {
        //Generate the biomeMapGenerator and pass to BiomeBaker to generate all biome cells 
        return;
    }

    public void AssingBiomes()
    {
        
        worldGenerationSettings.biomeGridSize = biomeGridSize; //just avoiding conflicts
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
        biomeLinks.GenerateLinksFromGrid(); //Serialize to Json file
    }

    public bool Load()
    {
        BiomeSamplerData loadedSamplerData = BiomeMapBaker.LoadBaked(); 

        if (loadedSamplerData != null && loadedSamplerData.singleBiomeSamplers.Count > 1)
        {
            biomeSamplers = loadedSamplerData.singleBiomeSamplers;
            biomeIdSampler = loadedSamplerData.fullBiomeMapSampler;
            biomeGridSize = loadedSamplerData.gridSize;

            AssingBiomes(); // REMOVE after proper biomeData serialization

            return true;
        }
        else
        {
            Debug.Log("Error Loading BiomeSamplers");      
            return false; 
        }
        
    }
    public bool Save()
    {
        if (biomeSamplers == null || biomeIdSampler == null)
        {
            return false;
        }
        BiomeMapBaker.SaveBaked(biomeGridSize, biomeIdSampler, biomeSamplers);
        return true;
    }


    public int[] GetNeighbours(int index)
    {
        return biomeLinks.neighbours[index];
    }

    public BiomeSampler GetBiomeIdSampler()
    {
        return biomeIdSampler;
    }
    public BiomeSampler GetBiomeSampler(int index)
    {
        return biomeSamplers[index];   
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
            //Generate biomelinks
        }
    }
}
