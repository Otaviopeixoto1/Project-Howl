using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;


    private int currentTriangleIndex;
    public MeshData(int width, int height)
    {
        vertices = new Vector3[width * height];
        uvs = new Vector2[width * height];
        triangles = new int[(width - 1) * (height - 1) * 6];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[currentTriangleIndex] = a;
        triangles[currentTriangleIndex + 1] = b;
        triangles[currentTriangleIndex + 2] = c;
        currentTriangleIndex += 3;    
    }   

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        return mesh;
    }

}



public static class MeshGenerator
{
    public static MeshData GenerateTerrainFromMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        MeshData meshData = new MeshData(width, height);
        int vertexIndex = 0;
        
        Vector3 centerOffset = new Vector3(-(width - 1)/2f, 0, -(height - 1)/2f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                meshData.vertices[vertexIndex] = new Vector3(x, heightMap[x, y], y) + centerOffset;
                meshData.uvs[vertexIndex] = new Vector2(x/(float)width, y/(float)height);

                if (x < (width - 1) && y < (height - 1))
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + width, vertexIndex + width + 1 );
                    meshData.AddTriangle(vertexIndex, vertexIndex + width + 1, vertexIndex + 1 );
                }


                vertexIndex++;
            }
        }

        return meshData;
    }
}
