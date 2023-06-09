using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MapDisplay3D : MonoBehaviour
{
    public bool autoUpdate = true;

    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;


    [SerializeField]
    private MapGenerator mapGenerator;

    [SerializeField]
    private float amplitudeMultiplier;

    [SerializeField]
    private Material mapMaterial;


    private bool updateMesh = true;


    public void DrawMesh()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();

        float[,] map = mapGenerator.GenerateMap();
        MeshData meshData = MeshGenerator.GenerateTerrainFromMap(map);

        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial = mapMaterial;
        meshRenderer.sharedMaterial.mainTexture = TextureGenerator.GenerateTextureFromMap(map, mapGenerator.amplitude );
    }

    void OnValidate()
    {
        mapGenerator.updateMap += OnMeshUpdate;
    }
    public void OnMeshUpdate()
    {
        Debug.Log("Update Mesh");
        if(mapGenerator != null && autoUpdate)
        {
            updateMesh = true;
        }
    }

    void Update()
    {
        if (updateMesh)
        {
            DrawMesh();
            updateMesh = false;
        }
    }


}
