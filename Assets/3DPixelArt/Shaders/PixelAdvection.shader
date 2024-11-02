Shader "Blit/PostProcessOutlines"
{
    
    Properties
    {
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" 
            "RenderingPipeline" = "UniversalPipeline"
        }
        ZWrite Off Cull Back ZTest LEqual 

        Pass
        {
            Name "PixelAdvectionPass"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            
            // The Blit.hlsl file provides the vertex shader (Vert),
            // input structure (Attributes) and output strucutre (Varyings)
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            
            sampler2D _PrevTex;

            TEXTURE2D_X(_MotionVectorTexture);
            float4 _MotionVectorTexture_TexelSize;
            SAMPLER(sampler_MotionVectorTexture);
            float4 mainCameraMotion;

            float4 frag(Varyings input) : SV_Target
            {
                //
                // THIS NEEDS MORE THAN BILINEAR + MOTION VECTORS. 
                // IT NEEDS SOME SORT OF DEPTH AWARE BILATERAL FILTERING 
                // JUST NEED TO FIGURE OUT THE DEPTH ISSUE, THE BILINEAR FILTER SOLVES EVERYTHING
                //
                // **THIS HAPPENS BECAUSE THE PIXELS ARE MOVING IN THE DEPTH AS WELL AS SCREEN. IF IT WAS ONLY SCREEN MOVEMENT 
                //   IT WOULD BE FINE, BILINEAR WOULD SOLVE IT (SEE UV ROTATION EXAMPLE BELOW) BUT THERE HAS TO BE A CORRECTION 
                //   WHEN SAMPLING FROM DIFFERENT DEPTHS ***
                //
                
                /*
                float2 pixelMotion = round(SAMPLE_TEXTURE2D_X(_MotionVectorTexture, sampler_MotionVectorTexture, input.texcoord) * _MotionVectorTexture_TexelSize.zw);
                pixelMotion += mainCameraMotion.xy;
                
                //return float4(abs(pixelMotion) > 0,0,1);
                pixelMotion *= _MotionVectorTexture_TexelSize.xy;
                //return float4(abs(pixelMotion),0,1);
                return tex2D(_PrevTex, input.texcoord - pixelMotion);
                */

                
                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
            }
            ENDHLSL
        }
    }
}