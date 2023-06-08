using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class MapDisplay2D : MonoBehaviour
{
    public bool autoUpdate = true;

    [SerializeField]
    private Renderer mapRenderer;

    [SerializeField]
    private MapGenerator mapProperties;



    public void DrawMap()
    {
        float[,] map = mapProperties.GenerateMap();
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        Texture2D texture = new Texture2D(width, height);
        Color[] colorMap = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                //colorMap[y * width + x] = Color.Lerp(Color.black,Color.white,map[x,y]);
                colorMap[y * width + x] = new Color(map[x,y],map[x,y],map[x,y]);
            }
        }

        texture.SetPixels(colorMap);
        //texture.filterMode = FilterMode.Point; 
        texture.Apply();

        mapRenderer.sharedMaterial.mainTexture = texture;
        mapRenderer.transform.localScale = new Vector3(width,height,1);
        //return texture. if there is a new texture it will be drawn
    }

    void Update()
    {
        if(mapProperties != null && autoUpdate && mapProperties.triggerUpdate)
        {
            DrawMap();
            mapProperties.triggerUpdate = false;
        }
    }

}
