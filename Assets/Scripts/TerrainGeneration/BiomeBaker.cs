using System.Collections;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEngine;




[Serializable]
public class BiomeSampler
{
    public readonly int id;
    public string name;

    private string heightMapPath;
    public HeightMapGenerator heightMap;
    private string biomeMapPath;
    private Texture2D biomeMap;
    
    public Color displayColor;


    public BiomeSampler(int id, Texture2D biomeMap)
    {
        this.id = id;
        this.biomeMap = biomeMap;
    }

    public BiomeSampler(BiomeData bData)
    {
        this.id = bData.id;

        heightMapPath = bData.heightMapPath;

        //this.heightMap = heightMap; //check if file exists before assign map



        biomeMapPath = bData.biomeMapPath;

        Texture2D tex = null;
        byte[] fileData;

        if (System.IO.File.Exists(biomeMapPath))
        {
            fileData = System.IO.File.ReadAllBytes(biomeMapPath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        else
        {
            Debug.Log("missing texture for biome: " + id);
        }
        
        this.biomeMap = tex; //check if file exists before assign map



        this.name = bData.name;
        this.displayColor = bData.displayColor;
        
    }

    public float SampleBiome(float x, float y)
    {
        return biomeMap.GetPixel(Mathf.RoundToInt(x),Mathf.RoundToInt(y)).r;
    }

    public Texture2D GetMap()
    {
        return biomeMap;
    }

    public int GetSize()
    {
        return biomeMap.width;
    }

    public BiomeData Save()
    {
        if (name == null)
        {
            name = "map" + id;
        }
        if (biomeMap != null)
        {
            biomeMapPath = Application.dataPath + "/Map/BiomeMaps/map" + id + ".png";
            System.IO.File.WriteAllBytes(biomeMapPath, biomeMap.EncodeToPNG());
            //return SerializeToJSON();
        }
        else
        {
            Debug.Log("Null Map texture for sampler id = " + id);
            //return "";
        }

        BiomeData bData = new BiomeData(id, name, heightMapPath, biomeMapPath, displayColor);
        return bData;
    }

    /*
    public string SerializeToJSON()
    {
        BiomeData bData = new BiomeData(id, name, heightMapPath, biomeMapPath, displayColor);

        string jsonData = JsonUtility.ToJson(bData);
        return jsonData;
    }*/
    
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
        //LoadBaked();
    }

    public void LoadBaked()
    {
        bakedBiomes.Clear();
        string biomesString = File.ReadAllText(Application.dataPath + "/Map/BiomeMaps/mapdata.json");
        BiomeMapData biomeMapData = JsonUtility.FromJson<BiomeMapData>(biomesString);

        fullBiomeMap = new BiomeSampler(biomeMapData.fullbiomeMapData);

        foreach (BiomeData biomeData in biomeMapData.biomeMaps)
        {
            bakedBiomes.Add(new BiomeSampler(biomeData));
        }

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
        BiomeData fullBiomeMapData;

        if (fullBiomeMap != null)
        {
            fullBiomeMapData = fullBiomeMap.Save();
        }
        else
        {
            Debug.Log("Full biome map is null");
            return;
        }

        BiomeData[] biomeMaps = new BiomeData[bakedBiomes.Count];

        for (int i = 0; i < (bakedBiomes.Count); i++)
        {
            biomeMaps[i] = bakedBiomes[i].Save();
        }
        
        BiomeMapData bmd = new BiomeMapData(biomeMapGenerator.gridDimension,fullBiomeMapData,biomeMaps);

        File.WriteAllText(Application.dataPath + "/Map/BiomeMaps/mapdata.json", JsonUtility.ToJson(bmd));


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
