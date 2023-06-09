using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class MapDisplay2D : MonoBehaviour
{
    public bool autoUpdate = true;

    private Renderer mapRenderer;

    [SerializeField]
    private MapGenerator mapGenerator;



    public void DrawMap()
    {
        mapRenderer = GetComponent<MeshRenderer>();
        float[,] map = mapGenerator.GenerateMap();
        int width = map.GetLength(0);
        int height = map.GetLength(1);
        Texture2D texture = TextureGenerator.GenerateTextureFromMap(map, mapGenerator.amplitude);

        mapRenderer.sharedMaterial.mainTexture = texture;
        mapRenderer.transform.localScale = new Vector3(width,height,1);
    }

    void OnValidate()
    {
        mapGenerator.updateMap += OnMapUpdate;
    }

    public void OnMapUpdate()
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
