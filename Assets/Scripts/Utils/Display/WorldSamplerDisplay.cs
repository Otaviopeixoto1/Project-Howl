using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WorldSampler))]
public class WorldSamplerDisplay : MonoBehaviour
{
    [SerializeField]
    private MeshRenderer displayRenderer;
    [SerializeField]
    private MeshFilter displayFilter;
    [SerializeField]
    private Material displayMaterial;

    void Awake()
    {
        gameObject.SetActive(false);
    }


    public void Display()
    {
        LoadSamplers();
        DisplayTerrainMesh();
        DisplayMap();

    }
    public void LoadSamplers()
    {
        BiomeManager biomeManager = GetComponent<BiomeManager>();
        biomeManager.Load();
    }

    public void DisplayTerrainMesh()// get world coordinates as input !
    {
        WorldSampler worldSampler = GetComponent<WorldSampler>();
        MeshData meshData = MeshGenerator.GenerateTerrainFromSampler(
            worldSampler,
            241,
            241,
            1f,
            Vector2.zero // offset in world coordinates

        );
        displayFilter.sharedMesh = meshData.CreateMesh();
        displayRenderer.sharedMaterial = displayMaterial;

    }

    public void DisplayMap()
    {
        if (displayRenderer == null)
        {
            return;
        }
        WorldSampler worldSampler = GetComponent<WorldSampler>();

        Texture2D texture = new Texture2D(241,241);

        Color[] colorMap = new Color[241 * 241];

        for (int x = 0; x < 241; x++)
        {
            for (int y = 0; y < 241; y++)
            {
                
                colorMap[x + y * 241] = worldSampler.SampleColor(x,y);
            }
        }
        texture.SetPixels(colorMap);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply();


        displayRenderer.sharedMaterial.mainTexture = texture;
    }
}
