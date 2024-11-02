using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;



public enum VolumetricLightSamples
{
    Samples_10 = 10,
    Samples_30 = 30,
    Samples_50 = 50
}

[Serializable]
public class GodraySettings
{
    public Shader godrayShader;

    public Texture2D ditherPattern;
    public VolumetricLightSamples samplesCount = VolumetricLightSamples.Samples_10;

    #if UNITY_EDITOR 
    [MinMax(0f,1f)]
    #endif
    public Vector2 sampleRange = new Vector2(0.5f,1.0f);


    [Range(0,10)] public float intensity = 0.2f;
    [Range(0,1.0f)] public float opacity = 0.2f;
    [Range(0, 10)] public float fadeStrength = 1.0f;
}


public class VolumetricLightsRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent _event = RenderPassEvent.AfterRenderingTransparents;
    public GodraySettings Settings;
    private Material m_Material; // temp material used to blit to the screen

    private GodrayPass m_GodrayPass; 


    public override void Create()
    {
        if (Settings.godrayShader != null)
        {
            m_Material = new Material(Settings.godrayShader); 
        }

        //Settings.volumeMarchShader.SetTextureFromGlobal("_MainLightShadowmapTexture");
        
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
            Destroy(m_Material);
        #endif
    }
}