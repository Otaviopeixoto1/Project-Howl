using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "BiomeMapGenerator", menuName = "ScriptableObjects/BiomeMapGenerator", order = 1)] 
public class BiomeMapGenerator : MapGenerator
{
    [SerializeField]
    private int biomeSeed = 1;
    private FastNoiseLite noiseGenerator = new FastNoiseLite();

    private FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.ModifiedCellular;
    public FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.None;

    public int gridDimension = 4;
    
    public int tiling = 4;

    [Range(1, 10)]
    public int octaves = 2;

    [Range(0, 5)]
    public float frequency = 1f;
    
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
        noiseGenerator.SetModifiedCellularTilingX(tiling);
        noiseGenerator.SetCellularJitter(cellularJitter);

    }

    public override float SampleMap(float x, float y, AnimationCurve samplingCurve = null)
    {
        float scale = mapScale;
        if (scale <= 0)
        {
            scale = 0.01f;
        }

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


    public override float[,] GenerateMap(int mapWidth, int mapHeight)
    {

        
        

        return null;
    }
}
