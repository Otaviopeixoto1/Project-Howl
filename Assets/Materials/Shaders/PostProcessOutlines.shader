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
            "RenderType"="Opaque" "RenderingPipeline"="UniversalPipeline"
        }
        ZWrite Off Cull Off
        
        

        Pass
        {
            Name "OutlinePass"

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            float _DLower;
            float _DUpper;
            float _NLower;
            float _NUpper;

            struct MeshData
            {
                float4 positionHCS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Interpolators
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Interpolators vert(MeshData input)
            {
                Interpolators o;
                UNITY_SETUP_INSTANCE_ID(input);

                o.positionCS = float4(input.positionHCS.xy, 0.0, 1.0);
                o.uv = input.uv;

                // If we're on a Direct3D like platform
                #if UNITY_UV_STARTS_AT_TOP
                    // Flip UVs
                    o.uv = o.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
                #endif
                
                return o;
            }


            sampler2D _CameraDepthTexture;
            //sampler2D _CameraNormalsTexture;
            sampler2D _GBuffer2;
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
                    diff += depthSamples[i] - depthSamples[0];
                    biasedDiff += clamp(depthSamples[i] - depthSamples[0],0,1);
                }
                for(int i = 3; i < 5 ; i++)
                {
                    diff += (depthSamples[i] - depthSamples[0]) * 0.5; // 0.2297 -> sin(fov/2)
                    biasedDiff += clamp(depthSamples[i] - depthSamples[0],0,1) * 0.5; // 0.2297 -> sin(fov/2)
                }
                
                diff = diff > 0;
                //return diff;

                float2 depthStrength = float2(smoothstep(_DLower , _DUpper , biasedDiff), diff);
                return depthStrength; //expose these numbers
            }

            float GetNormalStrength(float3 normalSamples[5], float depthIndicator, float3 directionBias)
            {
                float normalIndicator = 0;
                for(int i = 1; i < 5 ; i++)
                {
                    float sharpness = 1 - dot(normalSamples[0], normalSamples[i]);
                    float3 normaldiff = normalSamples[0] - normalSamples[i];

                    //the direction bias reduces the contribution of normals pointing away from screen
                    float normalBias = smoothstep(-0.01f, 0.01f, dot(normaldiff, directionBias));
                    normalIndicator += sharpness * normalBias;
                }

                float normalStrength = smoothstep(_NLower,_NUpper, normalIndicator * depthIndicator );
                return normalStrength;
            }


            float4 frag(Interpolators input) : SV_Target
            {
                float2 texelSize = float2(_CameraOpaqueTexture_TexelSize.x, _CameraOpaqueTexture_TexelSize.y);

                float2 uvSamples[5];
                float depthSamples[5];
                float3 normalSamples[5];

                float4 color = tex2D(_CameraOpaqueTexture, input.uv).rgba;
                //return color;

                uvSamples[0] = input.uv;
                uvSamples[1] = input.uv - float2(texelSize.x, 0);
                uvSamples[2] = input.uv + float2(texelSize.x, 0);
                uvSamples[3] = (input.uv + float2(0, texelSize.y));
                uvSamples[4] = (input.uv - float2(0, texelSize.y));


                for(int i = 0; i < 5 ; i++)
                {
                    depthSamples[i] = Linear01Depth(tex2D(_CameraDepthTexture, uvSamples[i]).r)/((1+input.uv.y));

                    normalSamples[i] = tex2D(_GBuffer2, uvSamples[i]).rgb;
                    normalSamples[i] = normalize(mul((float3x3)UNITY_MATRIX_MV, normalSamples[i]));
                }
                //return depthSamples[0];
                //return float4(normalSamples[0],1);

                float2 depthStrength = GetDepthStrength(depthSamples,input.uv.y);
                //return depthStrength.x; //depthStrength.y is also a good outline (maybe even better)
                //depthStrength.x = 0;

                float normalStrength = GetNormalStrength(normalSamples,depthStrength.y,float3(1,1,1));
                //return normalStrength; 

                float strength = depthStrength.x * (1 - 0.5 * depthStrength.x) 
                                +(1-depthStrength.x) * (1 + 0.5 * normalStrength);
                //float strength = depthStrength.x > 0 ?
                //               (1 - 0.5 * depthStrength.x) : ( 1 + 0.5* normalStrength);

                return color * strength ;


            }
            ENDHLSL
        }
    }
}