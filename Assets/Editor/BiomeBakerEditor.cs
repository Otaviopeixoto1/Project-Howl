using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BiomeBaker))]
public class BiomeBakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BiomeBaker mapDisp = (BiomeBaker)target;

        DrawDefaultInspector();

        if (GUILayout.Button("Load"))
        {
            mapDisp.LoadBaked();
            mapDisp.DisplayMap(0);
        }
        if (GUILayout.Button("Bake"))
        {
            mapDisp.BakeBiomeCells();
            mapDisp.BakeSingleBiomes();
            mapDisp.DisplayMap(0);
        }
        if (GUILayout.Button("Save"))
        {
            mapDisp.SaveBaked();
        }
    }
}
