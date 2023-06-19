using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Biomes
{

}

//save, load, assign biomes
public class BiomeManager : MonoBehaviour
{
    private int biomeGridSize;

    private BiomeSampler fullBiomeMap;

    [NonReorderable]
    [SerializeField]
    private List<BiomeSampler> biomeSamplers;
    private BiomeLinks biomeLinks; // serialize and save the biome links in a file

    [SerializeField]
    [Range(0.01f,5f)]
    private float heightMapScale = 1f;



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
            heightMapGenerator.amplitude = Random.Range(1f, 5.0f);
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
            fullBiomeMap = loadedSamplerData.fullBiomeMapSampler;
            biomeGridSize = loadedSamplerData.gridSize;

            biomeLinks = new BiomeLinks(biomeGridSize);
            biomeLinks.GenerateLinksFromGrid();
            //biomeLinks.Print();
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


    public int[] GetNeighbours(int index)
    {
        return biomeLinks.neighbours[index];
    }

    public BiomeSampler GetFullBiomeSampler()
    {
        return fullBiomeMap;
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
