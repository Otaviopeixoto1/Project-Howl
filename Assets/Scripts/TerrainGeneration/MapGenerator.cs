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

    [Range(0,0.1f)]
    public float mapScale = 1/40f;

    [Range(0,241)]
    public int mapWidth = 20;
    [Range(0,241)]
    public int mapHeight = 20;

    [Range(0, 20)]
    public float amplitude = 1f;


    public virtual void ApplySettings()
    {
        return;
    }

    public virtual float SampleMap(float x, float y)
    {
        return 0f;
    }

    public virtual float[,] GenerateMap()
    {
        return null;
    }

    void OnValidate()
    {
        ApplySettings();

        if (updateMap != null)
        {
            updateMap();
        }
    }
}


