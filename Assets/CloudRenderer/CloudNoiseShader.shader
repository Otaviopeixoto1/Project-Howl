Shader "Custom/CloudNoise"
{
    Properties
    {
        _Offset("Offset", Vector) = (0,0,0,0)
        _Amplitude("Amplitude", Range(0,2)) = 0.5
        [IntRange] _Octaves("Octaves", Range(1,8)) = 1
        _Frequency("Frequency", Range(1,100)) = 1.0
        _Lacunarity("Lacunarity", Range(0.01,10)) = 2 
        _Persistence("Persistence", Range(0.01, 2)) = 0.5 
        _DispersionSpeed ("Dispersion Speed", Range(0,1)) = 0.5 
    }
 
    SubShader
    {
       Lighting Off
       Blend Off
 
       Pass
       {
           CGPROGRAM
           #include "UnityCustomRenderTexture.cginc"
           #include "Assets/Data/Shaders/ShaderIncludes/NoiseLibrary/ashima/classicnoise3D.hlsl"
           #include "Assets/Data/Shaders/ShaderIncludes/NoiseLibrary/stegu-psrdnoise/psrdnoise3.hlsl"
           #pragma vertex CustomRenderTextureVertexShader
           #pragma fragment frag
           #pragma target 3.0
 



           float4 _CloudDisplacement; //Displacement vector calculated on cpu to simulate cloud movement
           float _Amplitude; 
           int _Octaves;
           float _Frequency;
           float _Lacunarity;
           float _Persistence;

           float _DispersionSpeed;
           float4 _Offset;
           float4 _TexelSize;

           float4x4 _LightMatrix; 
           float4x4 _InvLightMatrix; 
 
           float4 frag(v2f_customrendertexture IN) : COLOR
           {
                float time = _Time.x;

                float noiseValue = 0;
                float2 grad = float2(0.0, 0.0);
                const float3 tileSize = float3(289,289,289);
                
                float amplitude = _Amplitude;
                int octaves = _Octaves;
                float frequency = _Frequency;
                float lacunarity = _Lacunarity;
                float persistence = _Persistence;
                
                //
                //TRANSFORM UVS HERE WITHOUT TRANSFORMING OFFSET
                //THIS IS LIGHT SPACE COORDS. THE NOISE MUST BE SAMPLED IN WORLD SPACE
                //
                float2 coords = IN.localTexcoord.xy + _Offset.xy;

                float maxValue = 0;

                //float4 lightCoord = mul(_LightMatrix, float4(coords, 0, 0) );
                //float2 snappedCoord = floor(lightCoord * _TexelSize.zw) * _TexelSize.xy;
                //float2 snappedCoord = floor(coords * _TexelSize.zw) * _TexelSize.xy;

                //Here I use the x texel size as reference for snapping the cloud dispersion but a better approach is needed
                float dispersion = floor(time * 0.1f * _DispersionSpeed * _TexelSize.z * 2.0f) * _TexelSize.x * 0.5f;
                
                for (int i = 0; i < octaves; i++) 
                {
                    maxValue += amplitude;
                    float3 g; //use grad for distance field computations. Multiply it by amplitude as well
                    //Decresing the period based on persistance might make it tile:
                    
                    noiseValue += amplitude * psrdnoise(float3(coords.xy * frequency, dispersion), tileSize, 0.0, g);

                                                                                //swap y and z 
                    //grad +=  mul(_InvLightMatrix, (amplitude / frequency) * float4(g, 0.0)).xy;
                    grad += (amplitude / frequency) * float4(g, 0.0);

                    frequency *= lacunarity;
                    amplitude *= persistence;
                }
                noiseValue /= maxValue;
                grad /= maxValue;

                //return noiseValue;
                return float4(noiseValue, grad, 0); 
                
           }
           ENDCG
        }
    }
}