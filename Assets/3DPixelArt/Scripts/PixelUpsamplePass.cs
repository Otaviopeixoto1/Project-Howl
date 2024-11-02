using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEditor;


public class PixelUpsamplePass : ScriptableRenderPass
{
    private ProfilingSampler m_ProfilingSampler;
    
    Material m_Material;
    Mesh quadMesh;
    Matrix4x4 quadTransform;

    RenderTexture currentFrame;
    RTHandle colorTarget;

    //RenderTexture upscaledTex;

    static int 
        currentTexId = Shader.PropertyToID("_MainTex"),
        prevTexId = Shader.PropertyToID("_PrevTex"),
        upscaledTexId = Shader.PropertyToID("_UpscaledTex"); 

    private Mesh CreateQuad()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0)
        };
        mesh.vertices = vertices;

        int[] tris = new int[6]
        {
            // lower left triangle
            0, 2, 1,
            // upper right triangle
            2, 3, 1
        };
        mesh.triangles = tris;

        Vector3[] normals = new Vector3[4]
        {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
        };
        mesh.normals = normals;

        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        mesh.uv = uv;

        return mesh;
    }

    public PixelUpsamplePass(Material material, Matrix4x4 transform, RenderTextureDescriptor desc)
    {
        m_Material = material;
        quadMesh = CreateQuad();
        quadTransform = transform;
        m_ProfilingSampler = new ProfilingSampler("Upsample Pass");
    } 

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        base.OnCameraSetup(cmd, ref renderingData);

        //RESIZE THE PREV FRAME TEX. USE A BIG TEX INSTEAD !!!! DONT DOWNSCALE IT AGAIN
        //previousFrame.Release();
        //previousFrame.width = renderingData.cameraData.cameraTargetDescriptor.width;
        //previousFrame.height = renderingData.cameraData.cameraTargetDescriptor.height; 
        //previousFrame.Create();
        //RenderingUtils.ReAllocateIfNeeded(ref tempPrevFrame, desc, name: "_TempPreviousFrameTexture");
        //ConfigureTarget(colorTarget);
    }

    public void Setup(RTHandle colorTarget, RenderTexture currentFrameTex)
    {
        currentFrame = currentFrameTex;
        this.colorTarget = colorTarget; 
    }

    //clean-up allocated Resources
    public void Dispose() 
    {
        #if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                Object.Destroy(m_Material);
                Object.Destroy(quadMesh);
            }
            else
            {
                Object.DestroyImmediate(m_Material);
                Object.DestroyImmediate(quadMesh);
            }
        #else
            Object.Destroy(m_Material);
            Object.Destroy(quadMesh);
        #endif
    }



    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        
        if (m_Material == null) return;
        
        
        CommandBuffer cmd = CommandBufferPool.Get(name: "Upsample Pass");

        //m_Material.SetTexture(upscaledTexId, upscaledTex);
        if (currentFrame != null)
            m_Material.SetTexture(currentTexId, currentFrame);
        
    
        using (new ProfilingScope(cmd, m_ProfilingSampler)) 
        {
            cmd.DrawMesh(quadMesh, quadTransform, m_Material);
        }


        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }
    
}
