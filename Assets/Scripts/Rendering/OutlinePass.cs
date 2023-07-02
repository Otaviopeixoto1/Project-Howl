using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;
public class OutlinePass : ScriptableRenderPass
{
    private RTHandle rtTemp;
    private ProfilingSampler m_ProfilingSampler;

    Material m_Material;
    //readonly Color m_Color;
    readonly float m_DepthLowerThreshold;
    readonly float m_DepthUpperThreshold;
    readonly float m_NormalLowerThreshold;
    readonly float m_NormalUpperThreshold;

    RTHandle m_CameraColorTarget; //(color buffer) output target
    static int dLowerID = Shader.PropertyToID("_DLower");
    static int dUpperID = Shader.PropertyToID("_DUpper");
    static int nLowerID = Shader.PropertyToID("_NLower");
    static int nUpperID = Shader.PropertyToID("_NUpper");



    public OutlinePass(Material material, OutlineSettings settings)
    {
        m_Material = material;
        //m_Color = settings.OutlineColor;
        m_DepthLowerThreshold = settings.DepthLowerThreshold;
        m_DepthUpperThreshold = settings.DepthUpperThreshold;
        m_NormalLowerThreshold = settings.NormalLowerThreshold;
        m_NormalUpperThreshold = settings.NormalUpperThreshold;

        m_ProfilingSampler = new ProfilingSampler("Outline Pass");
            
        //renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing; //add post processing on outlines
        renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing; //no post processing on outlines
    }
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        base.OnCameraSetup(cmd, ref renderingData);

        var colorDesc = renderingData.cameraData.cameraTargetDescriptor;
        colorDesc.depthBufferBits = 0; // must set to 0 to specify a colour target
        // to use a different format, set .colorFormat or .graphicsFormat

        //setup the temporary render target used for blitting
        RenderingUtils.ReAllocateIfNeeded(ref rtTemp, colorDesc, name: "_TemporaryColorTexture");


        //ConfigureTarget(new RenderTargetIdentifier(m_CameraColorTarget, 0, CubemapFace.Unknown, -1));
    }

    //clean-up allocated RTHandle
    public void ReleaseTargets() {
        rtTemp?.Release();
    }




    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        
        if (camera.cameraType != CameraType.Game) return;
        if (m_Material == null) return;
        
        CommandBuffer cb = CommandBufferPool.Get(name: "OutlinePass");
        m_Material.SetFloat(dLowerID,m_DepthLowerThreshold);
        m_Material.SetFloat(dUpperID,m_DepthUpperThreshold);
        m_Material.SetFloat(nLowerID,m_NormalLowerThreshold);
        m_Material.SetFloat(nUpperID,m_NormalUpperThreshold);




        using (new ProfilingScope(cb, m_ProfilingSampler)) 
        {
            context.ExecuteCommandBuffer(cb);
            cb.Clear();
            /*
            Note : should always ExecuteCommandBuffer at least once before using
            ScriptableRenderContext functions (e.g. DrawRenderers) even if you 
            don't queue any commands! This makes sure the frame debugger displays 
            everything under the correct title.
            */

            //DEPRECATED:
            //cb.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material);
            
            RTHandle rtCamera = renderingData.cameraData.renderer.cameraColorTargetHandle;
            //
            Blitter.BlitCameraTexture(cb, rtCamera, rtTemp, m_Material, 0); //something wront with the material
            Blitter.BlitCameraTexture(cb, rtTemp, rtCamera, Vector2.one);
        }



        context.ExecuteCommandBuffer(cb);
        cb.Clear();
        CommandBufferPool.Release(cb);
    }
    
}
