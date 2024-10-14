Shader "Custom/MSStencilMask"
{
    Properties
    {

    }
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #pragma editor_sync_compilation // maybe not necessary

        ENDHLSL
        Pass
        {
            ZWrite Off 
            Cull Off 
            ZTest LEqual
            ColorMask 0

            //Main Point: IF the front is already drawn, dont draw backface. otherwise do draw them
            
            //NOT EXACTLY WHAT I WANT //THIS MAKES IT SO ONLY THINGS INSIDE THE SUFACE WILL HAVE A VALUE GREATER THAN ONE AND WILL EXCLUDE ANYTHING BEHIND OBJects
            
            //USE A DIFFERENT COMP METHOD FOR FRONT AN BACK FACES
            Stencil
            {
                Comp always 

                FailBack Keep
                ZFailBack IncrWrap
                PassBack Keep
                
                FailFront Keep
                ZFailFront DecrWrap
                PassFront Keep
            }


            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            struct MeshData
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float4 positionWS : TEXCOORD1;
                float4 positionCS : SV_POSITION;
                
            };

            
            struct Vert 
            {
                float4 position;
                float4 normal;
            };

            float4 centerOffset;
            float4 scale;
            float4 lightDir;

            StructuredBuffer<Vert> vertexBuffer;
            StructuredBuffer<uint> indexBuffer;

            Interpolators vert (MeshData v, uint instanceID : SV_InstanceID)
            {
                Interpolators o;
                
                uint vertId = v.positionOS.x + v.positionOS.y;
                uint id = indexBuffer[vertId + 2 * instanceID];
                Vert vert = vertexBuffer[id];
                
                float3 up = float3(0.0, 1.0, 0.0);
                o.positionWS = float4(vert.position.xzy * scale.xyz + centerOffset.xyz + v.positionOS.z * lightDir.xyz * 500.0f, 1.0f);
                o.positionCS = mul(UNITY_MATRIX_VP, o.positionWS);

                return o;
            }

            half4 frag (Interpolators i) : SV_Target
            {
                return half4(0,0,0,0);
            }

            ENDHLSL
        }
    }
}
