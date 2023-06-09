using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator 
{
    public static Texture2D GenerateTextureFromMap(float[,] map, float amplitude)
    {
        int width = map.GetLength(0);
        int height = map.GetLength(1);

        Texture2D texture = new Texture2D(width, height);
        Color[] colorMap = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                //colorMap[y * width + x] = Color.Lerp(Color.black,Color.white,map[x,y]);
                float heightPercent = map[x,y]/amplitude;
                colorMap[y * width + x] = new Color(heightPercent, heightPercent, heightPercent);
            }
        }

        texture.SetPixels(colorMap);
        texture.filterMode = FilterMode.Point; 
        texture.wrapMode = TextureWrapMode.Clamp; 
        texture.Apply();
        
        return texture;
    }
}
