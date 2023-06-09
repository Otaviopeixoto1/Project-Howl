using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TextureGenerator 
{
    public static Texture2D GenerateTextureFromSampler(MapGenerator sampler, int textureWidth, int textureHeight, float textureScale)
    {
        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        Color[] colorMap = new Color[textureWidth * textureHeight];

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                //colorMap[y * width + x] = Color.Lerp(Color.black,Color.white,map[x,y]);
                float heightPercent = sampler.SampleMap(x - (textureWidth - 1)/2, y - (textureHeight - 1)/2)/sampler.amplitude;
                colorMap[y * textureWidth + x] = new Color(heightPercent, heightPercent, heightPercent);
            }
        }

        texture.SetPixels(colorMap);
        texture.filterMode = FilterMode.Point; 
        texture.wrapMode = TextureWrapMode.Clamp; 
        texture.Apply();
        
        return texture;
    }
    public static Texture2D GenerateTextureFromMap(float[,] map, float amplitude, float textureScale)
    {
        int width = (int)(map.GetLength(0) * textureScale);
        int height = (int)(map.GetLength(1) * textureScale);

        Texture2D texture = new Texture2D(width, height);
        Color[] colorMap = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                //colorMap[y * width + x] = Color.Lerp(Color.black,Color.white,map[x,y]);
                float heightPercent = map[x, y]/amplitude;
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
