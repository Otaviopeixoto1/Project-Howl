using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
public class MapDisplay2D : MapDisplay
{
    private MeshRenderer mapRenderer;
    [SerializeField]
    private MapGenerator mapGenerator;


    [SerializeField]
    [Range(0,241)]
    private int textureWidth = 241;
    [SerializeField]
    [Range(0,241)]
    private int textureHeight = 241;

    [SerializeField]
    [Range(0,10)]
    private float textureScale = 1f;




    public void DrawMap()
    {
        mapRenderer = GetComponent<MeshRenderer>();
        //float[,] map = mapGenerator.GenerateMap();
        //Texture2D texture = TextureGenerator.GenerateTextureFromMap(map, mapGenerator.amplitude,textureScale);
        Texture2D texture = TextureGenerator.GenerateTextureFromSampler(mapGenerator, textureWidth, textureHeight, textureScale);


        mapRenderer.sharedMaterial.mainTexture = texture;
        mapRenderer.transform.localScale = new Vector3(textureWidth * textureScale, textureWidth * textureScale,1);
    }

    void OnValidate()
    {
        //DrawMap();
        


        
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
        Debug.Log("Update Map");
        if(mapGenerator != null && autoUpdate)
        {
            DrawMap();
        }
    }

    /*
    void Update()
    {
        if(mapGenerator != null && autoUpdate && mapGenerator.triggerUpdate)
        {
            DrawMap();
            mapGenerator.triggerUpdate = false;
        }
    }
    */
}
