using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DetailMeshType
{
    Quad,
    Custom
}

[CreateAssetMenu(fileName = "DetailSettings", menuName = "ScriptableObjects/WorldGeneration/DetailSettings", order = 10)] 

//THIS SHOULD BE A STRUCT !
public class TerrainDetailSettings : ScriptableObject
{
    //amount of quads generated per vertex on the terrain chunk mesh:
    public float density = 1f;
    public DetailMeshType meshType = DetailMeshType.Quad;

    [Tooltip("This material will override the default material used in the detail rendering")]
    public Material materialOverride; 

    public Vector2Int atlasOffset;
    public Vector2Int size;
    public int numVariants;

}
