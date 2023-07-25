using System.Collections;
using System.Collections.Generic;
using UnityEngine;


////////////////////////////////////////////////////////////////////////////////////////////////////////
//the detail chunk will sample the biome texture to identify what can be drawn and where
//for better performance and logistic, a single atlas texture can be used for all details !!
//this way we dont have to use different materials, every material samples the same texture 
////////////////////////////////////////////////////////////////////////////////////////////////////////


public class DetailChunk 
{
    //ADD SUPPORT FOR MULTIPLE DETAIL TYPES !
    //ADD A CLASS FOR EACH DETAIL THAT GENERATES THE CORRESPONDING BUFFERS FOR THEM

    public Material material;

    //Each chunk must have a different property block so that they dont share the same meshPropertiesBuffer
    public MaterialPropertyBlock propertyBlock;
    private ComputeBuffer meshPropertiesBuffer;
    private ComputeBuffer argsBuffer;
    private Mesh mesh;
    private Bounds bounds;

    private SubChunk subChunk;


    // Mesh Properties struct to be read from the GPU.
    private struct DetailMeshProperties 
    {
        public Matrix4x4 mat;
        public Vector4 color;


        // Size() is a convenience funciton which returns the stride of the struct.
        public static int Size() {
            return
                sizeof(float) * 4 * 4 + // matrix;
                sizeof(float) * 4;      // color;
        }
    }


    //Use a terrainDetailsSettings to initialize this
    public DetailChunk(Material material, SubChunk subChunk)
    {
        this.material = material;
        this.mesh = CreateQuad();
        this.subChunk = subChunk;
        this.bounds = subChunk.GetBounds();
        this.propertyBlock = new MaterialPropertyBlock();
        if (subChunk.IsReady())
        {
            InitializeBuffers(subChunk.GetVertices());
        }
        

    }






    private void InitializeBuffers(List<Vector3> positions) {
        
        int population = positions.Count;

        // Argument buffer used by DrawMeshInstancedIndirect.
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = (uint)mesh.GetIndexCount(0);
        args[1] = (uint)population;
        args[2] = (uint)mesh.GetIndexStart(0);
        args[3] = (uint)mesh.GetBaseVertex(0);

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        // Initialize buffer with the given population.
        DetailMeshProperties[] properties = new DetailMeshProperties[population];

        for (int i = 0; i < population; i++) {
            DetailMeshProperties props = new DetailMeshProperties();
            Vector3 position = positions[i];
            Quaternion rotation = Quaternion.Euler(Random.Range(-180, 180), Random.Range(-180, 180), Random.Range(-180, 180));
            Vector3 scale = Vector3.one;

            props.mat = Matrix4x4.TRS(position, rotation, scale);
            props.color = Color.Lerp(Color.red, Color.blue, Random.value);

            properties[i] = props;
        }

        meshPropertiesBuffer = new ComputeBuffer(population, DetailMeshProperties.Size());
        meshPropertiesBuffer.SetData(properties);
        propertyBlock.SetBuffer("_Properties", meshPropertiesBuffer);
        //material.SetBuffer("_Properties", meshPropertiesBuffer);
    }

    // Create a quad mesh
    private Mesh CreateQuad(float width = 1f, float height = 1f) {
        
        var mesh = new Mesh();
        float w = width * .5f;
        float h = height * .5f;
        var vertices = new Vector3[4] {
            new Vector3(-w, -h, 0),
            new Vector3(w, -h, 0),
            new Vector3(-w, h, 0),
            new Vector3(w, h, 0)
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


    

    public void Draw()
    {   
        if (meshPropertiesBuffer != null && argsBuffer != null)
        {
            Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, argsBuffer, properties:propertyBlock);
        }
        else if (subChunk.IsReady())
        {
            InitializeBuffers(subChunk.GetVertices());
        }
        
        
    }


    public void Clear() 
    {
        if (meshPropertiesBuffer != null) {
            meshPropertiesBuffer.Release();
        }
        meshPropertiesBuffer = null;

        if (argsBuffer != null) {
            argsBuffer.Release();
        }
        argsBuffer = null;
    }

}
