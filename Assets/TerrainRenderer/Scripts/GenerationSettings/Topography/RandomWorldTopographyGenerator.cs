using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RandomWorldTopographyGenerator", menuName = "ScriptableObjects/WorldGeneration/RandomWorldTopographyGenerator", order = 3)]
public class RandomWorldTopographyGenerator : WorldTopographyGenerator
{
    [Range(0.01f,10f)]
    public float heightMapScale = 5;

    [Range(1, 10)]
    public int noiseOctaves = 4;

    //Random range properties:
    #if UNITY_EDITOR 
    [MinMax(1f,50)]
    #endif
    public Vector2 noiseAmplitudeRange;
    #if UNITY_EDITOR 
    [MinMax(0.01f,2)]
    #endif
    public Vector2 noiseFrequencyRange;
    #if UNITY_EDITOR 
    [MinMax(0.01f,2)]
    #endif
    public Vector2 noiseLacunarityRange;
    #if UNITY_EDITOR 
    [MinMax(0.01f,10)]
    #endif
    public Vector2 noisePersistenceRange;
    

    public override HeightMapGenerator GetHeightMapGenerator(TopographySettings topographySettings)
    {
        Random.InitState(WorldGenerationSettings.worldSeed);
        HeightMapGenerator heightMapGenerator = ScriptableObject.CreateInstance<HeightMapGenerator>();
        heightMapGenerator.noiseSeed = WorldGenerationSettings.worldSeed;
        heightMapGenerator.frequency = Random.Range(noiseFrequencyRange.x, noiseFrequencyRange.y);
        heightMapGenerator.lacunarity = Random.Range(noiseLacunarityRange.x, noiseLacunarityRange.y);
        heightMapGenerator.persistence = Random.Range(noisePersistenceRange.x, noisePersistenceRange.y);
        heightMapGenerator.amplitude = Random.Range(noiseAmplitudeRange.x, noiseAmplitudeRange.y);
        heightMapGenerator.octaves = noiseOctaves;
        heightMapGenerator.mapScale = heightMapScale;
        heightMapGenerator.ApplySettings();
        return heightMapGenerator;
    }
}
