Shader "Unlit/CloudColorShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorRamp("Color Ramp", 2D) = "white" {}
        _Opacity ("Cloud Opacity", Range(0,1)) = 0.9
        _Density ("Cloud Density", Range(0,1)) = 0.9
    }
    SubShader
    {
       Lighting Off
       Blend Off
 
       Pass
       {
           CGPROGRAM
           #include "UnityCustomRenderTexture.cginc"
           #include "Assets/Data/Shaders/ShaderIncludes/noiseSimplex.cginc"
            
           #pragma vertex CustomRenderTextureVertexShader
           #pragma fragment frag
           #pragma target 3.0
 
           sampler2D _MainTex;
           sampler2D _ColorRamp;
           float _Opacity;
           float _Density;

           float4 _TexelSize;
 
           float4 frag(v2f_customrendertexture IN) : COLOR
           {
                //return tex2D(_MainTex, IN.localTexcoord.xy).rrrr;
                float cloudNoise = tex2D(_MainTex, IN.localTexcoord.xy).r;
                cloudNoise = (( 2.0 * _Density - 1.0 + cloudNoise));
                
                float4 cloudColor = tex2D(_ColorRamp, float2(max(1.0f - cloudNoise, 1.0f - _Opacity),0));

                float2 pixelCoords = _TexelSize.zw * IN.localTexcoord.xy;
                return all(pixelCoords > 1 && pixelCoords < (_TexelSize.zw - 1)) ? cloudColor : float4(1,1,1,0);
           }
           ENDCG
        }
    }
}
