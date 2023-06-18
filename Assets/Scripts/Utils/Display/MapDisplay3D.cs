using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MapDisplay3D : MapDisplay
{
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

        //meshRenderer.transform.localScale = new Vector3(meshScale, meshScale, meshScale);

        if (currentMesh == null) //check if the mesh has to be generated from scratch
        {
            //float[,] map = mapGenerator.GenerateMap();
            //MeshData meshData = MeshGenerator.GenerateTerrainFromMap(map, meshScale);
            MeshData meshData = MeshGenerator.GenerateTerrainFromSampler(mapGenerator, meshWidth, meshHeight, meshScale, lodBias);

            currentMesh = meshData.CreateMesh();
            currentMesh.MarkDynamic();
            meshFilter.sharedMesh = currentMesh;
            
            meshRenderer.sharedMaterial = mapMaterial;
            meshRenderer.sharedMaterial.mainTexture 
                            = TextureGenerator.GenerateTextureFromSampler(mapGenerator, meshWidth, meshHeight, meshScale);
            
        }
        else
        {
            //Debug.Log("updating Verts");
            Vector3[] vertices = MeshGenerator.CalculateMeshVertices(mapGenerator, meshWidth, meshHeight, meshScale,lodBias);
            currentMesh.SetVertices(vertices);
            currentMesh.RecalculateNormals();
            meshRenderer.sharedMaterial = mapMaterial;
            meshRenderer.sharedMaterial.mainTexture 
                            = TextureGenerator.GenerateTextureFromSampler(mapGenerator, meshWidth, meshHeight, meshScale);
            
        }
        
    }

    void OnValidate()
    {
        if (autoUpdate)
        {
            updateMesh = true;
        }
        currentMesh = null;
    }

    private void OnEnable()
    {
        // Subscribe to the event when the ScriptableObject updates
        mapGenerator.updateMap += OnMapUpdate;
    }

    private void OnDisable()
    {
        // Unsubscribe from the event when the script is disabled
        mapGenerator.updateMap -= OnMapUpdate;
    }

    public override void OnMapUpdate()
    {
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
