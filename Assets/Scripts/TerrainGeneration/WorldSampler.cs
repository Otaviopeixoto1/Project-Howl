using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Implement heightCurves from Scratch to run on threads
//improve sampling filter for height maps. currently has many artifacts


/// <summary>
/// Class used to sample the entire map and handle the blending between different (neighbouring) biome cells
/// </summary>
public class WorldSampler
{
    [Range(1,240)]
    public int biomeMapSize;

    [Range(0.01f,1)]
    public float biomeMapScale = 1f;

    public int biomeGridSize;


    public BiomeSampler biomeIdSampler;
    public List<BiomeSampler> biomeSamplers; //convert to array

    //Graph structure used to store information about neighbouring biomes (maybe blending between them):
    public BiomeLinks biomeLinks; 





    /// <summary>
    /// Constructor used when generating the biome samplers for the first time
    /// </summary>
    public WorldSampler(BiomeSampler biomeIdSampler, List<BiomeSampler> biomeSamplers, WorldGenerationSettings worldGenerationSettings)
    {
        this.biomeIdSampler = biomeIdSampler;
        this.biomeSamplers = biomeSamplers;
        this.biomeMapSize = worldGenerationSettings.biomeMapSize;
        this.biomeMapScale = worldGenerationSettings.biomeMapScale;
        this.biomeGridSize = worldGenerationSettings.biomeGridSize;
        

        this.biomeLinks = new BiomeLinks(biomeGridSize);
        this.biomeLinks.GenerateLinksFromGrid(); 
        //AssignBiomes(worldGenerationSettings,worldTopographyGenerator);
        GenerateWorldAtlas(worldGenerationSettings);
    }

    /// <summary>
    /// Constructor used for loading previously generated world biome samplers into a world sampler
    /// </summary>
    public WorldSampler(BiomeSampler biomeIdSampler, List<BiomeSampler> biomeSamplers, BiomeLinks biomeLinks, WorldGenerationSettings worldGenerationSettings)
    {
        this.biomeIdSampler = biomeIdSampler;
        this.biomeSamplers = biomeSamplers;
        this.biomeLinks = biomeLinks;
        this.biomeMapSize = worldGenerationSettings.biomeMapSize;
        this.biomeMapScale = worldGenerationSettings.biomeMapScale;
        this.biomeGridSize = worldGenerationSettings.biomeGridSize;
        
    }

    

    /// <summary>
    /// Generates a texture map of the world
    /// </summary>
    private void GenerateWorldAtlas(WorldGenerationSettings worldGenerationSettings)
    {
        int size = biomeMapSize;
        Texture2D worldtexture = new Texture2D(size + 1, size  + 1);
        Color[] colormap = new Color[(size + 1) * (size + 1)];

        for (int y = 0; y <= size; y++)
        {
            for (int x = 0; x <= size; x++)
            {
                int cellId = BiomeMapGenerator.DecodeCellIndex(biomeIdSampler.SampleBiomeNearest(x,y).r, biomeGridSize);
                GenerationSettings genSettings = worldGenerationSettings.GetGenerationSettings(cellId);
                Texture2D baseTexture = genSettings.baseTexture;
                float scale = genSettings.baseTextureScale;


                colormap[x + (size + 1) * y] = baseTexture.GetPixelBilinear(x/scale, y/scale);
            }
        }
        worldtexture.SetPixels(colormap);
        worldtexture.Apply();

        string atlasPath = "/Map/BiomeMaps/worldAtlas.png";
        System.IO.File.WriteAllBytes(Application.dataPath + atlasPath, worldtexture.EncodeToPNG());
    }













    public float SampleHeight(float _x, float _y) 
    {

        float x = biomeMapScale * _x;
        float y = biomeMapScale * _y;

        if (x < 0 || y < 0 || x > biomeMapSize || y > biomeMapSize)
        {
            return 0f;
        }


        int cellId = BiomeMapGenerator.DecodeCellIndex(biomeIdSampler.SampleBiomeNearest(x,y).r, biomeGridSize);


        BiomeSampler biomeSampler = biomeSamplers[cellId];
        float cellValue = biomeSampler.SampleBiome(x,y).r;
        float finalHeight = biomeSampler.SampleHeight(x,y) * cellValue * cellValue; 
        float totalValue = cellValue * cellValue;


        if (cellValue < 1.1) // add this as a threashold parameter
        {
            foreach (int neighbourId in biomeLinks.GetLinks(cellId))
            {
                BiomeSampler neighbourSampler = biomeSamplers[neighbourId];
                float nCellValue = neighbourSampler.SampleBiome(x,y).r;
                float nheight = neighbourSampler.SampleHeight(x,y) * nCellValue * nCellValue;
                totalValue += nCellValue * nCellValue;
                finalHeight += nheight;
            }
        }                                   
        finalHeight /= (totalValue + 0.001f); 

        return finalHeight;
    }


    public Color SampleAtlas(float _x, float _y)
    {
        float x = biomeMapScale * _x;
        float y = biomeMapScale * _y;

        if (x < 0 || y < 0 || x > biomeMapSize || y > biomeMapSize)
        {
            return Color.black;
        }


        int cellId = BiomeMapGenerator.DecodeCellIndex(biomeIdSampler.SampleBiomeNearest(x,y).r, biomeGridSize);


        BiomeSampler biomeSampler = biomeSamplers[cellId];
        float cellValue = biomeSampler.SampleBiome(x,y).r;
        Color finalColor = biomeSampler.displayColor * cellValue;
        float totalValue = cellValue;

        
        if (cellValue < 0.9)
        {
            foreach (int neighbourId in biomeLinks.GetLinks(cellId))
            {
                BiomeSampler neighbourSampler = biomeSamplers[neighbourId];
                float nCellValue = neighbourSampler.SampleBiome(x,y).r;
                Color nColor = neighbourSampler.displayColor * nCellValue;
                totalValue += nCellValue;
                finalColor += nColor;
            }
        }
        finalColor /= (totalValue + 0.001f); 

        return finalColor;
    }

    public Color SampleColor(float _x, float _y)
    {
        float x = biomeMapScale * _x;
        float y = biomeMapScale * _y;

        if (x < 0 || y < 0 || x > biomeMapSize || y > biomeMapSize)
        {
            return Color.black;
        }

        int cellId = BiomeMapGenerator.DecodeCellIndex(biomeIdSampler.SampleBiomeNearest(x,y).r, biomeGridSize);


        BiomeSampler biomeSampler = biomeSamplers[cellId];
        float cellValue = biomeSampler.SampleBiome(x,y).r;
        Color finalColor = biomeSampler.displayColor * cellValue;
        float totalValue = cellValue;

        
        if (cellValue < 0.9)
        {
            foreach (int neighbourId in biomeLinks.GetLinks(cellId))
            {
                BiomeSampler neighbourSampler = biomeSamplers[neighbourId];
                float nCellValue = neighbourSampler.SampleBiome(x,y).r;
                Color nColor = neighbourSampler.displayColor * nCellValue;
                totalValue += nCellValue;
                finalColor += nColor;
            }
        }
        finalColor /= (totalValue + 0.001f); 

        return finalColor;
    }
    
    public float GetBiomeMapScale()
    {
        return biomeMapScale;
    }
}
