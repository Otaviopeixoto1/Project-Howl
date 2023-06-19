using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldSampler))]
public class MapComposerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WorldSampler worldSampler = (WorldSampler)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Display"))
        {
            worldSampler.DisplayTerrainMesh();
        }
    }
}

