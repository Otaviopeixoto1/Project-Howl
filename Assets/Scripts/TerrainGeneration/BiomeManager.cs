using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//assign the biomes here. Make a graph structure for neighbouring biomes. When sampling the biome
//edges it should be enough to only sample neighbouring biome maps instead of all maps at once


public class BiomeLinks //add the links to the biome map data as an array off biome indexes
{
    //store the biome graph structure
}


public class BiomeManager : MonoBehaviour
{

    private BiomeSampler fullBiomeMap;

    [NonReorderable]
    [SerializeField]
    private List<BiomeSampler> biomeSamplers;

    private int biomeGridSize;


    public void GenerateMap()
    {
        //Generate the biomeMapGenerator and pass to BiomeBaker to generate all biome cells 
        return;
    }

    public bool Load()
    {
        BiomeSamplerData loadedSamplerData = BiomeBaker.LoadBaked(); 

        if (loadedSamplerData != null && loadedSamplerData.singleBiomeSamplers.Count > 1)
        {
            biomeSamplers = loadedSamplerData.singleBiomeSamplers;
            fullBiomeMap = loadedSamplerData.fullBiomeMapSampler;
            biomeGridSize = loadedSamplerData.gridSize;
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
        if (biomeSamplers == null || fullBiomeMap == null)
        {
            return false;
        }
        BiomeBaker.SaveBaked(biomeGridSize, fullBiomeMap, biomeSamplers);
        return true;
    }

    public void AssingBiomes()
    {
        //assing random biomes to each cell, based on the map position and neighbours
    }




    public void DisplayMap()
    {

    }




    void Start()
    {
        //check if a mapdata.json exists and if not. create the map from scratch (or look for a backup)
        //if the data doesnt exist, try to generate using bake and save methods from BiomeBaker
        if(Load())
        {
            return;
        }
        else
        {
            //look for backups or:
            GenerateMap();
        }
    }

    void Update()
    {
        
    }
}
