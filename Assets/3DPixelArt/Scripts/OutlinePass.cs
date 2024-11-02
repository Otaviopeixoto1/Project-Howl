using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEditor;

public class OutlinePass : ScriptableRenderPass
{
    private RTHandle rtTemp;
    private ProfilingSampler m_ProfilingSampler;
    
    Material m_Material;

    OutlineSettings settings;

    //RTHandle m_CameraColorTarget; //(color buffer) output target
    static int 
        strMultiplierId = Shader.PropertyToID("strengthMultiplier"),
        dLowerID = Shader.PropertyToID("_DLower"),
        dUpperID = Shader.PropertyToID("_DUpper"),
        nLowerID = Shader.PropertyToID("_NLower"),
        nUpperID = Shader.PropertyToID("_NUpper"),
        viewDirID = Shader.PropertyToID("viewDirection"),
        viewUpDirID = Shader.PropertyToID("viewUpDirection"),
        viewRightDirID = Shader.PropertyToID("viewRightDirection");



    public OutlinePass(Material material, OutlineSettings settings)
    {
        m_Material = material;
        this.settings = settings;

        m_ProfilingSampler = new ProfilingSampler("Outline Pass");
    } 
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        base.OnCameraSetup(cmd, ref renderingData);

        var colorDesc = renderingData.cameraData.cameraTargetDescriptor;
        colorDesc.depthBufferBits = 0; // must set to 0 to specify a color only target
        // to use a different format, set .colorFormat or .graphicsFormat

        //setup the temporary render target used for blitting
        RenderingUtils.ReAllocateIfNeeded(ref rtTemp, colorDesc, name: "_TemporaryColorTexture");
    }

    public override void OnCameraCleanup(CommandBuffer cmd) 
    {

    }

    //clean-up allocated RTHandle
    public void Dispose() {
        #if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                Object.Destroy(m_Material);
            }
            else
            {
                Object.DestroyImmediate(m_Material);
            }
        #else
                    Object.Destroy(m_Material);
        #endif

        //Debug.Log("releasing");
        rtTemp?.Release();
    }

    private void UpdateParameters(Camera camera)
    {
        m_Material.SetFloat(strMultiplierId, settings.strengthMultiplier);
        m_Material.SetFloat(dLowerID, settings.DepthRange.x);
        m_Material.SetFloat(dUpperID, settings.DepthRange.y);
        m_Material.SetFloat(nLowerID, settings.NormalRange.x);
        m_Material.SetFloat(nUpperID, settings.NormalRange.y);

        m_Material.SetVector(viewDirID, camera.transform.forward);
        m_Material.SetVector(viewUpDirID, camera.transform.up);
        m_Material.SetVector(viewRightDirID, camera.transform.right);
        
    }


    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        
        if (camera.cameraType != CameraType.Game) return;
        if (m_Material == null) return;
        
        CommandBuffer cb = CommandBufferPool.Get(name: "OutlinePass");
        UpdateParameters(camera);




        using (new ProfilingScope(cb, m_ProfilingSampler)) 
        {
            //context.ExecuteCommandBuffer(cb);
            //cb.Clear();
            /*
            Note : always ExecuteCommandBuffer at least once before using
            ScriptableRenderContext functions (e.g. DrawRenderers) even if you 
            don't queue any commands! This makes sure the frame debugger displays 
            everything under the correct title.
            */

            //DEPRECATED:
            //cb.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material);
            
            RTHandle rtCamera = renderingData.cameraData.renderer.cameraColorTargetHandle;
            //
            Blitter.BlitCameraTexture(cb, rtCamera, rtTemp, m_Material, 0); 
            Blitter.BlitCameraTexture(cb, rtTemp, rtCamera, Vector2.one);
        }



        context.ExecuteCommandBuffer(cb);
        cb.Clear();
        CommandBufferPool.Release(cb);
    }
    
}
