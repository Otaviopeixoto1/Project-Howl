using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;



[Serializable]
public class OutlineSettings
{
    public Shader OutlineShader;
    //public Color OutlineColor;

    [Range(0f, 0.1f)]
    public float DepthLowerThreshold = 0.04f;

    [Range(0f, 0.1f)]
    public float DepthUpperThreshold = 0.05f;

    [Range(0f, 0.1f)]
    public float NormalLowerThreshold = 0.05f;

    [Range(0f, 0.1f)]
    public float NormalUpperThreshold = 0.1f;





}


public class OutlineRendererFeature : ScriptableRendererFeature
{
    public OutlineSettings Settings;
    Material m_Material; // temporary material used to blit to the screen


    OutlinePass m_OutlinePass; //custom render pass

    public override void Create()
    {
        if (Settings.OutlineShader != null)
        {
            m_Material = new Material(Settings.OutlineShader);
        }

        m_OutlinePass = new OutlinePass(m_Material, Settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game) return;

        //generates the opaque texture used by the shader
        m_OutlinePass.ConfigureInput(ScriptableRenderPassInput.Color); //Remove
        //m_OutlinePass.SetTarget(renderer.cameraColorTarget);
        renderer.EnqueuePass(m_OutlinePass);
    }
    
    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);
    }
}
