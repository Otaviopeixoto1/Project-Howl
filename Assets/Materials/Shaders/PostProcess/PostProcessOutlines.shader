Shader "Unlit/PostProcessOutlines"
{
    
    Properties
    {
        _DLower("DepthLowerThreshold", float) = 0.04
        _DUpper("DepthUpperThreshold",  float) = 0.05
        _NLower("NormalLowerThreshold", float) = 0.05
        _NUpper("NormalUpperThreshold", float) = 0.1
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque" 
            "RenderingPipeline" = "UniversalPipeline"
        }
        ZWrite Off Cull Off ZTest Always
        
        

        Pass
        {
            Name "OutlinePass"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            
            // The Blit.hlsl file provides the vertex shader (Vert),
            // input structure (Attributes) and output strucutre (Varyings)
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"



            float _DLower;
            float _DUpper;
            float _NLower;
            float _NUpper;



            sampler2D _CameraDepthTexture;
            sampler2D _CameraNormalsTexture;
            //sampler2D _GBuffer2;
            sampler2D _CameraOpaqueTexture;

            float4 _CameraOpaqueTexture_TexelSize;



            inline float Linear01Depth( float z )
            {
                return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
            }

            float2 GetDepthStrength(float depthSamples[5],float uvy)
            {
                float biasedDiff = 0;
                float diff = 0;
                for(int i = 1; i < 3 ; i++)
                {
                    diff += (depthSamples[i] - depthSamples[0]);
                    biasedDiff += clamp(-(depthSamples[i] - depthSamples[0]),0,1);
                }
                for(int i = 3; i < 5 ; i++)
                {
                    diff += (depthSamples[i] - depthSamples[0]); // *0.5 perspective
                    biasedDiff += clamp(-(depthSamples[i] - depthSamples[0]),0,1); //  *0.5 perspective
                }
                
                diff = diff < 0.0;

                float2 depthStrength = float2(smoothstep(_DLower , _DUpper , biasedDiff), diff);
                //float2 depthStrength = float2(smoothstep(_DLower , _DUpper , biasedDiff), diff);
                return depthStrength; 
            }

            float GetNormalStrength(float3 normalSamples[5], float depthIndicator, float3 directionBias)
            {
                float normalIndicator = 0;
                for(int i = 1; i < 5 ; i++)
                {
                    float sharpness = 1 - dot(normalSamples[0], normalSamples[i]);
                    float3 normaldiff = normalSamples[0] - normalSamples[i];

                    float normalBias = smoothstep(-0.01f, 0.01f, dot(normaldiff, directionBias));
                    normalIndicator += sharpness * normalBias;
                }

                float normalStrength = smoothstep(_NLower,_NUpper, normalIndicator * depthIndicator);
                return normalStrength;
            }





            float4 frag(Varyings input) : SV_Target
            {
                //return 1;
                
                float2 texelSize = float2(_CameraOpaqueTexture_TexelSize.x, _CameraOpaqueTexture_TexelSize.y);

                float2 uvSamples[5];
                float depthSamples[5];
                float3 normalSamples[5];

                float4 color = tex2D(_CameraOpaqueTexture, input.texcoord).rgba;
                //return color;

                uvSamples[0] = input.texcoord;
                uvSamples[1] = input.texcoord - float2(texelSize.x, 0);
                uvSamples[2] = input.texcoord + float2(texelSize.x, 0);
                uvSamples[3] = (input.texcoord + float2(0, texelSize.y));
                uvSamples[4] = (input.texcoord - float2(0, texelSize.y));


                for(int i = 0; i < 5 ; i++)
                {
                    //use Linear01Depth on perspective camera
                    depthSamples[i] = (tex2D(_CameraDepthTexture, uvSamples[i]).r);///((1+input.uv.y));

                    normalSamples[i] = tex2D(_CameraNormalsTexture, uvSamples[i]).rgb;
                    normalSamples[i] = normalize(mul((float3x3)UNITY_MATRIX_MV, normalSamples[i]));
                }
                //return depthSamples[0];
                //return float4(normalSamples[0],1);

                float2 depthStrength = GetDepthStrength(depthSamples,input.texcoord.y);
                //return depthStrength.y; //depthStrength.y is also a good outline (maybe even better)
                //depthStrength.x = 0;

                float normalStrength = GetNormalStrength(normalSamples,depthStrength.y,float3(1,1,1));
                //return normalStrength; 

                float strength = depthStrength.x * (1 - 0.5 * depthStrength.x) 
                                +(1-depthStrength.x) * (1 + 0.5 * normalStrength);

                //strength = depthStrength.y;
                //float strength = depthStrength.x > 0 ?
                //               (1 - 0.5 * depthStrength.x) : ( 1 + 0.5* normalStrength);
                float4 finalColor = color * strength;
                return float4(finalColor.rgb, 1) ;


            }
            ENDHLSL
        }
    }
}