Shader "Custom/InstancedIndirect" 
{
    Properties
    {
        _SpriteAtlas ("Sprite Atlas", 2D) = "white" {} 
        _MapAtlas ("Map Atlas", 2D) = "white" {} //texture atlas
        _colorRamp("ColorRamp", 2D) = "white" {}
        //_atlasSize("Atlas Size", Vector) = (0,0,0,0)
        _lightIntensityBias("Light Intensity Bias", Range(0.0, 1.0)) = 0
        _ambientLightBias("Ambient Light Bias", Range(0.0, 1.0)) = 0
    }
    SubShader {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "UniversalMaterialType" = "Lit"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float4 _SpriteAtlas_ST;
        float4 _MapAtlas_ST;
        float4 _colorRamp_ST;
        //float _atlasScale;
        float _lightIntensityBias;
        float _ambientLightBias;
        CBUFFER_END
        ENDHLSL

        ZWrite On 
        Cull Back 
        ZTest LEqual

        Pass {

            Tags 
            { 
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _LIGHT_COOKIES
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            struct MeshData {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct Interpolators {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float2 auv      : TEXCOORD1;
                float lightIntensity : TEXCOORD2;
                float3 interpPositionWS : TEXCOORD3;
                float3 centerPositionWS : TEXCOORD4;
            }; 

            struct MeshProperties {
                float4x4 mat;
                float4 offsetScale;
                float3 terrainNormal;
                float2 atlasUV;
            };

            //Buffer with instanced properties:
            StructuredBuffer<MeshProperties> _Properties;

            sampler2D _MapAtlas;
            sampler2D _SpriteAtlas;
            sampler2D _colorRamp;

            Interpolators vert(MeshData i, uint instanceID: SV_InstanceID) 
            {
                Interpolators o;

                float4 pos = mul(_Properties[instanceID].mat, i.vertex);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(pos.xyz);
                o.vertex = positionInputs.positionCS;
                o.interpPositionWS = positionInputs.positionWS;

                //o.vertex = TransformObjectToHClip(pos.xyz);
                                    //TransformObjectToHClip UnityObjectToClipPos(pos)
                o.centerPositionWS = TransformObjectToWorld(mul(_Properties[instanceID].mat, float4(0,0,0,1)).xyz);
                o.uv = _Properties[instanceID].offsetScale.xy + (i.uv * _Properties[instanceID].offsetScale.zw);
                o.auv = _Properties[instanceID].atlasUV;

                //Lighting
                float3 N = _Properties[instanceID].terrainNormal;    
                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;

                o.lightIntensity = saturate(0.5*(dot(N, L) + 1) + _lightIntensityBias);
                return o;
            }

            float4 frag(Interpolators i) : SV_Target 
            {
                //USE THE COORDINATE OF THE CENTER OF THE QUAD FOR THE ENTIRE SHADOW SAMPLING
                // SAMPLE THIS IN THE VERTEX SHADER
                float4 shadowCoord = TransformWorldToShadowCoord(i.interpPositionWS);
                Light mainLight = GetMainLight(shadowCoord); 
                float4 lightColor = float4( mainLight.color.rgb,1);

                // cloud light cookie 
                #if defined(_LIGHT_COOKIES)
                    
                    float4 cookieColor = float4(SampleMainLightCookie(i.centerPositionWS), 1); 
                    lightColor *= cookieColor;
                #endif


                float4 tex = tex2D(_SpriteAtlas, i.uv );
                clip(tex.a - 0.01);
                float4 col = tex2D(_MapAtlas, i.auv);

                float toonLighting =  tex2D(_colorRamp, float2(i.lightIntensity,0)).r + 0.5;

                float shadowAttenuation = mainLight.shadowAttenuation;
                
                return  col * saturate( toonLighting * lightColor * shadowAttenuation + _ambientLightBias );
            }

            ENDHLSL
        }
    }
}
