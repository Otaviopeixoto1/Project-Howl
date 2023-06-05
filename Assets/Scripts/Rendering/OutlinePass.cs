using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;
public class OutlinePass : ScriptableRenderPass
{
    Material m_Material;
    //readonly Color m_Color;
    readonly float m_DepthLowerThreshold;
    readonly float m_DepthUpperThreshold;
    readonly float m_NormalLowerThreshold;
    readonly float m_NormalUpperThreshold;

    RenderTargetIdentifier m_CameraColorTarget; //(color buffer) output target
    static int colorID = Shader.PropertyToID("_OutlineColor");
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
            
        //renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing; //add post processing on outlines
        renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing; //no post processing on outlines
    }
    public void SetTarget(RenderTargetIdentifier colorHandle)
    {
        m_CameraColorTarget = colorHandle;
    }
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        base.OnCameraSetup(cmd, ref renderingData);

        //Configuring the target doesnt work !
        //ConfigureTarget(new RenderTargetIdentifier(m_CameraColorTarget, 0, CubemapFace.Unknown, -1));
    }




    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        
        if (camera.cameraType != CameraType.Game) return;
        if (m_Material == null) return;
        
        CommandBuffer cb = CommandBufferPool.Get(name: "OutlinePass");
        cb.BeginSample("Outline Pass");

        //m_Material.SetColor(colorID, m_Color);
        m_Material.SetFloat(dLowerID,m_DepthLowerThreshold);
        m_Material.SetFloat(dUpperID,m_DepthUpperThreshold);
        m_Material.SetFloat(nLowerID,m_NormalLowerThreshold);
        m_Material.SetFloat(nUpperID,m_NormalUpperThreshold);

        
        //setting the target like this doesnt work !
        //cb.SetRenderTarget(new RenderTargetIdentifier(m_CameraColorTarget, 0, CubemapFace.Unknown, -1));
        
        cb.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material);

        cb.EndSample("Outline Pass");
        context.ExecuteCommandBuffer(cb);
        //cb.Clear();
        CommandBufferPool.Release(cb);
    }
    
}
