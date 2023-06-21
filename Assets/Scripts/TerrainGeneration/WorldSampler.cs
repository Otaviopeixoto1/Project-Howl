using System.Collections;
using System.Collections.Generic;
using UnityEngine;




//Samples the entire map biomes and height for generating meshes. 
[RequireComponent(typeof(BiomeManager))]
public class WorldSampler : MonoBehaviour
{
    [SerializeField]
    private BiomeManager biomeManager;
     
    [SerializeField]
    [Range(1,20)]
    private float heightMultiplier = 1f;

    [SerializeField]
    [Range(1,240)]
    private int biomeMapSize = 240;

    [SerializeField]
    [Range(0.01f,1)]
    private float biomeMapScale = 1f;




    //Implement heightCurves from Scratch to run on threads
    //implement bilinear filtering
    public float SampleHeight(float _x, float _y) 
    {

        float x = biomeMapScale * _x;
        float y = biomeMapScale * _y;

        if (x < 0 || y < 0 || x > biomeMapSize || y > biomeMapSize)
        {
            return 0f;
        }

        BiomeSampler biomeIdSampler = biomeManager.GetBiomeIdSampler();
        int gridSize = biomeManager.biomeGridSize;
        int cellId = Mathf.RoundToInt(BiomeMapGenerator.DecodeCellIndex(biomeIdSampler.SampleBiomeNearest(x,y).r, gridSize));


        BiomeSampler biomeSampler = biomeManager.GetBiomeSampler(cellId);
        float cellValue = biomeSampler.SampleBiome(x,y).r;
        float finalHeight = biomeSampler.SampleHeight(x,y) * cellValue; 
        float totalValue = cellValue;


        if (cellValue < 1.1)
        {
            foreach (int neighbourId in biomeManager.GetNeighbours(cellId))
            {
                BiomeSampler neighbourSampler = biomeManager.GetBiomeSampler(neighbourId);
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

        BiomeSampler biomeIdSampler = biomeManager.GetBiomeIdSampler();
        int gridSize = biomeManager.biomeGridSize;
        int cellId = Mathf.RoundToInt(BiomeMapGenerator.DecodeCellIndex(biomeIdSampler.SampleBiomeNearest(x,y).r, gridSize));


        BiomeSampler biomeSampler = biomeManager.GetBiomeSampler(cellId);
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



    void OnValidate()
    {

    }
    void Awake()
    {
        
    }

    void Start()
    {
        biomeManager = GetComponent<BiomeManager>();
    }

    void Update()
    {
        
    }
}
