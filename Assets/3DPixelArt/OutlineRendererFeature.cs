using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;



[Serializable]
public class OutlineSettings
{
    public Shader OutlineShader;

    #if UNITY_EDITOR
    [MinMax(0,0.1f)]
    #endif
    public Vector2 DepthRange;
    
    #if UNITY_EDITOR
    [MinMax(0,0.1f)]
    #endif
    public Vector2 NormalRange;
}


public class OutlineRendererFeature : ScriptableRendererFeature
{
    public RenderPassEvent _event = RenderPassEvent.AfterRenderingSkybox;
    public OutlineSettings Settings;
    private Material m_Material; // temporary material used to blit to the screen
    private OutlinePass m_OutlinePass; //custom render pass

    private bool isGameCamera = false;

    public override void Create()
    {
        if (Settings.OutlineShader != null)
        {
            m_Material = new Material(Settings.OutlineShader);
        }

        m_OutlinePass = new OutlinePass(m_Material, Settings);
        m_OutlinePass.renderPassEvent = _event;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        CameraData cameraData = renderingData.cameraData;
        if (cameraData.cameraType != CameraType.Game) return;
        isGameCamera = true;
        
        //generates the normal texture used by the shader.    color texture: ScriptableRenderPassInput.Color
        m_OutlinePass.ConfigureInput(ScriptableRenderPassInput.Normal ); 
        renderer.EnqueuePass(m_OutlinePass);
    }
    
    protected override void Dispose(bool disposing)
    {
        m_OutlinePass.Dispose();

        #if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                Destroy(m_Material);
            }
            else
            {
                DestroyImmediate(m_Material);
            }
        #else
            Destroy(material);
        #endif
    }
}
