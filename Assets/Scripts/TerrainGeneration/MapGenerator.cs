using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class MapGenerator : ScriptableObject
{
    [HideInInspector]
    public delegate void UpdateTrigger();
    [HideInInspector]
    public UpdateTrigger updateMap;


    [HideInInspector]
    public bool triggerUpdate = false; 

    public float mapScale = 2f;
    public int mapWidth = 20;
    public int mapHeight = 20;

    [Range(0, 20)]
    public float amplitude = 1f;


    public virtual float[,] GenerateMap()
    {
        return null;
    }

    void OnValidate()
    {
        if (updateMap != null)
        {
            updateMap();
        }
    }
}


