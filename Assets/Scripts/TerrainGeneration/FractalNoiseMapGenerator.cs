using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FractalNoiseGenerator", menuName = "ScriptableObjects/FractalNoiseGenerator", order = 1)]
//Fractal* 
public class FractalNoiseMapGenerator : MapGenerator 
{   
    public int noiseSeed = 1;
    private FastNoiseLite noiseGenerator = new FastNoiseLite();

    public FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2;
    public FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.FBm;
    
    [Range(1, 10)]
    public int octaves = 10;

    [Range(0, 100)]
    public float frequency = 1f;
    
    [Range(0, 2)]
    public float lacunarity = 2f;

    [Range(0, 2)]
    public float persistence = 0.5f;

    [Range(0, 2)]
    public float amplitude = 1f;






    public override float[,] GenerateMap()
    {

        noiseGenerator.SetNoiseType(noiseType);
        noiseGenerator.SetFractalType(fractalType);
        noiseGenerator.SetFrequency(frequency);
        noiseGenerator.SetFractalOctaves(octaves);
        noiseGenerator.SetFractalLacunarity(lacunarity);
        noiseGenerator.SetFractalGain(persistence);

        float[,] noiseMap = new float[mapWidth,mapHeight];

        float scale = mapScale;
        if (scale <= 0)
        {
            scale = 0.01f;
        }

        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                float sampleX = x/scale;
                float sampleY = y/scale;
            
                float noiseValue = (noiseGenerator.GetNoise(sampleX,sampleY) + 1) * 0.5f * amplitude;
                noiseMap[x,y] = noiseValue;
            }
        }

        return noiseMap;
    }


    void OnValidate()
    {
        triggerUpdate = true;
    }


}