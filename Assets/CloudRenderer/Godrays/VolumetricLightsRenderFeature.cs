using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;



[Serializable]
public class GodraySettings
{
    public Shader shader;

    #if UNITY_EDITOR 
    [MinMax(0f,1f)]
    #endif
    public Vector2 sampleRange = new Vector2(0.5f,1.0f);

    [Range(1,50)] public int sampleCount = 10;

    [Range(0,10)] public float intensity = 0.2f;
    [Range(0, 10)] public float fadeStrength = 1.0f;
}


public class VolumetricLightsRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent _event = RenderPassEvent.AfterRenderingTransparents;
    public GodraySettings Settings;
    private Material m_Material; // temporary material used to blit to the screen
    private GodrayPass m_GodrayPass; //custom render pass


    public override void Create()
    {
        if (Settings.shader != null)
        {
            m_Material = new Material(Settings.shader);
        }

        m_GodrayPass = new GodrayPass(m_Material, Settings);
        m_GodrayPass.renderPassEvent = _event + 1;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        CameraData cameraData = renderingData.cameraData;
        if (cameraData.cameraType != CameraType.Game) return;

        
        renderer.EnqueuePass(m_GodrayPass);
    }
    
    protected override void Dispose(bool disposing)
    {
        m_GodrayPass.Dispose();

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
