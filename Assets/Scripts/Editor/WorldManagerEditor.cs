using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldManager))]
public class WorldManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WorldManager biomeManager = (WorldManager)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Generate"))
        {
            biomeManager.GenerateBiomeMap();
        }
         
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
