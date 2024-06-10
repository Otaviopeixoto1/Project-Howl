// ----------------------------------------------------------------------------------------------------------
// The terrain using this shader wont appear on the depthNormals prepass, its only rendered on the final pass
// This is NECESSARY for good outlines
// ----------------------------------------------------------------------------------------------------------

Shader "TerrainShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} //texture atlas
        _colorRamp("ColorRamp", 2D) = "white" {}
        //_atlasScale("Atlas Scale", float) = 1
        _lightIntensityBias("Light Intensity Bias", Range(0.0, 1.0)) = 0
        _ambientLightBias("Ambient Light Bias", Range(0.0, 1.0)) = 0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "UniversalMaterialType" = "Lit"
            
        }

        //ZWrite On Cull On ZTest LEqual

        // Include material cbuffer for all passes. 
        // The cbuffer has to be the same for all passes to make this shader SRP batcher compatible.
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float4 _MainTex_ST;
        float4 _colorRamp_ST;
        //float _atlasScale;
        float _lightIntensityBias;
        float _ambientLightBias;
        CBUFFER_END
        ENDHLSL

        ZWrite On 
        Cull Off 
        ZTest LEqual
        
        Pass
        {
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
            
            

            struct MeshData
            {
                float4 positionOS : POSITION; //position in Object Space
                float2 uv : TEXCOORD0;
                float2 atlasUV : TEXCOORD3;
                float3 normal : NORMAL;
            };

            struct Interpolators
            {
                float4 positionHCS : SV_POSITION; //position in Homogeneous Clip Space

                float2 uv : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 positionWS : TEXCOORD2; //position in World Space
                float2 atlasUV : TEXCOORD3;
                float lightIntensity : TEXCOORD4;
                
            };

            sampler2D _MainTex;
            sampler2D _colorRamp;


            Interpolators vert (MeshData v)
            {
                Interpolators o;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(v.positionOS.xyz);

                o.positionHCS = positionInputs.positionCS;
                o.positionWS = positionInputs.positionWS;


                o.atlasUV = v.atlasUV;// /_atlasScale
                o.normal = TransformObjectToWorldNormal(v.normal);


                float3 N = o.normal;    
                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;

                o.lightIntensity =  saturate(0.5*(dot(N ,L) + 1) + _lightIntensityBias);
                return o;
            }

            half4 frag (Interpolators i) : SV_Target
            {
                //return half4(i.atlasUV.x, i.atlasUV.y, 0, 1);

                //return half4(i.normal,1);

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord); //light data with shadows  

                half4 lightColor = half4(mainLight.color.rgb,1);

                #if defined(_LIGHT_COOKIES)
                    // cloud light cookie
                    half4 cookieColor = half4(SampleMainLightCookie(i.positionWS), 1); 
                    lightColor *= cookieColor;
                #endif
                

                float toonLighting =  tex2D(_colorRamp, float2(i.lightIntensity,0)) + 0.5;

                half4 col = tex2D(_MainTex, i.atlasUV);
                //return half4(i.atlasUV,0,1);
                
                float shadowAttenuation = mainLight.shadowAttenuation;
                
                return (col) * saturate( toonLighting * lightColor * shadowAttenuation + _ambientLightBias );
            }
            ENDHLSL
        }
    }
}
