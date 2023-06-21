using UnityEngine;
using System;

[CreateAssetMenu(fileName = "HeightMapGenerator", menuName = "ScriptableObjects/HeightMapGenerator", order = 1)] 
[Serializable]
public class HeightMapGenerator : MapGenerator 
{   
    private FastNoiseLite noiseGenerator = new FastNoiseLite();
    public int noiseSeed = 1;

    public FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.OpenSimplex2;
    public FastNoiseLite.FractalType fractalType = FastNoiseLite.FractalType.FBm;
    
    [Range(1, 10)]
    public int octaves = 4;

    [Range(0, 5)]
    public float frequency = 1.9f;
    
    [Range(0, 10)]
    public float lacunarity = 0.32f;

    [Range(0, 10)]
    public float persistence = 8f;




    //public Vector2 offset = Vector2.zero;

    public override void ApplySettings()
    {
        base.ApplySettings();
        noiseGenerator.SetSeed(noiseSeed);
        noiseGenerator.SetNoiseType(noiseType);
        noiseGenerator.SetFractalType(fractalType);
        noiseGenerator.SetFrequency(frequency);
        noiseGenerator.SetFractalOctaves(octaves);
        noiseGenerator.SetFractalLacunarity(lacunarity);
        noiseGenerator.SetFractalGain(persistence);
    }



    public override float SampleMap(float x, float y, AnimationCurve heightCurve = null)
    {

        float scale = mapScale;
        if (scale <= 0)
        {
            scale = 0.01f;
        }

        float sampleX = (x) * scale; // + GlobalOffset.y
        float sampleY = (y) * scale; // + GlobalOffset.x
        
        float noiseValue;
        
        if (heightCurve != null)
        {
            noiseValue = heightCurve.Evaluate((noiseGenerator.GetNoise(sampleX,sampleY) + 1) * 0.5f )* amplitude;
        }
        else
        {
            noiseValue = ((noiseGenerator.GetNoise(sampleX,sampleY) + 1) * 0.5f )* amplitude;
        }
        

        return noiseValue;
    }


    public override float[,] GenerateMap(int mapWidth, int mapHeight)
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