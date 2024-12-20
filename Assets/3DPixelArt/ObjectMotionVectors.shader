Shader "Hidden/CustomObjectMotionVectors"
{
    SubShader
    {
        Pass
        {
            Name "Custom Object Motion Vectors"

            HLSLPROGRAM
            #pragma target 3.5

            #pragma vertex vert
            #pragma fragment frag

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            // -------------------------------------
            // Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

            // -------------------------------------
            // Structs
            struct Attributes
            {
                float4 position             : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS         : SV_POSITION;
                float4 previousPositionCS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };


            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);


                /*
                // This is required to avoid artifacts ("gaps" in the _MotionVectorTexture) on some platforms
                #if defined(UNITY_REVERSED_Z)
                    output.positionCS.z -= unity_MotionVectorsParams.z * output.positionCS.w;
                #else
                    output.positionCS.z += unity_MotionVectorsParams.z * output.positionCS.w;
                #endif*/

                output.positionCS = mul(_NonJitteredViewProjMatrix, mul(UNITY_MATRIX_M, input.position));

                const float4 prevPos = mul(UNITY_PREV_MATRIX_M, input.position); 
                //(THIS RESULT MUST BE STORED IN ITS OWN TEXTURE, CAN BE DONE IN SEPARATE PASS)
                // PROBLEM IS: HOW TO ACCESS THAT TEXTURE ???

                //STORE POSITION TEXTURE (PRECISE WITH SUBPIXEL DISPLACEMENTS) + MOTION VECTORS
                //WE CAN USE THEM TO ACTUALLY HAVE PRECISE MOVEMENT DETECTION


                output.previousPositionCS = mul(_PrevViewProjMatrix, mul(UNITY_PREV_MATRIX_M, prevPos));
                return output;
            }

            // -------------------------------------
            // Fragment
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                // Note: unity_MotionVectorsParams.y is 0 is forceNoMotion is enabled
                /*
                bool forceNoMotion = unity_MotionVectorsParams.y == 0.0;
                if (forceNoMotion)
                {
                    return half4(0.0, 0.0, 0.0, 0.0);
                }*/

                // Calculate positions
                float4 posCS = input.positionCS;
                float4 prevPosCS = input.previousPositionCS;

                half2 posNDC = posCS.xy * rcp(posCS.w);
                half2 prevPosNDC = prevPosCS.xy * rcp(prevPosCS.w);

                // Calculate forward velocity
                half2 velocity = (posNDC.xy - prevPosNDC.xy);
                #if UNITY_UV_STARTS_AT_TOP
                    velocity.y = -velocity.y;
                #endif

                // Convert velocity from NDC space (-1..1) to UV 0..1 space
                // Note: It doesn't mean we don't have negative values, we store negative or positive offset in UV space.
                // Note: ((posNDC * 0.5 + 0.5) - (prevPosNDC * 0.5 + 0.5)) = (velocity * 0.5)
                velocity.xy *= 0.5;

                return half4(velocity, 0, 0);
            }
            ENDHLSL
        }
    }
}
