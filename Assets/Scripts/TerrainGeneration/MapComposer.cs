using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[RequireComponent(typeof(BiomeManager))]
public class MapComposer : MonoBehaviour
{
    [SerializeField]
    private MeshRenderer meshRenderer;
    [SerializeField]
    private MeshFilter meshFilter;
    [SerializeField]
    private BiomeManager biomeManager;

    [SerializeField]
    private Material mapMaterial; 


    [SerializeField]
    [Range(1,50)]
    private float heightMultiplier = 1f;

    [SerializeField]
    [Range(1,241)]
    private int meshSize = 241;
    [SerializeField]
    [Range(0.01f,5f)]
    private float meshScale = 1f;
    [SerializeField]
    [Range(0.01f,5f)]
    private float mapScale = 1f;



    public void GenerateHeightMaps()
    {
        if (biomeManager != null)
        {
            int gridSize = biomeManager.GetGridSize();
            for (int i = 0; i <= gridSize * (gridSize + 2); i++)
            {
                HeightMapGenerator heightMapGenerator = ScriptableObject.CreateInstance<HeightMapGenerator>();
                heightMapGenerator.frequency = Random.Range(0.01f, 1.0f);
                heightMapGenerator.amplitude = Random.Range(1f, 20.0f) * heightMultiplier;
                heightMapGenerator.mapScale = mapScale;
                biomeManager.SetBiomeHeightMap(i,heightMapGenerator);
            }
        }
    }

    public void GenerateTerrainMesh()// get world coordinates as input !
    {
        if (biomeManager != null)
        {
            MeshData meshData = MeshGenerator.GenerateTerrainFromSampler(
                biomeManager,
                meshSize,
                meshSize,
                meshScale,
                0,
                false
            );
            meshFilter.sharedMesh = meshData.CreateMesh();
            meshRenderer.sharedMaterial = mapMaterial;


        }
    }

    void OnValidate()
    {

    }

    void Start()
    {
        biomeManager = GetComponent<BiomeManager>();
    }

    void Update()
    {
        
    }
}
