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

    [Range(0, 20)]
    public float frequency = 1f;
    
    [Range(0, 10)]
    public float lacunarity = 2f;

    [Range(0, 10)]
    public float persistence = 0.5f;


    //public Vector2 offset = Vector2.zero;

    public override void ApplySettings()
    {
        base.ApplySettings();
        noiseGenerator.SetNoiseType(noiseType);
        noiseGenerator.SetFractalType(fractalType);
        noiseGenerator.SetFrequency(frequency);
        noiseGenerator.SetFractalOctaves(octaves);
        noiseGenerator.SetFractalLacunarity(lacunarity);
        noiseGenerator.SetFractalGain(persistence);
    }



    public override float SampleMap(float x, float y)
    {

        float scale = mapScale;
        if (scale <= 0)
        {
            scale = 0.01f;
        }

        float sampleX = (x) * scale; // + GlobalOffset.y
        float sampleY = (y) * scale; // + GlobalOffset.x
            
        float noiseValue = ((noiseGenerator.GetNoise(sampleX,sampleY) + 1) * 0.5f )* amplitude;

        return noiseValue;
    }


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
                float sampleX = (x) * scale;   // + offset.y
                float sampleY = (y) * scale;   // + offset.x
            
                float noiseValue = ((noiseGenerator.GetNoise(sampleX,sampleY) + 1) * 0.5f )* amplitude;
                noiseMap[x,y] = noiseValue;
            }
        }

        return noiseMap;
    }




}