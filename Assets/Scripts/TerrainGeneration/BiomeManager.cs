using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

//assign the biomes here. Make a graph structure for neighbouring biomes. When sampling the biome
//edges it should be enough to only sample neighbouring biome maps instead of all maps at once

[Serializable]
public class BiomeLinks //add the links to the biome map data as an array off biome indexes
{
    public int gridSize;
    public Dictionary<int, int[]> neighbours = new Dictionary<int, int[]>();
    
    public BiomeLinks(int gridSize)
    {
        this.gridSize = gridSize;
    }

    public void GenerateLinksFromGrid()
    {
        neighbours[0] = new int[]{
            1, 
            gridSize + 1, 
            gridSize + 2
        };

        neighbours[gridSize * (gridSize + 1)] = new int[]{
            (gridSize * gridSize) - 1, 
            (gridSize * gridSize), 
            gridSize * (gridSize + 1) + 1
        };

        neighbours[gridSize] = new int[]{
            gridSize - 1, 
            (2 * gridSize) + 1, 
            2 * gridSize
        };

        neighbours[gridSize * (gridSize + 2)] = new int[]{
            gridSize * (gridSize + 2) - 1, 
            gridSize * (gridSize + 1) - 1, 
            gridSize * (gridSize + 1) - 2
        };

        for (int i = 1; i < gridSize; i++)
        {
            neighbours[i] =  new int[]{
                i - 1, 
                i + 1, 
                i + gridSize,
                i + gridSize + 1,
                i + gridSize + 2,
            };

            int t = i + gridSize * (gridSize + 1);
            neighbours[t] =  new int[]{
                t - 1, 
                t + 1, 
                t - gridSize,
                t - gridSize - 1,
                t - gridSize - 2,
            };

            int l = (i) * (gridSize + 1);
            neighbours[l] =  new int[]{
                l - (gridSize + 1), 
                l + (gridSize + 1), 
                l + 1,
                l + (gridSize + 2),
                l - (gridSize), 
            };

            int r = (i) * (gridSize + 1) + gridSize;
            neighbours[r] =  new int[]{
                r - (gridSize + 1), 
                r + (gridSize + 1), 
                r - 1,
                r - (gridSize + 2),
                r + (gridSize), 
            };


        }


        for (int i = gridSize + 2; i < (gridSize * (gridSize + 1) + 1); i += gridSize + 1)
        {

            for (int j = 0; j < gridSize - 1; j++)
            {
                neighbours[i + j] =  new int[]{
                    i + j - 1, 
                    i + j + 1, 
                    i + j - (gridSize + 1), 
                    i + j + (gridSize + 1), 
                    i + j - (gridSize + 2), 
                    i + j + (gridSize + 2), 
                    i + j - (gridSize), 
                    i + j + (gridSize), 
                };
            }

        }
    }
    public void Print()
    {
        foreach(KeyValuePair<int, int[]> entry in neighbours)
        {
            Debug.Log("{" + entry.Key+ ", " + entry.Value + "}");
        }
    }
    
}


public class BiomeManager : MonoBehaviour
{
    private int biomeGridSize;
    private BiomeSampler fullBiomeMap;

    [NonReorderable]
    [SerializeField]
    private List<BiomeSampler> biomeSamplers;
    public BiomeLinks biomeLinks;

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

            biomeLinks = new BiomeLinks(biomeGridSize);
            biomeLinks.GenerateLinksFromGrid();
            biomeLinks.Print();
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

    public float SampleHeight(float x, float y)
    {
        return 1f;
    }

    public Color SampleColor(float x, float y)
    {
                                                    //use encoder/decoder for these values
        int cellId = Mathf.RoundToInt(fullBiomeMap.SampleBiomeNearest(x,y).r * 24f);
        float cellValue = biomeSamplers[cellId].SampleBiome(x,y).r;
        Color finalColor = biomeSamplers[cellId].displayColor * cellValue;



        if (cellValue < 0.6)
        {
            foreach (int neighbourId in biomeLinks.neighbours[cellId])
            {
                //Debug.Log(cellId + ", " + neighbourId);
                float nCellValue = biomeSamplers[neighbourId].SampleBiome(x,y).r;
                Color nColor = biomeSamplers[neighbourId].displayColor * cellValue;
                finalColor += nColor/5;
            }
        }

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
            GenerateMap();
        }
    }

    void Update()
    {
        
    }
}
