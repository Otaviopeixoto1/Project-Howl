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


    //Graph structure used to store information about neighbouring biomes (maybe blending between them):
    private BiomeLinks biomeLinks; 




    //use the world seed here as well
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
        biomeLinks.GenerateLinksFromGrid(); 
    }

    public bool Load()
    {
        BiomeMapBaker.BiomeSamplersData loadedSamplerData = BiomeMapBaker.LoadBaked(); 

        if (loadedSamplerData != null && loadedSamplerData.singleBiomeSamplers.Count > 1)
        {
            biomeSamplers = loadedSamplerData.singleBiomeSamplers;
            biomeIdSampler = loadedSamplerData.biomeIdSampler;
            biomeGridSize = loadedSamplerData.gridSize;
            biomeLinks = loadedSamplerData.biomeLinks;

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
        BiomeMapBaker.SaveBaked(biomeGridSize, biomeIdSampler, biomeSamplers, biomeLinks:biomeLinks);
        return true;
    }


    public int[] GetNeighbours(int index)
    {
        return biomeLinks.GetLinks(index);
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
