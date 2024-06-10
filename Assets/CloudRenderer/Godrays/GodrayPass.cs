using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEditor;


public class GodrayPass : ScriptableRenderPass
{
    private RTHandle rtTempAccumulation;
    private RTHandle rtTempPrevAccumulation; //This is for temporal reprojection on the godrays. It keeps the previous frame's light accumulation
    private RTHandle rtTempColor; //final Color with lights applied
    
    private ProfilingSampler m_ProfilingSampler;
    
    private Material m_Material;

    private Light mainLight;
    private GodraySettings settings;

    

    //RTHandle m_CameraColorTarget; //(color buffer) output target
    int 
        fadeStrengthId = Shader.PropertyToID("fadeStrength"),
        intensityId = Shader.PropertyToID("intensity"),
        planeOffsetId = Shader.PropertyToID("planeOffset"),
        planeNormalId = Shader.PropertyToID("planeNormal"),
        planeCenterId = Shader.PropertyToID("planeCenter"),
        planeSeparationId = Shader.PropertyToID("planeSeparation"),
        cameraDirId = Shader.PropertyToID("cameraDir"),
        inverseVPMatrixId = Shader.PropertyToID("inverseVPMatrix"); 


    public GodrayPass(Material material, GodraySettings settings)
    {
        m_Material = material;
        this.settings = settings;
        mainLight = RenderSettings.sun;

        m_ProfilingSampler = new ProfilingSampler("Godray Pass");
    } 
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        base.OnCameraSetup(cmd, ref renderingData);

        var colorDesc = renderingData.cameraData.cameraTargetDescriptor;
        colorDesc.depthBufferBits = 0; // must set to 0 to specify a color only target
        // to use a different format, set .colorFormat or .graphicsFormat
        
        //setup the temporary render target used for blitting 
        RenderingUtils.ReAllocateIfNeeded(ref rtTempAccumulation, colorDesc, name: "_TemporaryColorTexture_GodrayAccumulation");
        //RenderingUtils.ReAllocateIfNeeded(ref rtTempPrevAccumulation, colorDesc, name: "_TemporaryColorTexture_GodrayPrevAccumulation");
        //RenderingUtils.ReAllocateIfNeeded(ref rtTempAccumulation, colorDesc, name: "_TemporaryColorTexture_GodrayAccumulation");
    }

    public override void OnCameraCleanup(CommandBuffer cmd) 
    {

    }

    //clean-up allocated RTHandle
    public void Dispose() {
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
                    Object.Destroy(material);
        #endif

        rtTempAccumulation?.Release();
        //rtTempPrevAccumulation?.Release();
    }

    void UpdateParameters(Camera camera)
    {
        var volumeComponent = VolumeManager.instance.stack.GetComponent<GodrayVolumeComponent>();
        float start = volumeComponent.start.overrideState ? volumeComponent.start.value : settings.sampleRange.x;
        float end = volumeComponent.end.overrideState ? volumeComponent.end.value : settings.sampleRange.y;
        float intensity = volumeComponent.intensity.overrideState ? volumeComponent.intensity.value : settings.intensity;
        float fadeStrength = volumeComponent.fadeStrength.overrideState ? volumeComponent.fadeStrength.value : settings.fadeStrength;
        int sampleCount = volumeComponent.samples.overrideState ? volumeComponent.samples.value : settings.sampleCount;

        m_Material.SetFloat(fadeStrengthId, fadeStrength);
        m_Material.SetFloat(intensityId, intensity);


        Matrix4x4 VPMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix;
        m_Material.SetMatrix(inverseVPMatrixId, VPMatrix.inverse); 
        m_Material.SetVector(cameraDirId, camera.transform.forward);

        Vector3 lightDir = mainLight.transform.forward;
        Vector3 tangent = Vector3.Cross(lightDir, -camera.transform.forward); 
        Vector3 normal = Vector3.Cross(tangent, lightDir); 
        m_Material.SetVector(planeNormalId, normal.normalized);


        float planeDistance = start * (camera.farClipPlane - camera.nearClipPlane) + camera.nearClipPlane;
        Vector3 planeCenter = camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, planeDistance));

        float planeSeparation = (end - start) * (camera.farClipPlane - camera.nearClipPlane)/sampleCount;

        m_Material.SetFloat(planeOffsetId, start); 
        m_Material.SetFloat(planeSeparationId, planeSeparation); 
        m_Material.SetVector(planeCenterId, planeCenter);
    }


    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        
        if (camera.cameraType != CameraType.Game) return;
        if (m_Material == null) return;
        
        CommandBuffer cb = CommandBufferPool.Get(name: "Godray Pass");

        UpdateParameters(camera);


        using (new ProfilingScope(cb, m_ProfilingSampler)) 
        {
            context.ExecuteCommandBuffer(cb);
            cb.Clear();
            
            RTHandle rtCamera = renderingData.cameraData.renderer.cameraColorTargetHandle;

            //Accumulate Current Lights + prev Lights
            //Blitter.BlitCameraTexture(cb, rtTempPrevAccumulation, rtTempAccumulation, m_Material, 0); 
            
            //Apply Lights to the current rendered scene
            Blitter.BlitCameraTexture(cb, rtCamera, rtTempAccumulation, m_Material, 0); 
            
            //Blit to the camera output
            Blitter.BlitCameraTexture(cb, rtTempAccumulation, rtCamera, Vector2.one);
        }



        context.ExecuteCommandBuffer(cb);
        cb.Clear();
        CommandBufferPool.Release(cb);
    }
    
}
