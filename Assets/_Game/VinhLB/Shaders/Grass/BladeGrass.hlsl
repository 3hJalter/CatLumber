// Make sure this file is not included twice
#ifndef BLADE_GRASS_INCLUDED
#define BLADE_GRASS_INCLUDED

// Include some helper functions
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

#include "NMGBladeGrassGraphicsHelpers.hlsl"
#include "GrassTrample.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    float3 bladeAnchorOS : TEXCOORD1;
    float3 shadowCastNormalOS : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv : TEXCOORD0; // The height of this vertex on the grass blade
    float3 positionWS : TEXCOORD1; // Position in world space
    float3 normalWS : TEXCOORD2; // Normal vector in world space

    float4 positionCS : SV_POSITION; // Normal in clip space
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// Properties
CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _TipColor;
    float _RandomJitterRadius;

    TEXTURE2D(_WindTexture);
    SAMPLER(sampler_WindTexture);
    float4 _WindTexture_ST;
    float _WindFrequency;
    float _WindStrength;

    float _TrampleMaxDistance;
    float _TrampleFalloff;
    float _TramplePushStrength;
    float _TrampleSquishStrength;

    float _ShadowLightness;
CBUFFER_END

// Vertex functions

Varyings Vertex(Attributes input)
{
    // Initialize the output struct
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    float3 bladeAnchorWS = GetVertexPositionInputs(input.bladeAnchorOS).positionWS;
    // Get a plane perpendicular to the normal
    float3 normalWS = GetVertexNormalInputs(input.normalOS).normalWS;
    float3 tangentWS, bitangentWS;
    GetPerpendicularPlane(normalWS, tangentWS, bitangentWS);
    // Calculate a random jitter amount based on world space position. Use the blade anchor 
    // so the entire blade has the same jitter offset
    float3 randomOffset = tangentWS * randNegative1to1(bladeAnchorWS, 0) + bitangentWS * randNegative1to1(
        bladeAnchorWS, 1);
    randomOffset *= _RandomJitterRadius;
    // Apply jittery to the anchor for wind
    bladeAnchorWS += randomOffset;

    // Calculate the wind axis, which also encodes the wind strength
    // The windUV is affected by the world position and time. TRANSFORM_TEX applies _WindTexture_ST values
    float2 windUV = TRANSFORM_TEX(bladeAnchorWS.xz, _WindTexture) * _Time.y * _WindFrequency;
    // Sample the wind noise texture and remap it to the range between -1 and 1
    float2 windNoise = SAMPLE_TEXTURE2D_LOD(_WindTexture, sampler_WindTexture, windUV, 0).xy * 2 - 1;
    // Offset blade points in a vector perpendicular to it's normal, but also consistent across blades
    float3 windOffset = cross(normalWS, float3(windNoise.x, 0, windNoise.y));
    // Then scale by the amplitude andUV.y so points near the base of the blade are blown less
    //windOffset *= _WindStrength * input.uv.y;
    //
    //float3 positionWS = GetVertexPositionInputs(input.positionOS).positionWS + randomOffset + windOffset;

    float3 positionWS = GetVertexPositionInputs(input.positionOS).positionWS;
    float3 offset;
    float windMultiplier;
    CalculateTrample_float(positionWS, _TrampleMaxDistance, _TrampleFalloff,
                           _TramplePushStrength, _TrampleSquishStrength, offset, windMultiplier);

    windOffset *= _WindStrength * windMultiplier;
    windOffset += offset;
    windOffset *= input.uv.y;

    positionWS += randomOffset + windOffset;

    output.positionWS = positionWS;
    output.normalWS = normalWS;
    output.uv = input.uv;
    output.positionCS = CalculatePositionCSWithShadowCasterLogic(positionWS,
                                                                 GetVertexNormalInputs(input.shadowCastNormalOS).
                                                                 normalWS);

    return output;
}

// Fragment functions

half4 Fragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);

    #ifdef SHADOW_CASTER_PASS
    return 0;
    #else
    // Gather some data for the lighting algorithm
    InputData lightingInput = (InputData)0;
    lightingInput.positionWS = input.positionWS;
    lightingInput.normalWS = input.normalWS; // No need to normalize, triangles share a normal
    lightingInput.viewDirectionWS = GetViewDirectionFromPosition(input.positionWS);
    lightingInput.shadowCoord = CalculateShadowCoord(input.positionWS, input.positionCS);

    // Lerp between the base an tip color based on the blade height
    float colorLerp = input.uv.y;
    float3 albedo = lerp(_BaseColor.rgb, _TipColor.rgb, colorLerp);
    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.albedo = albedo * (1 - _ShadowLightness);
    surfaceData.specular = 1;
    surfaceData.smoothness = 0;
    surfaceData.emission = albedo * _ShadowLightness;
    surfaceData.alpha = 1;

    // The URP simple lit algorithm
    // The arguments are lighting input data, albedo color, specular color, smoothness, emission color and alpha
    return UniversalFragmentBlinnPhong(lightingInput, surfaceData);
    #endif
}

#endif
