using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;


public enum Biomes
{

}



[Serializable]
public class BiomeSampler
{
    public readonly int id;
    public string name;
    public HeightMapGenerator heightMap;
    private Texture2D map;
    public Color displayColor;


    public BiomeSampler(int id, Texture2D map)
    {
        this.id = id;
        this.map = map;
    }

    public float Sample(float x, float y)
    {
        return map.GetPixel(Mathf.RoundToInt(x),Mathf.RoundToInt(y)).r;
    }

    public Texture2D GetMap()
    {
        return map;
    }

    public int GetSize()
    {
        return map.width;
    }

    public void SerializeToJSON()
    {

    }
}

public class BiomeBaker : MonoBehaviour
{
    [SerializeField]
    private BiomeMapGenerator biomeMapGenerator;

    [SerializeField]
    [Range(1,2)]
    private float biomeScale = 1.5f;

    [SerializeField]
    private int currentDisplay = 0;

    [NonReorderable]
    [SerializeField]
    private List<BiomeSampler> bakedBiomes = new List<BiomeSampler>();
    private BiomeSampler fullBiomeMap = null;


    void Start()
    {
        LoadBaked();
    }

    public void LoadBaked()
    {

    }


    public void BakeBiomeCells()
    {
        if (biomeMapGenerator == null)
        {
            return;
        }
        Texture2D fullMap = biomeMapGenerator.GetBiomeIndexMap();


        fullBiomeMap = new BiomeSampler(-1,fullMap);
    }

    public void BakeSingleBiomes()
    {
        bakedBiomes.Clear();
        if (biomeMapGenerator != null && fullBiomeMap != null)
        {
            int size = biomeMapGenerator.gridDimension;
            for (int i = 0; i <= size * (size + 2); i++)
            {
                Texture2D singleMap = biomeMapGenerator.GetSingleBiomeMap(i, biomeScale, fullBiomeMap);
                bakedBiomes.Add(new BiomeSampler(i, singleMap));
            }
            
        }
    }

    public void SaveBaked()
    {

    }

    public void DisplayMap(int index)
    {
        MeshRenderer mapRenderer = GetComponent<MeshRenderer>();
        if (mapRenderer == null || bakedBiomes.Count == 0 || bakedBiomes[index] == null)
        {
            return;
        }
        Texture2D texture = bakedBiomes[index].GetMap();
        texture.Apply();
        mapRenderer.sharedMaterial.mainTexture = texture;
        //mapRenderer.transform.localScale = new Vector3(textureWidth * textureScale, textureHeight * textureScale,1);
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
