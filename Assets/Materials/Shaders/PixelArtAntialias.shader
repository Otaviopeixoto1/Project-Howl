Shader "Hidden/PixelArtAntialias"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            fixed4 frag (Interpolators i) : SV_Target
            {
                float2 boxSize = clamp(fwidth(i.uv) * _MainTex_TexelSize.zw, 1e-5, 1);

                float2 tx = i.uv * _MainTex_TexelSize.zw - 0.5 * boxSize;

                float2 txOffset = smoothstep(1-boxSize, 1, frac(tx));

                float2 uv = (floor(tx) + 0.5 + txOffset) *  _MainTex_TexelSize.xy;

                return tex2Dgrad(_MainTex, uv, ddx(i.uv),ddy(i.uv));
            }
            ENDCG
        }
    }
}
