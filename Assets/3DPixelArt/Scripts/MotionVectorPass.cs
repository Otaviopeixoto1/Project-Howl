using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
public class MotionVectorPass : ScriptableRenderPass
{
    public MotionVectorPass()
    {
        
    }

    public void Dispose()
    {

    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        //COPY UPSCALED DEPTH TEXTURE TO THE OUTPUT OF THIS 
        return;
    }
}
