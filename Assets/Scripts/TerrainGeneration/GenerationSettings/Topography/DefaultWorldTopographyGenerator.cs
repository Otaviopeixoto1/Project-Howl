using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "DefaultWorldTopographyGenerator", menuName = "ScriptableObjects/WorldGeneration/DefaultWorldTopographyGenerator", order = 2)] 
public class DefaultWorldTopographyGenerator : WorldTopographyGenerator
{

    //add custom functions for changing the heightmaps


    public override HeightMapGenerator GetHeightMapGenerator(TopographySettings topographySettings)
    {
        HeightMapGenerator heightMapGenerator = ScriptableObject.CreateInstance<HeightMapGenerator>();
        
        heightMapGenerator.noiseSeed = WorldGenerationSettings.worldSeed;
        
        Vector2 frequencyRange = topographySettings.noiseFrequencyRange;
        Vector2 lacunarityRange = topographySettings.noiseLacunarityRange;
        Vector2 pesistenceRange = topographySettings.noisePersistanceRange;
        
        Random.InitState(WorldGenerationSettings.worldSeed);
        heightMapGenerator.frequency = Random.Range(frequencyRange.x, frequencyRange.y);
        heightMapGenerator.lacunarity = Random.Range(lacunarityRange.x, lacunarityRange.y);
        heightMapGenerator.persistence = Random.Range(pesistenceRange.x, pesistenceRange.y);
        
        heightMapGenerator.noiseType = FastNoiseLite.NoiseType.OpenSimplex2;
        heightMapGenerator.fractalType = FastNoiseLite.FractalType.FBm;
        heightMapGenerator.amplitude = topographySettings.baseMaxHeight;
        heightMapGenerator.octaves = topographySettings.noiseOctaves;
        heightMapGenerator.mapScale = topographySettings.heightMapScale;
        heightMapGenerator.ApplySettings();
        return heightMapGenerator;
    }
}
