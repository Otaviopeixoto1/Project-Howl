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
    private BiomeSampler fullBiomeMap = null;



    public void Load()
    {
        bakedBiomes.Clear();

        BiomeSamplerData samplerData = BiomeBaker.LoadBaked();
        if (samplerData == null)
        {
            Debug.Log("Error Loading BiomeSamplers");
            return;
        }
        
        bakedBiomes = samplerData.singleBiomeSamplers;
        fullBiomeMap = samplerData.fullBiomeMapSampler;
    }

    public void Bake()
    {
        bakedBiomes.Clear();
        fullBiomeMap = BiomeBaker.BakeBiomeCells(biomeMapGenerator);
        bakedBiomes = BiomeBaker.BakeSingleBiomes(biomeMapGenerator, fullBiomeMap, biomeStretch);

    }

    public void Save()
    {
        BiomeBaker.SaveBaked(biomeMapGenerator.gridDimension, fullBiomeMap, bakedBiomes);

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
