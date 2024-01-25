Shader "Custom/Lit"
{
    Properties
    {
        [Header(Surface Options)]
        [Space]
        [MainTexture]
        _MainTex ("Main Texture", 2D) = "white" {}
        [MainColor]
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _AlphaCutoff ("Alpha Cutoff Threshold", Range(0, 1)) = 0.5
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Metallic ("Metallic", Range(0, 1)) = 0.5

        [HideInInspector]
        _SourceBlend ("Source Blend", Float) = 0
        [HideInInspector]
        _DestBlend ("Destination Blend", Float) = 0
        [HideInInspector]
        _ZWrite ("ZWrite", Float) = 0

        [HideInInspector]
        _SurfaceType ("Surface Type", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_ST;
            float4 _BaseColor;

            float _AlphaCutoff;
            float _Smoothness;
            float _Metallic;


        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend [_SourceBlend] [_DestBlend]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #define _SPECULAR_COLOR

            #pragma shader_feature_local _ALPHA_CUTOUT

            #if UNITY_VERSION >= 202120
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #else
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _MAIN_LIGHT_SHADOWS_SCREEN
            #endif
            // #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            // #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            // #pragma multi_compile _ LIGHTMAP_ON
            // #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            // #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            // #pragma multi_compile _ SHADOWS_SHADOWMASK
            // #pragma multi_compile _ _SCREEN_SPACE_OCCLUSION
            //
            // #pragma multi_compile_fog
            // #pragma multi_compile_instancing

            #pragma vertex Vertex
            #pragma fragment Fragment

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                float4 lightMapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vertex(Attributes input)
            {
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                Varyings output = (Varyings)0;
                output.positionCS = posInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;

                // OUTPUT_LIGHTMAP_UV(i.lightMapUV, unity_LightmapST, o.lightMapUV);
                // OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

                return output;
            }

            float4 Fragment(Varyings input) : SV_Target
            {
                float4 baseTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                #ifdef _ALPHA_CUTOUT
                clip(baseTex.a * _BaseColor.a - _AlphaCutoff);
                #endif

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                // inputData.bakedGI = SAMPLE_GI(i.lightMapUV, input.vertexSH, input.normalWS);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = baseTex.rgb * _BaseColor.rgb;
                surfaceData.alpha = baseTex.a * _BaseColor.a;
                surfaceData.specular = 1;
                surfaceData.smoothness = _Smoothness;
                surfaceData.metallic = _Metallic;

                #if UNITY_VERSION >= 202120
                return UniversalFragmentBlinnPhong(inputData, surfaceData);
                #else
                return UniversalFragmentBlinnPhong(inputData, surfaceData.albedo, float4(surfaceData.specular, 1),
                                                       surfaceData.smoothness, surfaceData.emission, surfaceData.alpha,
                                                       surfaceData.normalTS);
                #endif
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ColorMask 0

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Interpolators
            {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            float4 GetShadowCasterPositionCS(float3 positionWS, float3 normalWS)
            {
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Interpolators Vertex(Attributes input)
            {
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                Interpolators output = (Interpolators)0;
                output.positionCS = GetShadowCasterPositionCS(posInputs.positionWS, normInputs.normalWS);

                return output;
            }

            float4 Fragment(Interpolators input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    CustomEditor "VinhLB.CustomLitShaderGUI"
}