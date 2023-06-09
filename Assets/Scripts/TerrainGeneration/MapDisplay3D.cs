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
    [Range(0,241)]
    private int meshWidth = 241;
    [SerializeField]
    [Range(0,241)]
    private int meshHeight = 241;

    [SerializeField]
    [Range(0.1f,20)]
    private float meshScale = 1f; 

    [SerializeField]
    [Range(0,8)]
    private int lodBias = 0; 


    private bool updateMesh = true;

    private Mesh currentMesh = null;


    public void DrawMesh()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();

        if (currentMesh == null) //check if the mesh has to be generated from scratch
        {
            //float[,] map = mapGenerator.GenerateMap();
            //MeshData meshData = MeshGenerator.GenerateTerrainFromMap(map, meshScale);
            MeshData meshData = TerrainMeshGenerator.GenerateTerrainFromSampler(mapGenerator, meshWidth, meshHeight, meshScale);

            currentMesh = meshData.CreateMesh();//check if the mesh exists, if it does, then just update the
                                                // vertices
                                                //if the meshDensity changed, then we generate the mesh again


            currentMesh.MarkDynamic();
            meshFilter.sharedMesh = currentMesh;
            
            meshRenderer.sharedMaterial = mapMaterial;
            meshRenderer.sharedMaterial.mainTexture 
                            = TextureGenerator.GenerateTextureFromSampler(mapGenerator, meshWidth, meshHeight, meshScale);
            
        }
        else
        {
            Debug.Log("updating Verts");
            Vector3[] vertices = TerrainMeshGenerator.CalculateMeshVertices(mapGenerator, meshWidth, meshHeight, meshScale);
            currentMesh.SetVertices(vertices);
            currentMesh.RecalculateNormals();
            meshRenderer.sharedMaterial = mapMaterial;
            meshRenderer.sharedMaterial.mainTexture 
                            = TextureGenerator.GenerateTextureFromSampler(mapGenerator, meshWidth, meshHeight, meshScale);
            
        }
        meshRenderer.transform.localScale = new Vector3(meshScale, meshScale, meshScale);
    }

    void OnValidate()
    {
        if (autoUpdate)
        {
            mapGenerator.updateMap += OnMeshUpdate;
            updateMesh = true;
        }
        else
        {
            mapGenerator.updateMap -= OnMeshUpdate;
        }
        currentMesh = null;
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
