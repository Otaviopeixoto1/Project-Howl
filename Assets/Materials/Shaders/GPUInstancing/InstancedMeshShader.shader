Shader "Custom/InstancedIndirect" {
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
        Tags { "RenderType" = "Opaque" }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct MeshData {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct Interpolators {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float2 auv      : TEXCOORD1;
            }; 

            struct MeshProperties {
                float4x4 mat;
                float4 offsetScale;
                float3 normal;
                float2 atlasUV;
            };

            //Buffer with instanced properties:
            StructuredBuffer<MeshProperties> _Properties;

            sampler2D _MapAtlas;
            sampler2D _SpriteAtlas;

            Interpolators vert(MeshData i, uint instanceID: SV_InstanceID) 
            {
                Interpolators o;

                float4 pos = mul(_Properties[instanceID].mat, i.vertex);
                o.vertex = UnityObjectToClipPos(pos);

                o.uv = _Properties[instanceID].offsetScale.xy + (i.uv * _Properties[instanceID].offsetScale.zw);
                o.auv = _Properties[instanceID].atlasUV;
                return o;
            }

            fixed4 frag(Interpolators i) : SV_Target 
            {
                
                float4 tex = tex2D(_SpriteAtlas, i.uv );
                clip(tex.a - 0.01);
                float4 col = tex2D(_MapAtlas, i.auv);
                
                return tex * col;
            }

            ENDCG
        }
    }
}
