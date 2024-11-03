Shader "Blit/GodraysPass"
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
            Name "Render Godrays"

            HLSLPROGRAM

            #pragma vertex Vert //must set vertex shader name this way to use Utilities/Blit.hlsl 
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #pragma multi_compile _ _LIGHT_COOKIES
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma multi_compile SAMPLES_10 SAMPLES_30 SAMPLES_50

            // The Blit.hlsl file provides the vertex shader (Vert),
            // input structure (Attributes) and output strucutre (Varyings)
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            //Utilities for sampling camera depth texture:
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            //TEXTURE2D_SHADOW(_MainLightShadowmapTexture);
            sampler2D _cloudNoiseTex;
            sampler2D sceneDepth;
            sampler2D ditherTex;

            float intensity;
            float4x4 inverseVPMatrix; 
            float4 cameraDir;
            float4 planeCenter;
            float planeOffset;
            float planeSeparation;
            float fadeStrength;
            float opacityMultiplier;

            float exposure;

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                
                int2 interleavedCoord = (floor(uv * _BlitTexture_TexelSize.zw)) % 8;
                float ditherOffset =  tex2D(ditherTex, interleavedCoord/8.0f).a;
                //ditherOffset = 0;

                //////////////////////////////////////////////////
                //return tex2D(sceneDepth, uv );
                //////////////////////////////////////////////////
                
                float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgba;

                float2 ndcCoord = 2.0 * (uv - 0.5);

                #if UNITY_UV_STARTS_AT_TOP
                    ndcCoord.y = -ndcCoord.y;
                #endif
                
                #if UNITY_REVERSED_Z
                    //near plane z = 1 and far plane z = 0
                    float z = 1.0 - planeOffset;
                    float depth = SampleSceneDepth(uv);
                #else
                    float z = planeOffset;
                    // Adjust z to match NDC for OpenGL
                    float depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
                #endif
                
                Light mainLight = GetMainLight(); //light data with shadows  
                float3 lightDir = mainLight.direction.xyz;
                float3 tangent = cross(lightDir.xyz, -cameraDir.xyz); 
                float3 normal = normalize(cross(tangent, lightDir)); 
                float4 worldCoord = mul(inverseVPMatrix, float4(ndcCoord.x, ndcCoord.y, z, 1));
                //return worldCoord.y/100.0f;
                float t = dot((worldCoord.xyz - planeCenter.xyz), normal) / (dot(cameraDir.xyz, normal));
                //return t;
                worldCoord -= cameraDir * (t + ditherOffset * planeSeparation);

                float3 sceneCoord = ComputeWorldSpacePosition(uv, depth, inverseVPMatrix);
                float sceneDepthDif = dot(sceneCoord - worldCoord.xyz, cameraDir.xyz);

                


                float4 accum = float4(1, 1, 1, 1); //This should be default start
                //float4 accum = float4(0, 0, 0, 1); //This should be for debug


                #if defined(_LIGHT_COOKIES)  

                #if defined(SAMPLES_50)
                const float samples = 50.0f; 
                #elif defined(SAMPLES_30)
                const float samples = 30.0f; 
                #else
                const float samples = 10.0f; 
                #endif
                
                float4 lightColor = float4(mainLight.color.rgb * intensity * 10.0f / samples, 1);
                

                [unroll]
                for (float i = 0.0; i < samples; i++) 
                {   
                    float4 shadowCoord = TransformWorldToShadowCoord(worldCoord.xyz);
                    float lightSpaceDepth = SAMPLE_TEXTURE2D(_MainLightShadowmapTexture, sampler_LinearClamp, shadowCoord.xy).r;
                    float depthDif = shadowCoord.z - lightSpaceDepth ;
                    //return depthDif * 10;
                    float depthFade = 1.0f - pow(saturate(1 - depthDif * 10), 10.0/fadeStrength);
                    //float depthFade = depthDif * 50;
                    //return depthFade;
                    float3 cloudShadowAttenuation = saturate(SampleMainLightCookie(worldCoord.xyz) - 0.7f);
                                    
                                    //if the plane is behind the scene dont count the ray
                    float rayAlpha = (sceneDepthDif < 0) ? 0 : (cloudShadowAttenuation.r * opacityMultiplier * 0.05f * 30.0 / samples);
                    //return rayAlpha * depthFade * 10;
                    //return lightColor.rgba * (depthFade * rayAlpha);
                    //float alpha = rayAlpha + accum.a * (1-rayAlpha);
                    //accum.rgb = (lightColor.rgb * rayAlpha + accum.rgb * accum.a * (1-rayAlpha))/alpha;
                    //accum.a = alpha;
                    accum = float4(lightColor.rgb * (depthFade * rayAlpha) + accum.rgb * (1-rayAlpha), ( rayAlpha + accum.a * (1-rayAlpha)) );
                    
                    worldCoord -= cameraDir * planeSeparation;
                    sceneDepthDif += planeSeparation;
                }

                #endif
                return float4(1 - exp(-accum.rgb * exposure), 1);
                

                
            }
            ENDHLSL
        }
    }
}