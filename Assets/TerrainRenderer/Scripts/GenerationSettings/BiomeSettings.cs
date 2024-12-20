using UnityEngine;
using UnityEngine.UIElements;

public enum Biomes
{
    Forest,
    HighMountains,
    Mountains,
    Plains,
    Desert,
    Swamp
}

public struct TopographySettings
{
    public readonly Biomes biomeType;
    public readonly float baseMaxHeight;
    public readonly float heightMapScale;
    public readonly int noiseOctaves;
    public readonly Vector2 noiseFrequencyRange;
    public readonly Vector2 noiseLacunarityRange;
    public readonly Vector2 noisePersistanceRange;

    public TopographySettings(Biomes biomeType, float baseMaxHeight, float heightMapScale, int noiseOctaves, Vector2 noiseFrequencyRange, Vector2 noiseLacunarityRange, Vector2 noisePersistanceRange)
    {
        this.biomeType = biomeType;
        this.baseMaxHeight = baseMaxHeight;
        this.heightMapScale = heightMapScale;
        this.noiseOctaves = noiseOctaves;
        this.noiseFrequencyRange = noiseFrequencyRange;
        this.noiseLacunarityRange = noiseLacunarityRange;
        this.noisePersistanceRange = noisePersistanceRange;
    }
}




public struct PrimaryGenerationSettings
{
    public readonly Texture2D baseTexture; 
    public readonly float baseTextureScale; 

    public PrimaryGenerationSettings(Texture2D baseTexture, float baseTextureScale)
    {
        this.baseTexture = baseTexture;
        this.baseTextureScale = baseTextureScale;
    }
}



//store the height map generation parameters and the biome detains and objects

[CreateAssetMenu(fileName = "BiomeSettings", menuName = "ScriptableObjects/WorldGeneration/BiomeSettings", order = 1)] 
public class BiomeSettings : ScriptableObject
{
    [Header("Primary Settings")]
    public Biomes biome = Biomes.Forest;
    public Texture2D baseTexture;
    public float baseTextureScale = 1f; 


    [Header("Topography Settings")]

    [Range(0.01f,10f)]
    public float heightMapScale = 5;

    [Range(1f,20f)]
    public float baseMaxHeight =  1f;

    [Range(1, 10)]
    public int noiseOctaves = 4;

    //Random range properties:
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
    

    [Header("Terrain Object Generation Settings")]
    public TerrainDetailSettings terrainDetailSettings;



    public static BiomeSettings HighMountains { 
        get 
        {
            BiomeSettings HighMountainsSettings = ScriptableObject.CreateInstance<BiomeSettings>();
            HighMountainsSettings.biome = Biomes.HighMountains;
            HighMountainsSettings.baseMaxHeight = 30f;
            HighMountainsSettings.noiseFrequencyRange = new Vector2(0.8f, 1f);
            HighMountainsSettings.noiseLacunarityRange = new Vector2(1f, 1.5f);
            HighMountainsSettings.noisePersistenceRange = new Vector2(1f, 1.5f);
            return HighMountainsSettings;
        } 
    }

    void OnValidate()
    {

    }


    public PrimaryGenerationSettings GetPrimarySettings()
    {
        return new PrimaryGenerationSettings(baseTexture, baseTextureScale);
    }

    public TopographySettings GetTopographySettings()
    {
        return new TopographySettings(
            biome,
            baseMaxHeight, 
            heightMapScale,
            noiseOctaves,
            noiseFrequencyRange,
            noiseLacunarityRange,
            noisePersistenceRange
        );
    }


    
}
