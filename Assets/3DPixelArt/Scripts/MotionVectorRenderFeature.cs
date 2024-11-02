using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;



[Serializable]
public class MotionRenderSetting
{
    public LayerMask cullingMask;
}


public class MotionVectorRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent _event = RenderPassEvent.AfterRenderingSkybox;
    public MotionRenderSetting settings;
    //private Material m_Material; 
    private MotionVectorPass m_MotionVecPass; //custom render pass

    public override void Create()
    {
        m_MotionVecPass = new MotionVectorPass();
        m_MotionVecPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        CameraData cameraData = renderingData.cameraData;
        if (cameraData.cameraType != CameraType.Game) { return; }

        m_MotionVecPass.ConfigureInput(ScriptableRenderPassInput.Motion);
        renderer.EnqueuePass(m_MotionVecPass);
    }
    
    protected override void Dispose(bool disposing)
    {
        //m_MotionVecPass.Dispose();

        /*
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
        #endif*/
    }
}