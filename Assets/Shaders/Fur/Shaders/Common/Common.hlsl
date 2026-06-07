#ifndef FUR_COMMON_HLSL
#define FUR_COMMON_HLSL

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float3 _LightDirection;
float _ShadowExtraBias;

inline float3 GetViewDirectionOS(float3 posOS)
{
    float3 cameraOS = TransformWorldToObject(GetCameraPositionWS());
    return normalize(posOS - cameraOS);
}

inline float3 CustomApplyShadowBias(float3 positionWS, float3 normalWS)
{
    positionWS += _LightDirection * (_ShadowBias.x + _ShadowExtraBias);
    float invNdotL = 1.0 - saturate(dot(_LightDirection, normalWS));
    float scale = invNdotL * _ShadowBias.y;
    positionWS += normalWS * scale.xxx;

    return positionWS;
}

inline float4 GetShadowPositionHClip(float3 positionWS, float3 normalWS)
{
    positionWS = CustomApplyShadowBias(positionWS, normalWS);
    float4 positionCS = TransformWorldToHClip(positionWS);
    #if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #else
    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
    #endif
    return positionCS;
}

float _RimLightPower;
float _RimLightIntensity;

void ApplyRimLight(inout float3 color, float3 posWS, float3 viewDirWS, float3 normalWS, float rimFac)
{
    if (rimFac <= 0.0)
        return;

    Light mainLight = GetMainLight();

    float lightDirDotView = dot(mainLight.direction, viewDirWS);
    float backscatter = pow(saturate( -lightDirDotView), _RimLightPower);

    float NdotV = abs(dot(normalize(viewDirWS), normalize(normalWS)));
    float fresnel = pow(saturate(1.0 - NdotV), _RimLightPower / 2.0);

    float shadow = 1.0;
#if (defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)) && !defined(_RECEIVE_SHADOWS_OFF)
    float4 shadowCoord = TransformWorldToShadowCoord(posWS);
    shadow = MainLightRealtimeShadow(shadowCoord);
#endif

    float intensity = _RimLightIntensity * (backscatter + fresnel * 0.25) * shadow;

    color += mainLight.color * intensity * rimFac;
}
inline float rand(float2 seed)
{
    return frac(sin(dot(seed.xy, float2(12.9898, 78.233))) * 43758.5453);
}

inline float3 rand3(float2 seed)
{
    return 2.0 * (float3(rand(seed * 1), rand(seed * 2), rand(seed * 3)) - 0.5);
}

struct FurMoverData
{
    float3 posWS;
    float3 dPosWS;
    float3 velocityWS;
    float time;
};

RWStructuredBuffer<FurMoverData> _Buffer : register(u1);

#endif