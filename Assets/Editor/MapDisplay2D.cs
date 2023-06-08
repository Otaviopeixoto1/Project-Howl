using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapDisplay2D),true)]
public class MapDisplay2DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapDisplay2D mapDisp = (MapDisplay2D)target;

        DrawDefaultInspector();
        /*
        if (DrawDefaultInspector() && mapGen.autoUpdate)
        {
            mapGen.DisplayMap();
        }*/

        if (GUILayout.Button("Generate"))
        {
            mapDisp.DrawMap();
        }
    }
}
