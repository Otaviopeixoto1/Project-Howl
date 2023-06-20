using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Biomes
{

}

public enum BiomeGenerationMode
{
    Random,
    Standard
}


//save, load, assign biomes
public class BiomeManager : MonoBehaviour
{
    [SerializeField]
    private BiomeGenerationMode biomeGenerationMode = BiomeGenerationMode.Random;

    private BiomeSampler biomeIdSampler;
    [NonReorderable]
    [SerializeField]
    private List<BiomeSampler> biomeSamplers;

    [SerializeField]
    [Range(0.01f,5f)]
    private float heightMapScale = 1f;


    //Amount of biome cells on x and y. Default value = 4
    [HideInInspector]
    public int biomeGridSize;
    private BiomeLinks biomeLinks; // serialize and save the biome links in a file



    public void GenerateBiomeMap()
    {
        //Generate the biomeMapGenerator and pass to BiomeBaker to generate all biome cells 
        return;
    }

    public void AssingBiomes()
    {
        //assing random biomes to each cell, based on the map position and neighbours
    }

    public void GenerateHeightMaps()
    {
        for (int i = 0; i <= biomeGridSize * (biomeGridSize + 2); i++)
        {
            HeightMapGenerator heightMapGenerator = ScriptableObject.CreateInstance<HeightMapGenerator>();
            heightMapGenerator.frequency = Random.Range(0.01f, 50.0f);
            heightMapGenerator.amplitude = Random.Range(1f, 20.0f);
            heightMapGenerator.mapScale = heightMapScale;
            biomeSamplers[i].heightMap = heightMapGenerator;
        }
    }

    public bool Load()
    {
        BiomeSamplerData loadedSamplerData = BiomeBaker.LoadBaked(); 

        if (loadedSamplerData != null && loadedSamplerData.singleBiomeSamplers.Count > 1)
        {
            biomeSamplers = loadedSamplerData.singleBiomeSamplers;
            biomeIdSampler = loadedSamplerData.fullBiomeMapSampler;
            biomeGridSize = loadedSamplerData.gridSize;

            biomeLinks = new BiomeLinks(biomeGridSize);
            biomeLinks.GenerateLinksFromGrid();

            GenerateHeightMaps(); // REMOVE after propper biome assignment

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
        BiomeBaker.SaveBaked(biomeGridSize, biomeIdSampler, biomeSamplers);
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
            GenerateHeightMaps();
        }
        else
        {
            //look for backups or:
            GenerateBiomeMap();
            AssingBiomes();
            GenerateHeightMaps();
            //Generate biomelinks
        }
    }
}
