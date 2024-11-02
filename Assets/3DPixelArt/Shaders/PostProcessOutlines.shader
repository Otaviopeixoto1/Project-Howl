Shader "Blit/PostProcessOutlines"
{
    
    Properties
    {
        _SourceTex ("Texture", 2D) = "white"

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
        ZWrite Off Cull Back ZTest LEqual 

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
            float strengthMultiplier;
            float4 viewDirection;
            float4 viewUpDirection;
            float4 viewRightDirection;


            sampler2D _CameraDepthTexture;
            sampler2D _CameraNormalsTexture;
            

            inline float Linear01Depth( float z )
            {
                return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
            }
                //JOIN THIS WITH COLOR STRENGTH
            float3 GetDepthStrength(float depthSamples[5],float uvy)
            {
                float biasedDiff = 0;
                float2 diff = float2(0.0, 0.0);
                
                //unroll loops:
                float horizontalMask = 0.5 * (depthSamples[1] + depthSamples[2]) > depthSamples[0];
                diff.x = smoothstep(0.0, 0.01, abs(depthSamples[1] - depthSamples[2]));
                //diff.x += depthSamples[1] - depthSamples[0];
                //diff.x += depthSamples[2] - depthSamples[0];

                biasedDiff += clamp((depthSamples[0] - depthSamples[1]),0,1);
                biasedDiff += clamp((depthSamples[0] - depthSamples[2]),0,1);
                
                float verticalMask = 0.5 * (depthSamples[3] + depthSamples[4]) > depthSamples[0];
                diff.y = smoothstep(0.0, 0.01, abs(depthSamples[3] - depthSamples[4]));
                //diff.y += (depthSamples[3] - depthSamples[0]);
                //diff.y += (depthSamples[4] - depthSamples[0]);
                
                biasedDiff += clamp((depthSamples[0] - depthSamples[3]),0,1);
                biasedDiff += clamp((depthSamples[0] - depthSamples[4]),0,1);
                

                float3 depthStrength = float3(smoothstep(_DLower , _DUpper , biasedDiff), diff);
                return depthStrength; 
            }

            float GetNormalStrength(float3 normalSamples[5])
            {
                float2 normalIndicator = float2(0.0, 0.0);  
                float sharpness;
                float3 normalDiff[2];

                // Vertical Outlines
                float3 horizontalDiff = normalSamples[2] - normalSamples[1];
                sharpness = 1 - dot(normalSamples[0], normalSamples[2]);
                //return sharpness;
                normalDiff[0] = abs(normalSamples[0] - normalSamples[1]);
                normalDiff[1] = abs(normalSamples[0] - normalSamples[2]);
                //normalIndicator.x += smoothstep(0.1f, 0.3f, normalDiff[0].x + normalDiff[0].y + normalDiff[0].z);
                normalIndicator.x += smoothstep(0.1f, 0.3f, normalDiff[1].x + normalDiff[1].y + normalDiff[1].z);
                //return normalIndicator.x;
                //normalIndicator.x *= sharpness;

                // ***** Use These pixels for dark outlines instead *****
                normalIndicator.x *= dot(horizontalDiff, viewRightDirection.xyz) > 0;
                //return (normalIndicator.x);
                
                
                // Horizontal Outlines
                float3 verticalDiff = normalSamples[3] - normalSamples[4];
                sharpness = 1 - dot(normalSamples[0], normalSamples[3]);
                //return sharpness;
                normalDiff[0] = abs(normalSamples[0] - normalSamples[3]);
                normalDiff[1] = abs(normalSamples[0] - normalSamples[4]);
                normalIndicator.y += smoothstep(0.0,  0.3, normalDiff[1].x + normalDiff[1].y + normalDiff[1].z);
                //normalIndicator.y += smoothstep(0.0,  0.1f, normalDiff[1].x + normalDiff[1].y + normalDiff[1].z);
                //return (fwidth(normalDiff[1].x + normalDiff[1].y + normalDiff[1].z));
                //return ((normalIndicator.y));
                //normalIndicator.y *= sharpness;

                // ***** Use These pixels for dark outlines instead *****
                normalIndicator.y *= dot(verticalDiff, viewUpDirection.xyz) > 0; 
                //return normalIndicator.y;

                //return normalIndicator.x + normalIndicator.y;
                float normalStrength = smoothstep(_NLower, _NUpper, (normalIndicator.x + normalIndicator.y));
                return normalStrength;
            }





            float4 frag(Varyings input) : SV_Target
            {
                float2 texelSize = float2(_BlitTexture_TexelSize.x, _BlitTexture_TexelSize.y);

                float2 uvSamples[5];
                uvSamples[0] = input.texcoord;
                uvSamples[1] = input.texcoord - float2(texelSize.x, 0);
                uvSamples[2] = input.texcoord + float2(texelSize.x, 0);
                uvSamples[3] = (input.texcoord + float2(0, texelSize.y));
                uvSamples[4] = (input.texcoord - float2(0, texelSize.y));

                float3 colorSamples[5];
                [unroll]
                for(int i = 0; i < 5 ; i++)
                {
                    colorSamples[i] = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uvSamples[i]).rgb;
                }

                float depthSamples[5];
                [unroll]
                for(int j = 0; j < 5 ; j++)
                {
                    depthSamples[j] = Linear01Depth(tex2D(_CameraDepthTexture, uvSamples[j]).r);
                }
                float3 depthStrength = GetDepthStrength(depthSamples,input.texcoord.y);
                //return depthStrength.x;
                
                float depthMask = 1 - clamp(depthStrength.y + depthStrength.z, 0, 1);
                depthMask = depthMask > 0.8;
                //return depthMask;


                float3 normalSamples[5];
                [unroll]
                for(int k = 0; k < 5 ; k++)
                {
                    normalSamples[k] = tex2D(_CameraNormalsTexture, uvSamples[k]).rgb;
                    normalSamples[k] = normalize(mul((float3x3)UNITY_MATRIX_MV, normalSamples[k]));
                }

                //
                // TEST:
                // THE DOT BETWEEN NORMAL AND SOME DIRECTION OR DIRECTIONS IS VERY GOOD FOR
                // FINDING DISCONTINUITIES ON THE SURFACE:
                // return dot(viewDirection.xyz, normalSamples[0]);
                //

                float normalStrength = depthMask * GetNormalStrength(normalSamples);
                
                float strength = -depthStrength.x * (depthStrength.x) 
                                +(1-depthStrength.x) * (normalStrength);
                
                //strength =  5*(strength > 0.9);
                //return strength;
                float3 finalColor = colorSamples[0] * (1 + strengthMultiplier * strength);
                return float4(finalColor.rgb, 1) ;


            }
            ENDHLSL
        }
    }
}