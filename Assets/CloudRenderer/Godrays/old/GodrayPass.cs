using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEditor;




public class GodrayPass : ScriptableRenderPass
{
    private RTHandle rtTempAccumulation0;
    private RTHandle rtTempAccumulation1;
    private ProfilingSampler m_ProfilingSampler;
    
    private Material m_GodrayMaterial;
    private Material m_BlurMaterial;
    private GodraySettings settings;

    const string 
            k_Samples10 = "SAMPLES_10",
            k_Samples30 = "SAMPLES_30",
            k_Samples50 = "SAMPLES_50";

    string prevSampleMode = k_Samples10;

    static int 
        fadeStrengthId = Shader.PropertyToID("fadeStrength"),
        opacityId = Shader.PropertyToID("opacityMultiplier"),
        intensityId = Shader.PropertyToID("intensity"),
        planeOffsetId = Shader.PropertyToID("planeOffset"),
        planeCenterId = Shader.PropertyToID("planeCenter"),
        planeSeparationId = Shader.PropertyToID("planeSeparation"),
        cameraDirId = Shader.PropertyToID("cameraDir"),

        exposureId = Shader.PropertyToID("exposure"),
        ditherTexId = Shader.PropertyToID("ditherTex"),
        sceneColorTexId = Shader.PropertyToID("sceneColorTex"),

        // TEST
        sceneDepthId = Shader.PropertyToID("sceneDepth"),
        //

        inverseVPMatrixId = Shader.PropertyToID("inverseVPMatrix"); 





    public GodrayPass(Material godrayMaterial, Material blurMaterial, GodraySettings settings)
    {
        m_GodrayMaterial = godrayMaterial;
        m_BlurMaterial = blurMaterial;
        this.settings = settings;


        //Set the _mainLightShadowmapTexture with the new name: "shadowMap"
        //settings.volumeMarchShader.SetTextureFromGlobal(0, "shadowMap", "_MainLightShadowmapTexture");

        m_ProfilingSampler = new ProfilingSampler("Godray Pass");
    } 
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        base.OnCameraSetup(cmd, ref renderingData);

        var colorDesc = renderingData.cameraData.cameraTargetDescriptor;
        //colorDesc.colorFormat = RenderTextureFormat.ARGBHalf;
        colorDesc.depthBufferBits = 0; // must set to 0 to specify a color only target
        // to use a different format, set .colorFormat or .graphicsFormat
        
        //setup the temporary render target used for blitting 
        RenderingUtils.ReAllocateIfNeeded(ref rtTempAccumulation0, colorDesc, filterMode: FilterMode.Bilinear, name: "_TemporaryTexture_GodrayAccumulation0");
        RenderingUtils.ReAllocateIfNeeded(ref rtTempAccumulation1, colorDesc, filterMode: FilterMode.Bilinear, name: "_TemporaryTexture_GodrayAccumulation1");
        //ConfigureTarget() can be used to set the render target, but by default the target will be set as the current camera target

    }

    public override void OnCameraCleanup(CommandBuffer cmd) 
    {

    }

    //clean-up allocated RTHandle
    public void Dispose() 
    {
        #if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                Object.Destroy(m_GodrayMaterial);
                Object.Destroy(m_BlurMaterial);
            }
            else
            {
                Object.DestroyImmediate(m_GodrayMaterial);
                Object.DestroyImmediate(m_BlurMaterial);
            }
        #else
            Object.Destroy(m_GodrayMaterial);
            Object.Destroy(m_BlurMaterial);
        #endif

        rtTempAccumulation0?.Release();
        rtTempAccumulation1?.Release();
    }
    
    void UpdateParameters(Camera camera)
    {
        var volumeComponent = VolumeManager.instance.stack.GetComponent<GodrayVolumeComponent>();

        float start = volumeComponent.start.overrideState ? volumeComponent.start.value : settings.sampleRange.x;
        float end = volumeComponent.end.overrideState ? volumeComponent.end.value : settings.sampleRange.y;
        float intensity = volumeComponent.intensity.overrideState ? volumeComponent.intensity.value : settings.intensity;
        float fadeStrength = volumeComponent.fadeStrength.overrideState ? volumeComponent.fadeStrength.value : settings.fadeStrength;
        float opacity = volumeComponent.fadeStrength.overrideState ? volumeComponent.opacity.value : settings.opacity;
        float exposure = volumeComponent.exposure.overrideState ? volumeComponent.exposure.value : settings.exposure;
        VolumetricLightSamples sampleCount = volumeComponent.samples.overrideState ? volumeComponent.samples.value : settings.samplesCount;

        m_GodrayMaterial.SetTexture(ditherTexId, settings.ditherPattern);
        m_GodrayMaterial.SetFloat(opacityId, opacity);
        m_GodrayMaterial.SetFloat(exposureId, exposure);

        string sampleMode = "";
        switch (sampleCount)
        {
            case VolumetricLightSamples.Samples_50:
                sampleMode = k_Samples50;
                break;
            case VolumetricLightSamples.Samples_30:
                sampleMode = k_Samples30;
                break;
            case VolumetricLightSamples.Samples_10:
                sampleMode = k_Samples10;
                break;
        }

        if (sampleMode != prevSampleMode)
            m_GodrayMaterial.DisableKeyword(prevSampleMode);

        m_GodrayMaterial.EnableKeyword(sampleMode);
        prevSampleMode = sampleMode;


        m_GodrayMaterial.SetFloat(fadeStrengthId, fadeStrength);
        m_GodrayMaterial.SetFloat(intensityId, intensity);


        Matrix4x4 VPMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix;
        m_GodrayMaterial.SetMatrix(inverseVPMatrixId, VPMatrix.inverse); 
        
        m_GodrayMaterial.SetVector(cameraDirId, camera.transform.forward);

        float planeDistance = end * (camera.farClipPlane - camera.nearClipPlane) + camera.nearClipPlane;
        Vector3 planeCenter = camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, planeDistance));
        float planeSeparation = (end - start) * (camera.farClipPlane - camera.nearClipPlane)/(float)sampleCount;

        m_GodrayMaterial.SetFloat(planeOffsetId, end); 
        m_GodrayMaterial.SetFloat(planeSeparationId, planeSeparation); 
        m_GodrayMaterial.SetVector(planeCenterId, planeCenter);
    }


    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;

        if (camera.cameraType != CameraType.Game) return;
        if (m_GodrayMaterial == null) return;
        
        CommandBuffer cmd = CommandBufferPool.Get(name: "Godray Pass");

        UpdateParameters(camera);

        using (new ProfilingScope(cmd, m_ProfilingSampler)) 
        {
            //context.ExecuteCommandBuffer(cmd);
            //cmd.Clear();
            
            RTHandle rtCamera = renderingData.cameraData.renderer.cameraColorTargetHandle;
            
            //DEPTH **BUFFER** CAN BE ACQUIRED HERE FROM THIS HANDLE:
            RTHandle rtDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle;
            m_GodrayMaterial.SetTexture(sceneDepthId, rtDepth);
            
            

            //JUST DRAW A QUAD ? use rtTemp0 as render target
            Blitter.BlitCameraTexture(cmd, rtCamera, rtTempAccumulation0, m_GodrayMaterial, 0); 

            /**/

            Blitter.BlitCameraTexture(cmd, rtTempAccumulation0, rtTempAccumulation1, m_BlurMaterial, 0); 

            //Order doesnt matter here, SetTexture can be called at the beginning or end of the pass and result should be the same ...
            m_BlurMaterial.SetTexture(sceneColorTexId, rtCamera);
            Blitter.BlitCameraTexture(cmd, rtTempAccumulation1, rtTempAccumulation0, m_BlurMaterial, 1); 

            //Blit to the camera output
            Blitter.BlitCameraTexture(cmd, rtTempAccumulation0, rtCamera, Vector2.one);
        }



        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }
    
}
