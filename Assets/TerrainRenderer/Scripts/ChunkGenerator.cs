using UnityEngine;


public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;
    public Vector2[] atlasUvs;


    private int currentTriangleIndex;
    public MeshData(int width, int height)
    {
        vertices = new Vector3[width * height];
        uvs = new Vector2[width * height];
        atlasUvs = new Vector2[width * height];
        triangles = new int[(width - 1) * (height - 1) * 6];
    }

    public void AddTriangle(int a, int b, int c)
    {
        triangles[currentTriangleIndex] = a;
        triangles[currentTriangleIndex + 1] = b;
        triangles[currentTriangleIndex + 2] = c;
        currentTriangleIndex += 3;    
    }   

    public Mesh CreateMesh(bool skipNormals = false)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.SetUVs(0, uvs);

        if (atlasUvs[0] != null)
        {
            mesh.SetUVs(3,atlasUvs);
        }
        
        if (!skipNormals)
        {
            mesh.RecalculateNormals();
        }

        return mesh;
    }
    
    public static Mesh CreateQuad(float width = 1f, float height = 1f) {
        
        var mesh = new Mesh();
        float w = width * .5f;
        float h = height * .5f;
        var vertices = new Vector3[4] {
            new Vector3(-w, 0, 0),
            new Vector3(w, 0, 0),
            new Vector3(-w, 2*h, 0),
            new Vector3(w, 2*h, 0)
        };

        var tris = new int[6] {
            // lower left triangle
            0, 2, 1,
            // lower right triangle
            2, 3, 1
        };

        var normals = new Vector3[4] {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
        };

        var uv = new Vector2[4] {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1),
        };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.normals = normals;
        mesh.uv = uv;

        return mesh;
    }
}


//a struct used as a wrapper for all the data returned in the ChunkGenerator
public struct ChunkData
{
    public readonly MeshData meshData;
    public readonly MeshData colliderData;
    public readonly ChunkDataTree dataTree;
    public readonly Biomes[] biomeMap;
    public ChunkData(MeshData meshData, MeshData colliderData, ChunkDataTree dataTree, Biomes[] biomeMap)
    {
        this.meshData = meshData;
        this.colliderData = colliderData;
        this.dataTree = dataTree;
        this.biomeMap = biomeMap;
    }

}





public static class ChunkGenerator
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
    public static MeshData GenerateRectMesh(MapGenerator sampler, int meshWidth, int meshHeight, float meshScale, int lodBias = 0,bool isThread = false)
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


    //Using WorldGenerator as a sampler
    public static MeshData GenerateQuadMesh(WorldGenerator sampler, int meshSize, float meshScale, Vector2 sampleOffset, int lodBias = 0)
    {
        int increment = CalculateLodIncrement(meshSize,lodBias);

        int mapSize = sampler.biomeMapSize;
        float mapChunks = 1/sampler.biomeMapScale;
        int vertices = ((meshSize - 1)/increment) + 1;

        MeshData meshData = new MeshData(vertices, vertices);

        Vector3 centerOffset = new Vector3(-(meshSize - 1)/2f, 0, -(meshSize - 1)/2f);

        int vertexIndex = 0;

        for (int y = 0; y < meshSize; y += increment)
        {
            for (int x = 0; x < meshSize; x += increment)
            {
                meshData.vertices[vertexIndex] = (new Vector3(x, sampler.GetHeight(x + sampleOffset.x, y + sampleOffset.y), y) 
                                                + centerOffset) * meshScale;
                meshData.uvs[vertexIndex] = new Vector2(x, y)/(float)meshSize;
                meshData.atlasUvs[vertexIndex] = (meshData.uvs[vertexIndex] + sampleOffset/(float)mapSize - Vector2.one * 0.5f)/(mapChunks);

                if (x < (meshSize - 1) && y < (meshSize - 1))
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + vertices, vertexIndex + vertices + 1 );
                    meshData.AddTriangle(vertexIndex, vertexIndex + vertices + 1, vertexIndex + 1 );
                }

                vertexIndex++;
            }
        }

        return meshData;
    }

    public static ChunkData GenerateTerrainChunk(WorldGenerator worldGenerator, int chunkSize, float meshScale, Vector2Int sampleOffset, int meshLodBias = 0,int colliderLodBias = 0)
    {
        ChunkDataTree dataTree = new ChunkDataTree(worldGenerator.subChunkLevel);
        
        int subChunkCount = MathMisc.TwoPowX(worldGenerator.subChunkLevel);
        int subChunkLength = chunkSize/subChunkCount;

        Biomes[] biomeMap = new Biomes[(subChunkCount + 1) * (subChunkCount + 1)];

        int startY = sampleOffset.y - chunkSize/2;
        int startX = sampleOffset.x - chunkSize/2;

        //Sample the Biomes on each subchunk edge:
        int i = 0;
        for (int y = startY; y <= startY + chunkSize; y += subChunkLength)
        {
            for (int x = startX; x <= startX + chunkSize; x += subChunkLength)
            {
                biomeMap[i] = worldGenerator.GetBiome(x,y);
                i++;
            }
        }
    
        MeshData terrainMesh = GenerateQuadMesh(worldGenerator,chunkSize + 1,meshScale,sampleOffset,meshLodBias);
        MeshData colliderMesh = GenerateQuadMesh(worldGenerator,chunkSize + 1,meshScale,sampleOffset,colliderLodBias);

        return new ChunkData(terrainMesh, colliderMesh, dataTree, biomeMap);
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
