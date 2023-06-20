using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WorldSamplerDisplay))]
public class WorldSamplerDisplayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WorldSamplerDisplay worldSamplerDisp = (WorldSamplerDisplay)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Display"))
        {
            worldSamplerDisp.Display();
        }
    }
}
