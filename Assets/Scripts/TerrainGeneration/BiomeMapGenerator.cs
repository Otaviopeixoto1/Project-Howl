using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "BiomeMapGenerator", menuName = "ScriptableObjects/BiomeMapGenerator", order = 1)] 
public class BiomeMapGenerator : MapGenerator
{
    [SerializeField]
    private int cellularSeed = 1;
    private FastNoiseLiteExtension noiseGenerator = new FastNoiseLiteExtension();

    private FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.ModifiedCellular;
    public FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.None;
    
    [Range(1,240)]
    public int MapSize = 240;

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

    //public Vector2 offset = Vector2.zero;

    public override void ApplySettings()
    {
        frequency = gridDimension/(mapScale * MapSize);
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

        float sampleX = (x) * scale; // + GlobalOffset.y
        float sampleY = (y) * scale; // + GlobalOffset.x
        
        float noiseValue;
        
        /*
        if (heightCurve != null)
        {
            noiseValue = heightCurve.Evaluate((noiseGenerator.GetNoise(sampleX,sampleY) + 1) * 0.5f )* amplitude;
        }*/
        //else
        {
            noiseValue = ((noiseGenerator.GetNoise(sampleX,sampleY) ))* amplitude;
        }
        

        return noiseValue;
    }

    public void BakeCellValues()
    {

    }


    public Texture2D GetBiomeMap(int index, float scaleIncrease)
    {
        noiseGenerator.SetModifiedCellularReturnType(FastNoiseLite.ModifiedCellularReturnType.ModifiedCellValue);

        Vector2[] vectors = noiseGenerator.GetCellularVectors();

        Vector2 offset = ((vectors[index] * scaleIncrease) - vectors[index])* MapSize/(float)gridDimension;
        int pixelXOffset = Mathf.RoundToInt(offset.x);
        int pixelYOffset = Mathf.RoundToInt(offset.y);


        int newSize = Mathf.RoundToInt(MapSize * scaleIncrease);
        float scaleDif = MapSize/(float)newSize;
        int pixelDif = newSize - MapSize;

        Texture2D singleBiomeMap = new Texture2D(MapSize+1, MapSize+1);
        Color[] colorMap = new Color[(MapSize+1) * (MapSize+1)];

        for (int y = -newSize/2; y <= newSize/2; y++)
        {
            for (int x = -newSize/2; x <= newSize/2; x++)
            {
                //save this map before everything;
                float cellVal = SampleMap(x * scaleDif , y * scaleDif)/amplitude;
                if (Mathf.RoundToInt(cellVal * frequency) == index)
                {
                    int finalX = x + MapSize/2 - pixelXOffset;
                    int finalY = y + MapSize/2 - pixelYOffset;

                    if (finalX >= 0 && finalY >= 0 && finalX <= MapSize && finalY <= MapSize)
                    {
                        noiseGenerator.SetModifiedCellularReturnType(FastNoiseLite.ModifiedCellularReturnType.Distance2Sub);
                        float cellSDF = SampleMap(x * scaleDif , y * scaleDif)/amplitude;
                        noiseGenerator.SetModifiedCellularReturnType(FastNoiseLite.ModifiedCellularReturnType.ModifiedCellValue);
                        //Debug.Log(finalX + " " + finalY);
                        colorMap[finalY * (MapSize+1) + finalX] =  Color.white * cellSDF;
                        
                    }
                    



                }

                
                //discard finalX and finalY with values < 0 or > mapsize
            }
        }
        singleBiomeMap.SetPixels(colorMap);
        singleBiomeMap.filterMode = FilterMode.Point; 
        singleBiomeMap.wrapMode = TextureWrapMode.Clamp; 
        singleBiomeMap.Apply();
        System.IO.File.WriteAllBytes(Application.dataPath+ "/Map/map.png" , singleBiomeMap.EncodeToPNG());

        noiseGenerator.SetModifiedCellularReturnType(modCellularReturnType);
        return singleBiomeMap;
    }





    public override float[,] GenerateMap(int mapWidth, int mapHeight)
    {

        
        

        return null;
    }
}
