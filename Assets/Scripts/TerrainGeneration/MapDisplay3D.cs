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
    private Material mapMaterial;


    [SerializeField]
    private float meshScale = 1f; //use a get set

    private bool updateMesh = true;

    private Mesh currentMesh = null;


    public void DrawMesh()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();

        //float[,] map = mapGenerator.GenerateMap();
        //MeshData meshData = MeshGenerator.GenerateTerrainFromMap(map, meshScale);
        MeshData meshData = TerrainMeshGenerator.GenerateTerrainFromSampler(mapGenerator, meshScale);

        currentMesh = meshData.CreateMesh();//check if the mesh exists, if it does, then just update the
                                            // vertices
                                            //if the meshDensity changed, then we generate the mesh again

        //currentMesh.MarkDynamic();
        meshFilter.sharedMesh = currentMesh;
        
        meshRenderer.sharedMaterial = mapMaterial;
        meshRenderer.sharedMaterial.mainTexture = TextureGenerator.GenerateTextureFromSampler(mapGenerator, meshScale);
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
