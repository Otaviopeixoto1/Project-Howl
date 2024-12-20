using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Class used to generate random Voronoi/Cellular noise that is then used for assigning biome regions (biome cells)
/// </summary>
[CreateAssetMenu(fileName = "BiomeMapGenerator", menuName = "ScriptableObjects/BiomeMapGenerator", order = 1)] 
public class BiomeMapGenerator : MapGenerator
{
    private FastNoiseLiteExtension noiseGenerator = new FastNoiseLiteExtension();

    public int cellularSeed = 1;
    
    public FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.None;
    
    [Range(1,240)]
    public int mapSize = 240;

    [Range(1,10)]
    public int gridDimension = 4;
    
    //[Range(0, 5)]
    [HideInInspector]
    public float frequency = 1f;
    [Range(1, 10)]
    public int octaves = 2;
    [Range(0, 10)]
    public float lacunarity = 0.31f;

    [Range(0, 10)]
    public float persistence = 6.74f;

    public FastNoiseLite.CellularDistanceFunction distanceFunction = FastNoiseLite.CellularDistanceFunction.Hybrid;
    public FastNoiseLite.ModifiedCellularReturnType modCellularReturnType = FastNoiseLite.ModifiedCellularReturnType.ModifiedCellValue;

    [Range(0,1)]
    public float cellularJitter = 1f;



    /// <summary>
    /// Applies all changes made to the exposed variables into the noise generator
    /// </summary>
    public override void ApplySettings()
    {
        frequency = gridDimension/(mapScale * mapSize);
        base.ApplySettings();
        noiseGenerator.SetSeed(cellularSeed);
        noiseGenerator.SetNoiseType(FastNoiseLite.NoiseType.ModifiedCellular);
        noiseGenerator.SetFractalType(fractalType);
        noiseGenerator.SetFrequency(frequency);
        noiseGenerator.SetFractalOctaves(octaves);
        noiseGenerator.SetFractalLacunarity(lacunarity);
        noiseGenerator.SetFractalGain(persistence);
        noiseGenerator.SetCellularDistanceFunction(distanceFunction);
        noiseGenerator.SetModifiedCellularReturnType(modCellularReturnType);
        noiseGenerator.SetModifiedCellularGridDim(gridDimension);
        noiseGenerator.SetCellularJitter(cellularJitter);
    }

    /// <summary>
    /// Samples the dynamicaly generated map (must use ApplySettings() first). The result will be encoded and will
    /// depend on the choice of ModifiedCellularReturnType
    /// </summary>
    public override float SampleMap(float x, float y, AnimationCurve samplingCurve = null)
    {
        float scale = mapScale;

        float sampleX = (x) * scale;
        float sampleY = (y) * scale;
        
        float noiseValue = ((noiseGenerator.GetNoise(sampleX,sampleY) ))* amplitude;

        return noiseValue;
    }

    /// <summary>
    /// Decodes the value obtained from sampling the map containing the cell indexes
    /// </summary>
    public static int DecodeCellIndex(float indexSample, int gridSize) 
    {
        return Mathf.RoundToInt(indexSample * gridSize *(gridSize + 2));
    }

    /// <summary>
    /// Encodes the value obtained from sampling the noise map with the return value of ModifiedCellValue
    /// reprensenting the cell indexes
    /// </summary>
    public static float EncodeCellIndex(float indexSample, int gridSize)
    {
        return indexSample /( gridSize *(gridSize + 2));
    }

    /// <summary>
    /// Sets the return type to ModifiedCellValue, generates the cell index map and returns the encoded version in the form of a texture
    /// </summary>
    public Texture2D GetBiomeIndexMap() // this could be faster without textures
    {
        noiseGenerator.SetModifiedCellularReturnType(FastNoiseLite.ModifiedCellularReturnType.ModifiedCellValue);

        Texture2D fullBiomeMap = new Texture2D(mapSize+1, mapSize+1);

        Color[] colorMap = new Color[(mapSize+1) * (mapSize+1)];

        for (int y = -mapSize/2; y <= mapSize/2; y++)
        {
            for (int x = -mapSize/2; x <= mapSize/2; x++)
            {
                float cellVal = (SampleMap(x , y)/amplitude) * frequency;
                int finalX = x + mapSize/2;
                int finalY = y + mapSize/2;
                colorMap[finalX + finalY * (mapSize+1)] =  Color.white * EncodeCellIndex(cellVal, gridDimension);
            }
        }

        fullBiomeMap.SetPixels(colorMap);

        noiseGenerator.SetModifiedCellularReturnType(modCellularReturnType);

        return fullBiomeMap;
    }


    public Texture2D GetSingleBiomeMap(int index, float scaleIncrease)
    {
        noiseGenerator.SetModifiedCellularReturnType(FastNoiseLite.ModifiedCellularReturnType.ModifiedCellValue);

        Vector2[] vectors = noiseGenerator.GetCellularVectors();

        Vector2 offset = ((vectors[index] * scaleIncrease) - vectors[index])* mapSize/(float)gridDimension;
        int pixelXOffset = Mathf.RoundToInt(offset.x);
        int pixelYOffset = Mathf.RoundToInt(offset.y);


        int newSize = Mathf.RoundToInt(mapSize * scaleIncrease);
        float scaleDif = mapSize/(float)newSize;

        Texture2D singleBiomeMap = new Texture2D(mapSize+1, mapSize+1);
        
        Color[] colorMap = new Color[(mapSize+1) * (mapSize+1)];

        for (int y = -newSize/2; y <= newSize/2; y++)
        {
            for (int x = -newSize/2; x <= newSize/2; x++)
            {
                //save this map before everything;
                float cellVal = SampleMap(x * scaleDif , y * scaleDif)/amplitude;
                if (Mathf.RoundToInt(cellVal * frequency) == index)
                {
                    int finalX = x + mapSize/2 - pixelXOffset;
                    int finalY = y + mapSize/2 - pixelYOffset;

                    if (finalX >= 0 && finalY >= 0 && finalX <= mapSize && finalY <= mapSize)
                    {
                        noiseGenerator.SetModifiedCellularReturnType(FastNoiseLite.ModifiedCellularReturnType.Distance2Sub);
                        float cellSDF = ( SampleMap(x * scaleDif , y * scaleDif)/amplitude);
                        noiseGenerator.SetModifiedCellularReturnType(FastNoiseLite.ModifiedCellularReturnType.ModifiedCellValue);
                        //Debug.Log(finalX + " " + finalY);
                        colorMap[finalY * (mapSize+1) + finalX] =  Color.white * cellSDF;
                        
                    }
                }
            }
        }

        singleBiomeMap.SetPixels(colorMap);
        singleBiomeMap.filterMode = FilterMode.Point; 
        singleBiomeMap.wrapMode = TextureWrapMode.Clamp; 

        noiseGenerator.SetModifiedCellularReturnType(modCellularReturnType);
        return singleBiomeMap;
    }


    /// <summary>
    /// Gets the distance field of a single cell (representing a biome map) in the form of a texture
    /// </summary>
    public Texture2D GetSingleBiomeMap(int index, float scaleIncrease, BiomeSampler cellIndexSampler, int gridSize)
    {
        noiseGenerator.SetModifiedCellularReturnType(FastNoiseLite.ModifiedCellularReturnType.Distance2Sub);
        
        Vector2[] vectors = noiseGenerator.GetCellularVectors();

        int mSize = cellIndexSampler.GetSize() - 1;

        Vector2 offset = ((vectors[index] * scaleIncrease) - vectors[index])* mSize/(float)gridDimension;
        
        int pixelXOffset = Mathf.RoundToInt(offset.x);
        int pixelYOffset = Mathf.RoundToInt(offset.y);

        

        int newSize = Mathf.RoundToInt(mSize * scaleIncrease);

        if (Mathf.Abs(pixelXOffset) > (newSize - mSize)/2)
        {
            newSize += Mathf.Abs(pixelXOffset) - (newSize - mSize)/2 + 2;
        }
        else if (Mathf.Abs(pixelYOffset) > (newSize - mSize)/2)
        {
            newSize += Mathf.Abs(pixelYOffset) - (newSize - mSize)/2 + 2;
        }


    
        float scaleDif = mSize/(float)newSize;

        Texture2D singleBiomeMap = new Texture2D(mSize + 1, mSize + 1);
        
        Color[] colorMap = new Color[(mSize + 1) * (mSize + 1)];

        for (int y = -newSize/2; y <= newSize/2; y++)
        {
            for (int x = -newSize/2; x <= newSize/2; x++)
            {
                float cellIdEnc = cellIndexSampler.SampleBiomeNearest((x + newSize/2) * scaleDif , (y + newSize/2) * scaleDif).r;
                if (DecodeCellIndex(cellIdEnc,gridSize) == index)
                {
                    int finalX = x + mSize/2 - pixelXOffset;
                    int finalY = y + mSize/2 - pixelYOffset;

                    if (finalX >= 0 && finalY >= 0 && finalX <= mSize && finalY <= mSize)
                    {
                        //Encode this sdf in a way that doesnt saturate so quickly

                        float cellSDF = SampleMap(x * scaleDif , y * scaleDif)/amplitude;

                        colorMap[finalY * (mSize+1) + finalX] =  new Color(cellSDF, 0,0,1);
                        
                    }
                }
            }
        }

        singleBiomeMap.SetPixels(colorMap);
        singleBiomeMap.filterMode = FilterMode.Point; 
        singleBiomeMap.wrapMode = TextureWrapMode.Clamp; 
  
        noiseGenerator.SetModifiedCellularReturnType(modCellularReturnType);

        return singleBiomeMap;
    }








    public override float[,] GenerateMap(int mapWidth, int mapHeight)
    {

        
        

        return null;
    }
}
