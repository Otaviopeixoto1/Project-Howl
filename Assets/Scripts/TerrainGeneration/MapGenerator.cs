using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class MapGenerator : ScriptableObject
{
    [HideInInspector]
    public bool triggerUpdate = false; 

    public float mapScale = 2f;
    public int mapWidth = 20;
    public int mapHeight = 20;

    public virtual float[,] GenerateMap()
    {
        return null;
    }
}


