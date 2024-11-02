using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;



[Serializable]
public class UpsampleSettings
{
    public Shader upsampleShader;
    //public Shader pixelAdvectionShader;
    public RenderTexture sceneRenderTexture;
    //public RenderTexture upscaledTexture;
}


public class CanvasRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent _event = RenderPassEvent.AfterRenderingSkybox;
    public UpsampleSettings settings;

    private Material m_UpsampleMaterial; // temporary material used to blit to the screen
    private PixelUpsamplePass m_UpsamplePass; //custom render pass

    //private Material m_AdvectionMaterial; // temporary material used to blit to the screen
    //private PixelAdvectionPass m_AdvectionPass; //custom render pass

    private RTHandle tempColorTarget;

    public override void Create()
    {
        //
        // Upsample
        //

        if (settings.upsampleShader != null) 
        {
            m_UpsampleMaterial = new Material(settings.upsampleShader);
        }

        RenderTextureDescriptor desc = new RenderTextureDescriptor();
        Vector3 scale = Vector3.one;
        if (settings.sceneRenderTexture != null)
        {
            desc = settings.sceneRenderTexture.descriptor;
            scale.x = desc.width;
            scale.y = desc.height;
        }
        //take transform and scale into parameters !!!!
        Matrix4x4 transformMat = Matrix4x4.TRS(new Vector3(323.5f, 182, 100), Quaternion.identity, scale);
        m_UpsamplePass = new PixelUpsamplePass(m_UpsampleMaterial, transformMat, desc);
        m_UpsamplePass.renderPassEvent = _event;

        //
        // Advect
        //
        /*
        if (settings.pixelAdvectionShader != null) 
        {
            m_AdvectionMaterial = new Material(settings.pixelAdvectionShader);
        }
        m_AdvectionPass = new PixelAdvectionPass(m_AdvectionMaterial);
        m_AdvectionPass.renderPassEvent = _event;*/

    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData) 
    {
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;

        RenderingUtils.ReAllocateIfNeeded(ref tempColorTarget, desc, name: "_upsampledTarget");
        m_UpsamplePass.Setup(tempColorTarget, settings.sceneRenderTexture);
        //m_AdvectionPass.Setup(tempColorTarget, desc);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        CameraData cameraData = renderingData.cameraData;
        if (cameraData.cameraType != CameraType.Game && cameraData.cameraType != CameraType.SceneView) { return; }

        renderer.EnqueuePass(m_UpsamplePass);
        //renderer.EnqueuePass(m_AdvectionPass);
    }
    
    protected override void Dispose(bool disposing)
    {
        m_UpsamplePass.Dispose();
        //m_AdvectionPass.Dispose();

        tempColorTarget?.Release();

        #if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                Destroy(m_UpsampleMaterial);
                //Destroy(m_AdvectionMaterial);
            }
            else
            {
                DestroyImmediate(m_UpsampleMaterial);
                //DestroyImmediate(m_AdvectionMaterial);
            }
        #else
            Destroy(m_UpsampleMaterial);
            //Destroy(m_AdvectionMaterial);
        #endif
    }
}