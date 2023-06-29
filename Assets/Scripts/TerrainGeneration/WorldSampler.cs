using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Implement heightCurves from Scratch to run on threads
//improve sampling filter for height maps. currently has many artifacts


/// <summary>
/// Class used to sample the entire map and handle the blending between different (neighbouring) biome cells
/// </summary>

[RequireComponent(typeof(WorldManager))]
public class WorldSampler : MonoBehaviour
{
    [SerializeField]
    private WorldManager worldManager;
     
    [SerializeField]
    [Range(1,20)]
    private float heightMultiplier = 1f;

    [SerializeField]
    [Range(1,240)]
    private int biomeMapSize = 240;

    [SerializeField]
    [Range(0.01f,1)]
    private float biomeMapScale = 1f;


    void OnEnable()
    {
        WorldManager.OnSuccessfulLoad += SetManager;
    }


    void OnDisable()
    {
        WorldManager.OnSuccessfulLoad -= SetManager;
    }


    void Start()
    {
        worldManager = GetComponent<WorldManager>();
    }

    void Update()
    {
        
    }

    private void SetManager()
    {
        worldManager = GetComponent<WorldManager>();
        WorldManager.OnSuccessfulLoad -= SetManager;
    }

    public float SampleHeight(float _x, float _y) 
    {

        float x = biomeMapScale * _x;
        float y = biomeMapScale * _y;

        if (x < 0 || y < 0 || x > biomeMapSize || y > biomeMapSize)
        {
            return 0f;
        }

        BiomeSampler biomeIdSampler = worldManager.GetBiomeIdSampler();
        int gridSize = WorldManager.biomeGridSize;
        int cellId = Mathf.RoundToInt(BiomeMapGenerator.DecodeCellIndex(biomeIdSampler.SampleBiomeNearest(x,y).r, gridSize));


        BiomeSampler biomeSampler = worldManager.GetBiomeSampler(cellId);
        float cellValue = biomeSampler.SampleBiome(x,y).r;
        float finalHeight = biomeSampler.SampleHeight(x,y) * cellValue; 
        float totalValue = cellValue;


        if (cellValue < 1.1) // add this as a threashold parameter
        {
            foreach (int neighbourId in worldManager.GetNeighbours(cellId))
            {
                BiomeSampler neighbourSampler = worldManager.GetBiomeSampler(neighbourId);
                float nCellValue = neighbourSampler.SampleBiome(x,y).r;
                float nheight = neighbourSampler.SampleHeight(x,y) * nCellValue;
                totalValue += nCellValue;
                finalHeight += nheight;
            }
        }                                   
        finalHeight /= (totalValue + 0.001f); 

        return finalHeight * heightMultiplier;
    }

    public Color SampleColor(float _x, float _y)
    {
        float x = biomeMapScale * _x;
        float y = biomeMapScale * _y;

        if (x < 0 || y < 0 || x > biomeMapSize || y > biomeMapSize)
        {
            return Color.black;
        }

        BiomeSampler biomeIdSampler = worldManager.GetBiomeIdSampler();
        int gridSize = WorldManager.biomeGridSize;
        int cellId = Mathf.RoundToInt(BiomeMapGenerator.DecodeCellIndex(biomeIdSampler.SampleBiomeNearest(x,y).r, gridSize));


        BiomeSampler biomeSampler = worldManager.GetBiomeSampler(cellId);
        float cellValue = biomeSampler.SampleBiome(x,y).r;
        Color finalColor = biomeSampler.displayColor * cellValue;
        float totalValue = cellValue;

        /*
        if (cellValue < 0.9)
        {
            foreach (int neighbourId in biomeManager.GetNeighbours(cellId))
            {
                BiomeSampler neighbourSampler = biomeManager.GetBiomeSampler(neighbourId);
                float nCellValue = neighbourSampler.SampleBiome(x,y).r;
                Color nColor = neighbourSampler.displayColor * nCellValue;
                totalValue += nCellValue;
                finalColor += nColor;
            }
        }*/
        finalColor /= (totalValue + 0.001f); 

        return finalColor;
    }

}
