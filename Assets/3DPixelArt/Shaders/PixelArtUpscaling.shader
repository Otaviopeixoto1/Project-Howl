Shader "Hidden/PixelArtAntialias"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            

            struct MeshData
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            Interpolators vert (MeshData v)
            {
                Interpolators o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            
            sampler2D _PrevTex;
            sampler2D _UpscaledTex;

            float4 frag (Interpolators i) : SV_Target
            {
                /* BILINEAR FILTERED SAMPLES WITH ROTATION:
                // SNAPPING INTRODUCES THE ALIASING ISSUES:
                // i.uv = round(i.uv * _MainTex_TexelSize.zw) * _MainTex_TexelSize.xy;
                // ROTATION
                float c = cos(_Time.x);
                float s = sin(_Time.x);
                i.uv -= 0.5;
                i.uv.x = c*i.uv.x - s*i.uv.y;
                i.uv.y = s*i.uv.x + c*i.uv.y;
                i.uv += 0.5;*/


                float2 boxSize = clamp(fwidth(i.uv) * _MainTex_TexelSize.zw, 1e-5, 5) ;
                float2 tx = i.uv * _MainTex_TexelSize.zw - 0.5 * boxSize;
                float2 txOffset = smoothstep(1-boxSize, 1, frac(tx));
                float2 uv = (floor(tx) + 0.5 + txOffset) * _MainTex_TexelSize.xy;

                return tex2Dgrad(_MainTex, uv, ddx(i.uv),ddy(i.uv));
            }
            ENDHLSL
        }
    }
}
