using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum Biomes
{

}

public class BiomeManager : MonoBehaviour
{
    private int biomeGridSize;
    private BiomeSampler fullBiomeMap;

    [NonReorderable]
    [SerializeField]
    private List<BiomeSampler> biomeSamplers;
    private BiomeLinks biomeLinks; // serialize and save the biome links in a file

    public void GenerateBiomeMap()
    {
        //Generate the biomeMapGenerator and pass to BiomeBaker to generate all biome cells 
        return;
    }

    public void SetBiomeHeightMap(int index, HeightMapGenerator heightMapGenerator)
    {
        if ((biomeSamplers != null) && (index >= 0) && (index < biomeSamplers.Count))
        {
            biomeSamplers[index].heightMap = heightMapGenerator;
        }
    }

    public int GetGridSize()
    {
        return biomeGridSize;
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

    public void AssingBiomes()
    {
        //assing random biomes to each cell, based on the map position and neighbours
    }

    

    //this method will be a problem in threads because of heightCurves. 
    //Implement heightCurves from Scratch
    public float SampleHeight(float x, float y) 
    {
                                            //use encoder/decoder for these values
        int cellId = Mathf.RoundToInt(fullBiomeMap.SampleBiomeNearest(x,y).r * 24f);
        float cellValue = biomeSamplers[cellId].SampleBiome(x,y).r;
        float finalHeight = biomeSamplers[cellId].SampleHeight(x,y) * cellValue; 
                                        //use the width and height offset for sampling heightmap
        float totalValue = cellValue;


        if (cellValue < 0.9)
        {
            foreach (int neighbourId in biomeLinks.neighbours[cellId])
            {
                float nCellValue = biomeSamplers[neighbourId].SampleBiome(x,y).r;
                float nheight = biomeSamplers[neighbourId].SampleHeight(x,y) * nCellValue;
                totalValue += nCellValue;
                finalHeight += nheight;
            }
        }
        finalHeight /= totalValue;

        return finalHeight;
    }

    public Color SampleColor(float x, float y)
    {
                                                    //use encoder/decoder for these values
        int cellId = Mathf.RoundToInt(fullBiomeMap.SampleBiomeNearest(x,y).r * 24f);
        float cellValue = biomeSamplers[cellId].SampleBiome(x,y).r;
        Color finalColor = biomeSamplers[cellId].displayColor * cellValue;
        float totalValue = cellValue;


        if (cellValue < 0.9)
        {
            foreach (int neighbourId in biomeLinks.neighbours[cellId])
            {
                float nCellValue = biomeSamplers[neighbourId].SampleBiome(x,y).r;
                Color nColor = biomeSamplers[neighbourId].displayColor * nCellValue;
                totalValue += nCellValue;
                finalColor += nColor;
            }
        }
        finalColor /= totalValue;

        return finalColor;
    }



    public void DisplayMap()
    {
        MeshRenderer mapRenderer = GetComponent<MeshRenderer>();
        if (mapRenderer == null || biomeSamplers == null || fullBiomeMap == null)
        {
            return;
        }
        Texture2D texture = new Texture2D(241,241);

        Color[] colorMap = new Color[241 * 241];

        for (int x = 0; x < 241; x++)
        {
            for (int y = 0; y < 241; y++)
            {
                
                colorMap[x + y * 241] = SampleColor(x,y);
            }
        }
        texture.SetPixels(colorMap);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();


        mapRenderer.sharedMaterial.mainTexture = texture;
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
            GenerateBiomeMap();
        }
    }

    void Update()
    {
        
    }
}
