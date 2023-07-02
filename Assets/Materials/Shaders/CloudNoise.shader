Shader "CustomRenderTexture/Simple"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _Tex("InputTex", 2D) = "white" {}
    }
 
        SubShader
    {
       Lighting Off
       Blend One Zero
 
       Pass
       {
           CGPROGRAM
           #include "UnityCustomRenderTexture.cginc"
           #include "Assets/Materials/Shaders/ShaderIncludes/noiseSimplex.cginc"
            
           #pragma vertex CustomRenderTextureVertexShader
           #pragma fragment frag
           #pragma target 3.0
 
           float4 _Color;
           sampler2D _Tex;
 
           float4 frag(v2f_customrendertexture IN) : COLOR
           {
                //return 0;
                //add more octaves and do in 3d space for evolving the noise
                //add noise scrolling
                return snoise(IN.localTexcoord.xy * 15 );
                return _Color * tex2D(_Tex, IN.localTexcoord.xy);
           }
           ENDCG
        }
    }
}