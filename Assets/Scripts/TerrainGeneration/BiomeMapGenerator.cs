using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "BiomeMapGenerator", menuName = "ScriptableObjects/BiomeMapGenerator", order = 1)] 
public class BiomeMapGenerator : MapGenerator
{
    [SerializeField]
    private int cellularSeed = 1;
    private FastNoiseLiteExtension noiseGenerator = new FastNoiseLiteExtension();
    public FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.None;
    
    [Range(1,240)]
    public int mapSize = 240;

    [Range(1,10)]
    public int gridDimension = 4;
    
    //[Range(0, 5)]
    private float frequency = 1f;
    [Range(1, 10)]
    public int octaves = 2;
    [Range(0, 10)]
    public float lacunarity = 0.31f;

    [Range(0, 10)]
    public float persistence = 6.74f;

    public FastNoiseLite.CellularDistanceFunction distanceFunction = FastNoiseLite.CellularDistanceFunction.Euclidean;
    public FastNoiseLite.ModifiedCellularReturnType modCellularReturnType = FastNoiseLite.ModifiedCellularReturnType.ModifiedCellValue;

    [Range(0,1)]
    public float cellularJitter = 1f;




    public override void ApplySettings()
    {
        frequency = gridDimension/(mapScale * mapSize);
        base.ApplySettings();
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

    public override float SampleMap(float x, float y, AnimationCurve samplingCurve = null)
    {
        float scale = mapScale;

        float sampleX = (x) * scale;
        float sampleY = (y) * scale;
        
        float noiseValue = ((noiseGenerator.GetNoise(sampleX,sampleY) ))* amplitude;

        return noiseValue;
    }

    public Texture2D GetBiomeIndexMap()
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
                colorMap[finalX + finalY * (mapSize+1)] =  Color.white * cellVal/24f;
                //Add encoding and decoding of the colors based on grid size (it is necessary here)
                //add encoding and decoding functions for this. the decoding will be passed to the 
                //samplers !
                // this is a simple encoder: gridsize * (gridsize + 2)
            }
        }

        fullBiomeMap.SetPixels(colorMap);
        fullBiomeMap.filterMode = FilterMode.Point; 
        fullBiomeMap.wrapMode = TextureWrapMode.Clamp; 
        
        //fullBiomeMap.Apply();
        //System.IO.File.WriteAllBytes(Application.dataPath+ "/Map/BiomeMaps/FullMap.png" , fullBiomeMap.EncodeToPNG());
        
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
                        float cellSDF = SampleMap(x * scaleDif , y * scaleDif)/amplitude;
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

        singleBiomeMap.Apply();
        System.IO.File.WriteAllBytes(Application.dataPath+ "/Map/BiomeMaps/map.png" , singleBiomeMap.EncodeToPNG());

        noiseGenerator.SetModifiedCellularReturnType(modCellularReturnType);
        return singleBiomeMap;
    }



    public Texture2D GetSingleBiomeMap(int index, float scaleIncrease, BiomeSampler cellIndexSampler)
    {
        noiseGenerator.SetModifiedCellularReturnType(FastNoiseLite.ModifiedCellularReturnType.Distance2Sub);
        
        Vector2[] vectors = noiseGenerator.GetCellularVectors();
        //Debug.Log(index + ", " + vectors.Length);

        //int mSize = cellIndexSampler.GetSize() - 1;
        int mSize = 240;


        Vector2 offset = ((vectors[index] * scaleIncrease) - vectors[index])* mSize/(float)gridDimension;
        int pixelXOffset = Mathf.RoundToInt(offset.x);
        int pixelYOffset = Mathf.RoundToInt(offset.y);

        
        int newSize = Mathf.RoundToInt(mSize * scaleIncrease);
        float scaleDif = mSize/(float)newSize;


        Texture2D singleBiomeMap = new Texture2D(mSize + 1, mSize + 1);
        
        Color[] colorMap = new Color[(mSize + 1) * (mSize + 1)];

        for (int y = -newSize/2; y <= newSize/2; y++)
        {
            for (int x = -newSize/2; x <= newSize/2; x++)
            {
                                            //use a decoder delegate instead of this hardcoded value
                float cellVal = cellIndexSampler.Sample((x + newSize/2) * scaleDif , (y + newSize/2) * scaleDif) * 24f;
                if (Mathf.RoundToInt(cellVal) == index)
                {
                    int finalX = x + mSize/2 - pixelXOffset;
                    int finalY = y + mSize/2 - pixelYOffset;

                    if (finalX >= 0 && finalY >= 0 && finalX <= mSize && finalY <= mSize)
                    {

                        float cellSDF = SampleMap(x * scaleDif , y * scaleDif)/amplitude;
                        colorMap[finalY * (mSize+1) + finalX] =  Color.white * cellSDF;
                        
                    }
                }
            }
        }

        singleBiomeMap.SetPixels(colorMap);
        singleBiomeMap.filterMode = FilterMode.Point; 
        singleBiomeMap.wrapMode = TextureWrapMode.Clamp; 

        //singleBiomeMap.Apply();
        //System.IO.File.WriteAllBytes(Application.dataPath+ "/Map/BiomeMaps/map.png" , singleBiomeMap.EncodeToPNG());
        
        noiseGenerator.SetModifiedCellularReturnType(modCellularReturnType);

        return singleBiomeMap;
    }








    public override float[,] GenerateMap(int mapWidth, int mapHeight)
    {

        
        

        return null;
    }
}
