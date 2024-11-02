using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEditor;




public class GodrayPass : ScriptableRenderPass
{
    private RTHandle rtTempAccumulation;
    private ProfilingSampler m_ProfilingSampler;
    
    private Material m_Material;
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

        ditherTexId = Shader.PropertyToID("ditherTex"),

        // TEST
        sceneDepthId = Shader.PropertyToID("sceneDepth"),
        //

        inverseVPMatrixId = Shader.PropertyToID("inverseVPMatrix"); 





    public GodrayPass(Material material, GodraySettings settings)
    {
        m_Material = material;
        this.settings = settings;

        //Set the _mainLightShadowmapTexture with the new name: "shadowMap"
        //settings.volumeMarchShader.SetTextureFromGlobal(0, "shadowMap", "_MainLightShadowmapTexture");

        m_ProfilingSampler = new ProfilingSampler("Godray Pass");
    } 
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        base.OnCameraSetup(cmd, ref renderingData);

        var colorDesc = renderingData.cameraData.cameraTargetDescriptor;
        colorDesc.depthBufferBits = 0; // must set to 0 to specify a color only target
        // to use a different format, set .colorFormat or .graphicsFormat
        
        //setup the temporary render target used for blitting 
        RenderingUtils.ReAllocateIfNeeded(ref rtTempAccumulation, colorDesc, name: "_TemporaryTexture_GodrayAccumulation");
        
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
                Object.Destroy(m_Material);
            }
            else
            {
                Object.DestroyImmediate(m_Material);
            }
        #else
            Object.Destroy(m_Material);
        #endif

        rtTempAccumulation?.Release();
    }
    
    void UpdateParameters(Camera camera)
    {
        var volumeComponent = VolumeManager.instance.stack.GetComponent<GodrayVolumeComponent>();

        float start = volumeComponent.start.overrideState ? volumeComponent.start.value : settings.sampleRange.x;
        float end = volumeComponent.end.overrideState ? volumeComponent.end.value : settings.sampleRange.y;
        float intensity = volumeComponent.intensity.overrideState ? volumeComponent.intensity.value : settings.intensity;
        float fadeStrength = volumeComponent.fadeStrength.overrideState ? volumeComponent.fadeStrength.value : settings.fadeStrength;
        float opacity = volumeComponent.fadeStrength.overrideState ? volumeComponent.opacity.value : settings.opacity;
        VolumetricLightSamples sampleCount = volumeComponent.samples.overrideState ? volumeComponent.samples.value : settings.samplesCount;

        m_Material.SetTexture(ditherTexId, settings.ditherPattern);
        m_Material.SetFloat(opacityId, opacity);

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
            m_Material.DisableKeyword(prevSampleMode);

        m_Material.EnableKeyword(sampleMode);
        prevSampleMode = sampleMode;


        m_Material.SetFloat(fadeStrengthId, fadeStrength);
        m_Material.SetFloat(intensityId, intensity);


        Matrix4x4 VPMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix;
        m_Material.SetMatrix(inverseVPMatrixId, VPMatrix.inverse); 
        
        m_Material.SetVector(cameraDirId, camera.transform.forward);

        float planeDistance = end * (camera.farClipPlane - camera.nearClipPlane) + camera.nearClipPlane;
        Vector3 planeCenter = camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, planeDistance));
        float planeSeparation = (end - start) * (camera.farClipPlane - camera.nearClipPlane)/(float)sampleCount;

        m_Material.SetFloat(planeOffsetId, end); 
        m_Material.SetFloat(planeSeparationId, planeSeparation); 
        m_Material.SetVector(planeCenterId, planeCenter);
    }


    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;

        if (camera.cameraType != CameraType.Game) return;
        if (m_Material == null) return;
        
        CommandBuffer cmd = CommandBufferPool.Get(name: "Godray Pass");

        UpdateParameters(camera);

        using (new ProfilingScope(cmd, m_ProfilingSampler)) 
        {
            //context.ExecuteCommandBuffer(cmd);
            //cmd.Clear();
            
            RTHandle rtCamera = renderingData.cameraData.renderer.cameraColorTargetHandle;
            
            //DEPTH **BUFFER** CAN BE ACQUIRED HERE FROM THIS HANDLE:
            RTHandle rtDepth = renderingData.cameraData.renderer.cameraDepthTargetHandle;
            m_Material.SetTexture(sceneDepthId, rtDepth);
            
            
            

            //Apply Lights to the current rendered scene
            Blitter.BlitCameraTexture(cmd, rtCamera, rtTempAccumulation, m_Material, 0); 
            
            //Blit to the camera output
            Blitter.BlitCameraTexture(cmd, rtTempAccumulation, rtCamera, Vector2.one);
        }



        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }
    
}
