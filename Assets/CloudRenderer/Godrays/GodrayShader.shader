Shader "Custom/InstancedMSMesh"
{
    Properties
    {

    }
    SubShader
    {
        Tags 
        {
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline" 
            "UniversalMaterialType" = "Lit"
        }

        // Include material cbuffer for all passes.  
        // The cbuffer has to be the same for all passes to make this shader SRP batcher compatible.
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #pragma editor_sync_compilation // maybe not necessary


        CBUFFER_START(UnityPerMaterial)
        //float _lightIntensityBias;
        //float _ambientLightBias;
        CBUFFER_END
        ENDHLSL
        

        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            ZWrite Off 
            Cull Off 
            ZTest LEqual
            Blend One One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _FORWARD_PLUS
            
            // GPU Instancing
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            

            struct MeshData
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct Interpolators
            {
                //float2 uv : TEXCOORD0;
                float4 positionWS : TEXCOORD1;
                float4 color : TEXCOORD2;
                float3 normal : TEXCOORD3;
                float4 positionCS : SV_POSITION;
                
            };

            
            struct Vert 
            {
                float4 position;
                float4 normal;
            };

            float4 centerOffset;
            float4 scale;
            float4 lightDir;

            StructuredBuffer<Vert> vertexBuffer;
            StructuredBuffer<uint> indexBuffer;
            sampler2D _cameraColorTexture;

            Interpolators vert (MeshData v, uint instanceID : SV_InstanceID)
            {
                Interpolators o;
                
                uint vertId = v.positionOS.x + v.positionOS.y;
                uint id = indexBuffer[vertId + 2 * instanceID];
                Vert vert = vertexBuffer[id];
                
                float3 up = float3(0.0, 1.0, 0.0);
                o.positionWS = float4(vert.position.xzy * scale.xyz + centerOffset.xyz + v.positionOS.z * lightDir.xyz * 500.0f, 1.0f);
                o.normal = vert.normal.xyz;
                o.color = float4(1.0, 1.0, 0.7, 1.0);
                o.positionCS = mul(UNITY_MATRIX_VP, o.positionWS);

                return o;
            }

            half4 frag (Interpolators i) : SV_Target
            {
                half4 color =  i.color;

                return color * 0.01f;//Sample Sreen Color texture
                

                Light mainLight = GetMainLight();

                float3 L = mainLight.direction;
                float diff = saturate(0.5f*(dot(i.normal, L) + 1));

                color *= diff;
                
                // FOR SAMPLING SHADOWMAP:
                //float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS.xyz);
                //Light mainLight = GetMainLight(shadowCoord);
                //float3 L = mainLight.direction;
                //float diff = saturate(0.5*(dot(i.normal, L) + 1));

                //#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                //    color *= diff * mainLight.shadowAttenuation;
                //#else
                //    color *= diff;
                //#endif

                return color;
            }
            ENDHLSL
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////
        //THE SHADOW CASTING PASS MUST BE CUSTOM IN ORDER TO PROPERLY TRANSFORM FROM OBJECT TO SHADOW COORDS
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /*
        Pass 
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            
        
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
        
            // GPU Instancing
            //#pragma multi_compile_instancing
            #pragma multi_compile_shadowcaster

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            // Shadow Casting Light geometric parameters. These variables are used when applying the shadow Normal Bias and are set by UnityEngine.Rendering.Universal.ShadowUtils.SetupShadowCasterConstantBuffer in com.unity.render-pipelines.universal/Runtime/ShadowUtils.cs
            // For Directional lights, _LightDirection is used when applying shadow Normal Bias.
            // For Spot lights and Point lights, _LightPosition is used to compute the actual light direction because it is different at each shadow caster geometry vertex.
            float3 _LightDirection;
            float3 _LightPosition;

            struct MeshData
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
            };

            struct Interpolators
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
            };

            Interpolators ShadowPassVertex(MeshData v, uint instanceID : SV_InstanceID)
            {
                Interpolators o;

                float3 positionWS =  float4((v.positionOS.xyz * boidScale + boidsIn[instanceID].position), 1.0f);
                float3 normalWS = (v.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                o.positionCS = positionCS;
                return o;
            }

            half4 ShadowPassFragment(Interpolators i) : SV_TARGET
            {
                return 0;
            }
            
            
            ENDHLSL
        }*/
        
        /*
        Pass  //DEFINE A CUSTOM ONE ONLY IF NECESSARY
        {
            Name "DepthOnly"
            Tags 
            { 
                "LightMode"="DepthOnly" 
            }
        
            ColorMask 0
            ZWrite On
            ZTest LEqual
        
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
        
            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        
            // GPU Instancing
            #pragma multi_compile_instancing
            // #pragma multi_compile _ DOTS_INSTANCING_ON
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"        //CHECK THIS TO DO A CUSTOM IMPLEMENTATION
            ENDHLSL
        }*/
    }
}
