using System.Collections.Generic;
using UnityEngine;


//Implement heightCurves from Scratch to run on threads
//improve sampling filter for height maps. currently has many artifacts


/// <summary>
/// Class used to sample the entire map and handle the blending between different (neighbouring) biome cells
/// </summary>
public class WorldGenerator
{
    //check if there is an atlas on this path, otherwise create one.
    //Also pass this path to the terrain material
    public const string atlasPath = "/Map/BiomeMaps/worldAtlas.png";
    
    public int biomeMapSize;
    public float biomeMapScale = 1f;
    public int biomeGridSize;
    public BiomeSampler biomeIdSampler;
    public List<BiomeSampler> biomeSamplers; 
    public BiomeLinks biomeLinks; 

    public int subChunkLevel;


    /// <summary>
    /// Constructor used when generating the biome samplers for the first time
    /// </summary>
    public WorldGenerator(BiomeSampler biomeIdSampler, List<BiomeSampler> biomeSamplers, WorldGenerationSettings worldGenerationSettings)
    {
        this.biomeIdSampler = biomeIdSampler;
        this.biomeSamplers = biomeSamplers;
        this.biomeMapSize = worldGenerationSettings.biomeMapSize;
        this.biomeMapScale = worldGenerationSettings.biomeMapScale;
        this.biomeGridSize = worldGenerationSettings.biomeGridSize;
        
        this.biomeLinks = new BiomeLinks(biomeGridSize);
        this.biomeLinks.GenerateLinksFromGrid(); 

        GenerateWorldAtlas(worldGenerationSettings);
        this.subChunkLevel = worldGenerationSettings.subChunkLevel;
        //this.detailGenSettings = worldGenerationSettings.GetAllDetailSettings();
    }

    /// <summary>
    /// Constructor used for loading previously generated world biome samplers into a world sampler
    /// </summary>
    public WorldGenerator(BiomeSampler biomeIdSampler, List<BiomeSampler> biomeSamplers, BiomeLinks biomeLinks, WorldGenerationSettings worldGenerationSettings)
    {
        this.biomeIdSampler = biomeIdSampler;
        this.biomeSamplers = biomeSamplers;
        this.biomeLinks = biomeLinks;
        this.biomeMapSize = worldGenerationSettings.biomeMapSize;
        this.biomeMapScale = worldGenerationSettings.biomeMapScale;
        this.biomeGridSize = worldGenerationSettings.biomeGridSize;
        this.subChunkLevel = worldGenerationSettings.subChunkLevel;
        
    }

    

    /// <summary>
    /// Generates a texture map of the world
    /// </summary>
    private void GenerateWorldAtlas(WorldGenerationSettings worldGenerationSettings)
    {
        int textureSize = worldGenerationSettings.biomeTextureSize;
        int mapSize = biomeMapSize;

        float textureScale = mapSize/(float)(textureSize-1);

        Texture2D atlasTexture = new Texture2D(textureSize, textureSize);
        Color[] colormap = new Color[(textureSize) * (textureSize)];

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                ///////////////////////////////////////////////////////////////////////////////
                //sum nearby biome textures to form transition zones
                //later this can be evolved by conditionally checking the neighbouring biomes
                //and using a specific texture for trasition between them
                ///////////////////////////////////////////////////////////////////////////
                int cellId = BiomeMapGenerator.DecodeCellIndex(biomeIdSampler.SampleBiomeNearest(x * textureScale,y * textureScale).r, biomeGridSize);
                PrimaryGenerationSettings genSettings = worldGenerationSettings.GetPrimarySettings(cellId);
                Texture2D baseTexture = genSettings.baseTexture;
                float scale = genSettings.baseTextureScale;

                colormap[x + (textureSize) * y] = baseTexture.GetPixelBilinear(x/scale, y/scale);
            }
        }
        atlasTexture.SetPixels(colormap);
        atlasTexture.Apply();

        System.IO.File.WriteAllBytes(Application.dataPath + atlasPath, atlasTexture.EncodeToPNG());
    }





    public float GetHeight(float _x, float _y) 
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
    
    public Biomes GetBiome(float _x, float _y) 
    {

        float x = biomeMapScale * _x;
        float y = biomeMapScale * _y;

        
        if (x < 0 || y < 0 || x > biomeMapSize || y > biomeMapSize)
        {
            return Biomes.HighMountains;
        }

        
        int cellId = BiomeMapGenerator.DecodeCellIndex(biomeIdSampler.SampleBiomeNearest(x,y).r, biomeGridSize);

        BiomeSampler biomeSampler = biomeSamplers[cellId];
        
        //Debug.Log(x + ", " + y + ": "+ biomeSampler.biomeType);
        return biomeSampler.biomeType;
    }
    public int GetId(float _x, float _y) 
    {

        float x = biomeMapScale * _x;
        float y = biomeMapScale * _y;

        
        if (x < 0 || y < 0 || x > biomeMapSize || y > biomeMapSize)
        {
            return 100;
        }

        
        int cellId = BiomeMapGenerator.DecodeCellIndex(biomeIdSampler.SampleBiomeNearest(x,y).r, biomeGridSize);
        

        return cellId;
    }

    /*
    public TerrainDetailSettings GetDetails(float _x, float _y) 
    {
        Biomes biome = GetBiome(_x,_y);
        if (detailGenSettings.ContainsKey(biome))
        {
            return detailGenSettings[biome];
        }
        else
        {
            return null;
        }
        
    }

    public TerrainDetailSettings[] GetDetailsOnBounds(Bounds bounds) 
    {
        TerrainDetailSettings[] allDetails = new TerrainDetailSettings[4]
        {
            GetDetails(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y),
            GetDetails(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y),
            GetDetails(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y),
            GetDetails(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y)
        };


        return allDetails;

    }*/

    public float GetBiomeMapScale()
    {
        return biomeMapScale;
    }
}
