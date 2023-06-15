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

    [Range(0.01f,0.1f)]
    public float mapScale = 0.0682f;

    [Range(0, 20)]
    public float amplitude = 14.9f;

    public AnimationCurve samplingCurve;


    public virtual void ApplySettings()
    {
        return;
    }

    public virtual float SampleMap(float x, float y, AnimationCurve samplingCurve = null)
    {
        return 0f;
    }

    public virtual float[,] GenerateMap(int mapWidth, int mapHeight)
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


