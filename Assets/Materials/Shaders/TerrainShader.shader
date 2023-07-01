Shader "TerrainShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} //texture atlas
        _colorRamp("ColorRamp", 2D) = "white" {}
        _atlasScale("Atlas Scale", float) = 1
        _ambientLightBias("Ambient Light Bias", float) = 0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            
        }
        LOD 100
        
        Pass
        {
            
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            
            

            struct MeshData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 atlasUV : TEXCOORD3;
                float3 normal : NORMAL;
            };

            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float2 atlasUV : TEXCOORD3;
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _colorRamp;
            float _atlasScale;
            float _ambientLightBias;


            Interpolators vert (MeshData v)
            {
                Interpolators o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.atlasUV = v.atlasUV/_atlasScale;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (Interpolators i) : SV_Target
            {
                //return 1;
                float3 N = i.normal;    
                float3 L = _WorldSpaceLightPos0.xyz;

                float lightIntensity = saturate(0.5*(dot(N,L) + 1) + _ambientLightBias);

                float toonLighting = tex2D(_colorRamp, float2(lightIntensity,0));

                fixed4 col = tex2D(_MainTex, i.atlasUV);
                
                float shadowAttenuation = 1;


                return col * shadowAttenuation * (toonLighting + 0.5);
            }
            ENDCG
        }
    }
}
