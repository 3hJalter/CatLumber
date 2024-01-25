Shader "Custom/BladeGrass"
{
    Properties
    {
        [Header(Base)][Space]
        _BaseColor("Base Color", Color) = (0, 0.5, 0, 1) // Color of the lowest layer
        _TipColor("Tip Color", Color) = (0, 1, 0, 1) // Color of the highest layer
        _RandomJitterRadius("Random Jitter Radius", Float) = 0.1
        
        [Header(Wind)][Space]
        _WindTexture("Wind Texture", 2D) = "white" {}
        _WindFrequency("Wind Frequency", Float) = 1
        _WindStrength("Wind Strength", Float) = 1
        
        [Header(Trample)][Space]
        _TrampleMaxDistance("Trample Max Distance", Float) = 1
        _TrampleFalloff("Trample Falloff", Float) = 1
        _TramplePushStrength("Trample Push Strength", Float) = 1
        _TrampleSquishStrength("Trample Squish Strength", Float) = 1
        
        [Header(Shadow)][Space]
        _ShadowLightness("Shadow Lightness", Range(0, 1)) = 0
    }
    SubShader
    {
        // UniversalPipeline needed to have this render in URP
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        // Forward Lit Pass
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            // Signal this shader requires a compute buffer
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            // Lighting and shadow keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            // Register functions
            #pragma vertex Vertex
            #pragma fragment Fragment

            // Include logic file
            #include "BladeGrass.hlsl"
            
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            HLSLPROGRAM
            // Signal this shader requires a compute buffer
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #define SHADOW_CASTER_PASS

            // Register functions
            #pragma vertex Vertex
            #pragma fragment Fragment

            // Include logic file
            #include "BladeGrass.hlsl"
            
            ENDHLSL
        }
    }
}
