using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BiomeManager))]
public class BiomeManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BiomeManager mapDisp = (BiomeManager)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Load"))
        {
            mapDisp.Load();
        }
        if (GUILayout.Button("Save"))
        {
            mapDisp.Save();
        }
    }
}
