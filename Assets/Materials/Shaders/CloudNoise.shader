Shader "CustomRenderTexture/Simple"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _ColorRamp("Color Ramp", 2D) = "white" {}
        _Offset("Offset", Vector) = (0,0,0,0)
        _WindDirection("Wind Direction", Vector) = (1,1,1,1)
        _windSpeed("Wind Speed", Range(0.01,1)) = 1
        _Amplitude("Amplitude", Range(0,2)) = 0.5
        [IntRange] _Octaves("Octaves", Range(1,8)) = 1
        _Frequency("Frequency", Range(1,100)) = 1.0
        _Lacunarity("Lacunarity", Range(0.01,10)) = 2 
        _Persistence("Persistence", Range(0.01, 2)) = 0.5 
        _Density ("Cloud Density", Range(0,1)) = 0.9
        _DispersionSpeed ("Dispersion Speed", Range(0,1)) = 0.5 
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
           sampler2D _ColorRamp;

           float _Amplitude; 
           int _Octaves;
           float _Frequency;
           float _Lacunarity;
           float _Persistence;
           float4 _WindDirection;
           float _windSpeed;
           float _Density;
           float _DispersionSpeed;
           float4 _Offset;
 
           float4 frag(v2f_customrendertexture IN) : COLOR
           {
                //return 0;
                float time = _Time.x;

                float noiseValue = 0;
                
                float amplitude = _Amplitude;
                int octaves = _Octaves;
                float frequency = _Frequency;
                float lacunarity = _Lacunarity;
                float persistence = _Persistence;

                float2 coords = IN.localTexcoord.xy - float2(1,1) * 0.2 * _windSpeed * time + _Offset.xy;

                float maxValue = 0;
                
                //remove amplitude from parameters and fix it to one.

                for (int i = 0; i < octaves; i++) 
                {
                    maxValue += amplitude;
                    noiseValue += amplitude * snoise(float3(coords * frequency  , time * _DispersionSpeed));
                    frequency *= lacunarity;
                    amplitude *= persistence;
                }
                noiseValue /= maxValue;
                
                float cloudNoise = (( _Density + noiseValue)) ;
                //the step function causes the cloud movement to look weird. Adjust it to make the clouds smoother
                //add some smooth edges to the clouds defined by cloudNoise
                //make a color ramp for the clouds !
                return  tex2D(_ColorRamp, float2(1.0f - cloudNoise,0));
           }
           ENDCG
        }
    }
}