using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapComposer))]
public class MapComposerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapComposer mapDisp = (MapComposer)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Generate"))
        {
            mapDisp.GenerateHeightMaps();
            mapDisp.GenerateTerrainMesh();
        }
    }
}

