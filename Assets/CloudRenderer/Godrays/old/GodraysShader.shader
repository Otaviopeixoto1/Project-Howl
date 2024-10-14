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
            Name "render godrays"

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

            // The Blit.hlsl file provides the vertex shader (Vert),
            // input structure (Attributes) and output strucutre (Varyings)
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            
            //Utilities for sampling camera depth texture:
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            sampler2D _cloudNoiseTex;
            sampler2D sceneDepth;

            float intensity;
            float4x4 inverseVPMatrix; 
            float4 cameraDir;
            float4 planeNormal;
            float4 planeCenter;
            float planeOffset;
            float planeSeparation;
            float fadeStrength;
            float samples;

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                
                //////////////////////////////////////////////////
                //return tex2D(sceneDepth, uv );
                //////////////////////////////////////////////////

                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).rgba;
                float3 normal = (planeNormal.xyz);
                float2 ndcCoord = 2 * (uv) - 1.0;

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

                float3 sceneCoord = ComputeWorldSpacePosition(uv, depth, inverseVPMatrix);
                //return float4(depth,0,0,1);

                float4 worldCoord = mul(inverseVPMatrix, float4(ndcCoord.x, ndcCoord.y, z, 1));
                float t = dot((worldCoord.xyz - planeCenter.xyz), normal) / (dot(cameraDir.xyz, normal));
                worldCoord -= cameraDir * t;


                Light mainLight = GetMainLight(); //light data with shadows  
                float4 lightColor = float4(mainLight.color.rgb,1);
                float4 accum = float4(0,0,0,1);
                float depthDif = dot(sceneCoord - worldCoord, cameraDir);
                
                /////////////////////////////////////////////////////////////
                /*
                float2 suv = ComputeLightCookieUVDirectional(_MainLightWorldToLight, worldCoord, float4(1, 1, 0, 0), URP_TEXTURE_WRAP_MODE_NONE);
                float4 cNoise = tex2D(_cloudNoiseTex, suv); //suv
                
                //return dot(cNoise.gb, cNoise.gb);                                                  // divide the gradient components by the cloud tile size components, then do rsqrt
                float rgmod = rsqrt(dot(cNoise.gb, cNoise.gb)); //150 = cloud cookie tile size assumed to be uniform (replace with the actual variable)
                float sdf = min(1.0, abs(cNoise.r - 0.01) * rgmod); //10.0 is the maximum march distance
                
                */
                /////////////////////////////////////////////////////////////

                #if defined(_LIGHT_COOKIES)                
                    for (float i = 0.0; i < samples; i++) // i< samples
                    {   
                        /*
                        worldCoord += cameraDir * sdf; //march

                        //suv = ComputeLightCookieUVDirectional(_MainLightWorldToLight, worldCoord, float4(1, 1, 0, 0), URP_TEXTURE_WRAP_MODE_NONE);
                        //float res = tex2D(_cloudNoiseTex, suv); // use saturate(SampleMainLightCookie(worldCoord) - 0.3f)
                        
                        accum.rgb = saturate(SampleMainLightCookie(worldCoord) - 0.3f).rgb;
                        //accum.r = res;
                        return accum;
                        
                        //accum = max(accum, float4(cloudShadowAttenuation * shadowAttenuation * depthFade, 0) ); */



                        //Problem: the current terrain doesnt render into depth buffer.
                        //There must be two copies of depth: one before and one after rendering the terrain
                        
                        /**/
                        //this formula + the max or add mode still makes an ugly gradient on screen. 
                        //The falloff gradient must be smoother
                        //float depthFade = smoothstep(0.0, 1.0, (depthDif/100.0f) * fadeStrength); 
                        //Other options:
                        //float depthFade = 1.0f - fadeStrength * exp(-max(0.0, depthDif/10.0f));
                        float depthFade = 1.0f - pow(saturate(1 - depthDif/100.0f), 1.0/fadeStrength);

                        float4 shadowCoord = TransformWorldToShadowCoord(worldCoord);
                        Light L = GetMainLight(shadowCoord); //light data with shadows 
                        float shadowAttenuation = L.shadowAttenuation;
                        float3 cloudShadowAttenuation = saturate(SampleMainLightCookie(worldCoord) - 0.3f);

                        //accum += float4(cloudShadowAttenuation * shadowAttenuation * depthFade, 0) ;
                        accum = max(accum, float4(cloudShadowAttenuation * shadowAttenuation * depthFade, 0) ); 
                        //DO max based on depth as well. IT CAN SOLVE SOME PROBLEMS
                        
                        
                        worldCoord += cameraDir * planeSeparation;
                        depthDif -= planeSeparation;
                    }
                    
                #endif
                
            
                //float LdotV = saturate(0.5 * (dot(-cameraDir, mainLight.direction) + 1));
                float4 mapped = 1.0 - exp(-lightColor * accum * intensity * 2.0);
                
                //float4 mapped = lightColor * accum * intensity;
                return (mapped + 1.0f) * color;
                return (mapped);
            }
            ENDHLSL
        }
    }
}