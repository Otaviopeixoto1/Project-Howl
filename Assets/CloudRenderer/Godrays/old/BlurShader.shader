Shader "Blit/GaussianBlur"
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

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"


        #pragma multi_compile Blur_5 Blur_9 Blur_13

        // The Blit.hlsl file provides the vertex shader (Vert),
        // input structure (Attributes) and output strucutre (Varyings)
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"


        float4 Blur5(float2 uv, float2 direction)
        {
            const float offset5[2] = {0.0, 1.3333333333333333};
            const float weight5[2] = {0.29411764705882354, 0.35294117647058826};

            float2 texOffset = _BlitTexture_TexelSize.xy * direction;

            float3 result = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb * weight5[0]; 
            result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset5[1] * texOffset).rgb * weight5[1];
            result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - offset5[1] * texOffset).rgb * weight5[1];

            return float4(result, 1.0);
            //float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgba;
        }

        float4 Blur9(float2 uv, float2 direction)
        {
            const float offset9[3] = {0.0, 1.3846153846, 3.2307692308};
            const float weight9[3] = {0.2270270270, 0.3162162162, 0.0702702703};

            float2 texOffset = _BlitTexture_TexelSize.xy * direction;
            float3 result = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb * weight9[0]; // current fragment's contribution
            
            for(int i = 1; i < 3; ++i)
            {
                result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset9[i] * texOffset).rgb * weight9[i];
                result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - offset9[i] * texOffset).rgb * weight9[i];
            }
            
            return float4(result, 1.0);
        }

        float4 Blur13(float2 uv, float2 direction)
        {
            const float offset13[4] = {0.0, 1.411764705882353, 3.2941176470588234, 5.176470588235294};
            const float weight13[4] = {0.1964825501511404, 0.2969069646728344, 0.09447039785044732, 0.010381362401148057};

            float2 texOffset = _BlitTexture_TexelSize.xy * direction;
            float3 result = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgb * weight13[0]; // current fragment's contribution
            
            for(int i = 1; i < 4; ++i)
            {
                result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + offset13[i] * texOffset).rgb * weight13[i];
                result += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - offset13[i] * texOffset).rgb * weight13[i];
            }
            
            return float4(result, 1.0);
        }

        ENDHLSL



        ZWrite Off Cull Back ZTest LEqual 
        
        
        Pass
        {
            Name "Horizontal Blur"

            HLSLPROGRAM

            #pragma vertex Vert //must set vertex shader name this way to use Utilities/Blit.hlsl 
            #pragma fragment frag
            

            float4 frag(Varyings input) : SV_Target
            {
                return Blur13(input.texcoord, float2(1, 0));
            }
            ENDHLSL
        }

        Pass
        {
            Name "Vertical Blur"

            HLSLPROGRAM

            #pragma vertex Vert //must set vertex shader name this way to use Utilities/Blit.hlsl 
            #pragma fragment frag

            sampler2D sceneColorTex;

            float4 frag(Varyings input) : SV_Target
            {
                float4 sceneColor = tex2D(sceneColorTex, input.texcoord);
                return sceneColor * Blur13(input.texcoord, float2(0, 1));
            }
            ENDHLSL
        }
    }
}