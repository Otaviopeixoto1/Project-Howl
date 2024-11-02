using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEditor;


public class PixelAdvectionPass : ScriptableRenderPass
{
    private ProfilingSampler m_ProfilingSampler;
    
    Material m_Material;

    RTHandle tempCurrentFrame;
    RenderTexture previousFrame;


    static int 
        currentTexId = Shader.PropertyToID("_MainTex"),
        prevTexId = Shader.PropertyToID("_PrevTex"); 


    public PixelAdvectionPass(Material material)
    {
        m_Material = material;
        previousFrame = new RenderTexture(new RenderTextureDescriptor(1,1));
        previousFrame.filterMode = FilterMode.Bilinear;
        m_ProfilingSampler = new ProfilingSampler("Pixel Advection Pass");
    } 

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        base.OnCameraSetup(cmd, ref renderingData);

        //This will render to the backbuffer
    }

    public void Setup(RTHandle upscaledTarget, RenderTextureDescriptor desc)
    {
        this.tempCurrentFrame = upscaledTarget;

        if (previousFrame == null) return;

        bool requiresResize = false;
        requiresResize = requiresResize || previousFrame.descriptor.height != desc.height;
        requiresResize = requiresResize || previousFrame.descriptor.width != desc.width;
        requiresResize = requiresResize || previousFrame.descriptor.colorFormat != desc.colorFormat;

        if (!previousFrame.IsCreated() || requiresResize)
        {
            previousFrame.Release();
            desc.depthBufferBits = 0;
            previousFrame.descriptor = desc;
            previousFrame.Create();
        }
        
    }

    //clean-up allocated Resources
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
        previousFrame.Release();
        //tempPrevFrame?.Release();
    }



    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        Camera camera = renderingData.cameraData.camera;
        
        if (m_Material == null) return;
        
        
        CommandBuffer cmd = CommandBufferPool.Get(name: "Pixel Advection Pass");

        m_Material.SetTexture(currentTexId, tempCurrentFrame);
        m_Material.SetTexture(prevTexId, previousFrame);
        
    
        using (new ProfilingScope(cmd, m_ProfilingSampler)) 
        {
            RTHandle rtCamera = renderingData.cameraData.renderer.cameraColorTargetHandle;
            Blitter.BlitCameraTexture(cmd, tempCurrentFrame, rtCamera, m_Material, 0);
            
            cmd.CopyTexture(tempCurrentFrame, previousFrame);
            //RTHandle rtCamera = renderingData.cameraData.renderer.cameraColorTargetHandle;
            //cmd.Blit(rtCamera, previousFrame, Vector2.one, Vector2.zero);

            //USELESS, TAKE THE PREVIOUS ONE

            //CANT DO THIS: THE ERRORS ACCUMULATE AND THE IMAGE GETS DISTORTED. WE ARE UPSAMPLING AND DOWNSAMPLIG THE SAME IMAGE REPEATEDLY            
            //Blitter.BlitCameraTexture(cmd, rtCamera, tempPrevFrame, Vector2.one, 0, true);
            //cmd.CopyTexture(tempPrevFrame, previousFrame);
        }


        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }
    
}
