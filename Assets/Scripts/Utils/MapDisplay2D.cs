using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public enum DisplayStyle //deprecate this displayStyle, use delegates. Rename to MapDecoder
{
    Disabled,
    GrayScale,
    RandomColors,
    Gradient
}


[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
public class MapDisplay2D : MapDisplay
{
    private static Dictionary<Type, DisplayStyle[]> allowedStyles = new Dictionary<Type, DisplayStyle[]>()
    {
        {typeof(BiomeMapGenerator), new DisplayStyle[]{DisplayStyle.GrayScale, DisplayStyle.RandomColors, DisplayStyle.Gradient}},
        {typeof(HeightMapGenerator), new DisplayStyle[]{DisplayStyle.GrayScale}}
    };

    [SerializeField]
    private MapGenerator mapGenerator;

    [SerializeField]
    private DisplayStyle displayStyle = DisplayStyle.Disabled;

    [SerializeField]
    [Range(0,241)]
    private int textureWidth = 241;
    [SerializeField]
    [Range(0,241)]
    private int textureHeight = 241;

    [SerializeField]
    [Range(0,10)]
    private float textureScale = 1f;

    void Start()
    {
        
    }

    void Awake()
    {
        gameObject.SetActive(false);
    }


    public static bool IsValidStyle(MapGenerator mapGenerator, DisplayStyle displayStyle)
    {
        foreach(DisplayStyle allowedStyle in allowedStyles[mapGenerator.GetType()])
        {
            if (allowedStyle == displayStyle)
            {
                return true;
            }
        }
        return false;
    }

    public void DrawMap()
    {
        MeshRenderer mapRenderer = GetComponent<MeshRenderer>();
        //float[,] map = mapGenerator.GenerateMap();
        //Texture2D texture = TextureGenerator.GenerateTextureFromMap(map, mapGenerator.amplitude,textureScale);
        Texture2D texture = TextureGenerator.GenerateTextureFromSampler(mapGenerator, 
                                                                        textureWidth, 
                                                                        textureHeight, 
                                                                        textureScale,
                                                                        displayStyle
                                                                        );


        mapRenderer.sharedMaterial.mainTexture = texture;
        mapRenderer.transform.localScale = new Vector3(textureWidth * textureScale, textureHeight * textureScale,1);
    }

    void OnValidate()
    {
        //DrawMap();
        if (mapGenerator != null && !IsValidStyle(mapGenerator,displayStyle))
        {
            Debug.Log("Invalid DisplayStyle: " + displayStyle + ", for map of type: " + mapGenerator.GetType());
            displayStyle = allowedStyles[mapGenerator.GetType()][0];
        }

        
    }
    private void OnEnable()
    {
        // Subscribe to the event when the ScriptableObject updates
        if (mapGenerator != null)
        {
            mapGenerator.updateMap += OnMapUpdate;
        }
        
    }

    private void OnDisable()
    {
        // Unsubscribe from the event when the script is disabled
        if (mapGenerator != null)
        {
            mapGenerator.updateMap -= OnMapUpdate;
        }
    }


    public override void OnMapUpdate()
    {
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
