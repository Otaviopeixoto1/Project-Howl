using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DetailMeshType
{
    Quad,
    Custom
}

//add this to the biome generation settings SO of each biome

[CreateAssetMenu(fileName = "DetailSettings", menuName = "ScriptableObjects/WorldGeneration/DetailSettings", order = 10)] 
public class TerrainDetailSettings : ScriptableObject
{
    public float density = 1f;//amount of grass quads generated per vertex on the terrain chunk mesh
    public DetailMeshType meshType = DetailMeshType.Quad;
    public Material material;

    [Header("Atlas Texture Settings")]
    public Texture2D atlasTexture;
    public Vector2Int size;
    public Vector2Int offset;

}
