using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BiomeManager))]
public class BiomeManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BiomeManager biomeManager = (BiomeManager)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Load"))
        {
            biomeManager.Load();
        }
        if (GUILayout.Button("Save"))
        {
            biomeManager.Save();
        }
    }
}
