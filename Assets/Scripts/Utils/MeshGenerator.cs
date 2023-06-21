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

        //Debug.Log("total vertices: " + width * height +" total triangles, " + (width - 1) * (height - 1) * 6);
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

    public static int CalculateLodIncrement(int dim, int lodBias)
    {
        int[] dimDivisors = Divisors.GetDivisorsButN(dim - 1);

        int increment;

        if (lodBias > (dimDivisors.Length - 1))
        {
            increment = dimDivisors[dimDivisors.Length - 1];
        }
        else
        {
            increment = dimDivisors[lodBias];
        }

        return increment;
    }
    

    //Using MapGenerator as sampler:

    public static MeshData GenerateTerrainFromSampler(MapGenerator sampler, int meshWidth, int meshHeight, float meshScale, int lodBias = 0,bool isThread = false)
    {
        AnimationCurve heightCurve;
        if(isThread)
        {
           heightCurve = new AnimationCurve(sampler.samplingCurve.keys);
        }
        else
        {
            heightCurve = sampler.samplingCurve;
        }
        

        int widthIncrement = CalculateLodIncrement(meshWidth,lodBias);
        int heightIncrement = CalculateLodIncrement(meshHeight,lodBias);

        //Debug.Log( widthIncrement +", " + heightIncrement);

        int widthVertices = ((meshWidth - 1)/widthIncrement) + 1;
        int heightVertices = ((meshHeight - 1)/heightIncrement) + 1;
        MeshData meshData = new MeshData(widthVertices, heightVertices);
        

        Vector3 centerOffset = new Vector3(-(meshWidth - 1)/2f, 0, -(meshHeight - 1)/2f);

        int vertexIndex = 0;

        for (int y = 0; y < meshHeight; y += heightIncrement)
        {
            for (int x = 0; x < meshWidth; x += widthIncrement)
            {
                meshData.vertices[vertexIndex] = (new Vector3(x, sampler.SampleMap(x - (meshWidth - 1)/2, y - (meshHeight - 1)/2,heightCurve), y) 
                                                + centerOffset) * meshScale;
                meshData.uvs[vertexIndex] = new Vector2(x/(float)meshWidth, y/(float)meshHeight);

                if (x < (meshWidth - 1) && y < (meshHeight - 1))
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + widthVertices, vertexIndex + widthVertices + 1 );
                    meshData.AddTriangle(vertexIndex, vertexIndex + widthVertices + 1, vertexIndex + 1 );
                }


                vertexIndex++;
            }
        }

        return meshData;
        
    }

    
    //using BiomeManager as sampler:

    public static MeshData GenerateTerrainFromSampler(WorldSampler sampler, int meshWidth, int meshHeight, float meshScale, Vector2 sampleOffset, int lodBias = 0, bool isThread = false)
    {
        int widthIncrement = CalculateLodIncrement(meshWidth,lodBias);
        int heightIncrement = CalculateLodIncrement(meshHeight,lodBias);


        int widthVertices = ((meshWidth - 1)/widthIncrement) + 1;
        int heightVertices = ((meshHeight - 1)/heightIncrement) + 1;
        MeshData meshData = new MeshData(widthVertices, heightVertices);

        Vector3 centerOffset = new Vector3(-(meshWidth - 1)/2f, 0, -(meshHeight - 1)/2f);

        int vertexIndex = 0;

        for (int y = 0; y < meshHeight; y += heightIncrement)
        {
            for (int x = 0; x < meshWidth; x += widthIncrement)
            {
                meshData.vertices[vertexIndex] = (new Vector3(x, sampler.SampleHeight(x + sampleOffset.x, y + sampleOffset.y), y) 
                                                + centerOffset) * meshScale;
                meshData.uvs[vertexIndex] = new Vector2(x/(float)meshWidth, y/(float)meshHeight);

                if (x < (meshWidth - 1) && y < (meshHeight - 1))
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + widthVertices, vertexIndex + widthVertices + 1 );
                    meshData.AddTriangle(vertexIndex, vertexIndex + widthVertices + 1, vertexIndex + 1 );
                }


                vertexIndex++;
            }
        }

        return meshData;
        
    }



    // A quicker way to just update the heightmap of the mesh:

    public static Vector3[] CalculateMeshVertices(MapGenerator sampler, int meshWidth, int meshHeight, float meshScale, int lodBias)
    {
        int widthIncrement = CalculateLodIncrement(meshWidth,lodBias);
        int heightIncrement = CalculateLodIncrement(meshHeight,lodBias);

        Vector3[] vertices = new Vector3[meshWidth * meshHeight];
        
        Vector3 centerOffset = new Vector3(-(meshWidth - 1)/2f, 0, -(meshHeight - 1)/2f);
        int vertexIndex = 0;

        for (int y = 0; y < meshHeight; y += heightIncrement)
        {
            for (int x = 0; x < meshWidth; x += widthIncrement)
            {
                vertices[vertexIndex] = (new Vector3(x, sampler.SampleMap(x - (meshWidth - 1)/2, y - (meshHeight - 1)/2, sampler.samplingCurve), y) 
                                                + centerOffset) * meshScale;
                                                
                vertexIndex++;
            }
        }

        return vertices;
    }
    

    //Deprecated
    public static MeshData GenerateTerrainFromMap(float[,] heightMap, float meshScale)
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
