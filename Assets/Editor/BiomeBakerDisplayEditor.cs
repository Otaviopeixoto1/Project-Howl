using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BiomeBakerDisplay))]
public class BiomeBakerDisplayEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BiomeBakerDisplay mapDisp = (BiomeBakerDisplay)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Load"))
        {
            mapDisp.Load();
            mapDisp.DisplayMap(0);
        }
        if (GUILayout.Button("Bake"))
        {
            mapDisp.Bake();
            mapDisp.DisplayMap(0);
        }
        if (GUILayout.Button("Save"))
        {
            mapDisp.Save();
        }
    }
}
