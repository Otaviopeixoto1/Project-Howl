using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiomeBakerDisplay : MonoBehaviour
{
    [SerializeField]
    private BiomeMapGenerator biomeMapGenerator;

    [SerializeField]
    [Range(1,2)]
    private float biomeStretch = 1.2f;

    [SerializeField]
    private int currentDisplay = 0;

    [NonReorderable]
    [SerializeField]
    private List<BiomeSampler> bakedBiomes = new List<BiomeSampler>();
    private BiomeSampler biomeIdSampler = null;



    public void Load()
    {
        bakedBiomes.Clear();

        BiomeMapBaker.BiomeSamplersData samplerData = BiomeMapBaker.LoadBaked();
        if (samplerData == null)
        {
            Debug.Log("Error Loading BiomeSamplers");
            return;
        }
        
        bakedBiomes = samplerData.singleBiomeSamplers;
        biomeIdSampler = samplerData.biomeIdSampler;
    }

    public void Bake()
    {
        bakedBiomes.Clear();
        biomeIdSampler = BiomeMapBaker.BakeBiomeCellIds(biomeMapGenerator);
        DisplayFullMap();
        bakedBiomes = BiomeMapBaker.BakeSingleBiomes(biomeMapGenerator, biomeIdSampler, biomeStretch);

    }

    public void Save()
    {
        BiomeMapBaker.SaveBaked(biomeMapGenerator.gridDimension, biomeIdSampler, bakedBiomes, saveHeights:false);

    }
    public void DisplayFullMap()
    {
        //biomeIdSampler.GetMap().Apply();
        MeshRenderer mapRenderer = GetComponent<MeshRenderer>();

        Texture2D tex = new Texture2D(241,241);

        tex.SetPixels(biomeIdSampler.biomeMapThreaded);
        tex.Apply();
        mapRenderer.sharedMaterial.mainTexture = tex; 
    }


    public void DisplayMap(int index)
    {
        MeshRenderer mapRenderer = GetComponent<MeshRenderer>();
        if (mapRenderer == null || bakedBiomes.Count == 0 || bakedBiomes[index] == null)
        {
            return;
        }
        Texture2D texture = bakedBiomes[index].GetMap();

        if (texture != null)
        {
            texture.Apply();
            mapRenderer.sharedMaterial.mainTexture = texture; 
        }
        
    }
    private void OnEnable()
    {
        bakedBiomes.Clear();
        currentDisplay = 0;
    }

    private void OnDisable()
    {
        bakedBiomes.Clear();
        currentDisplay = 0;
    }

    void OnValidate()
    {
        if (currentDisplay >=0 && currentDisplay < bakedBiomes.Count)
        {
            DisplayMap(currentDisplay);
        }
        else
        {
            currentDisplay = Mathf.Min(Mathf.Max(0,currentDisplay),bakedBiomes.Count - 1);
            DisplayMap(currentDisplay);
        }

    }

}
