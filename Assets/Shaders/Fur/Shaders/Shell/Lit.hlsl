#ifndef FUR_SHELL_LIT_HLSL
#define FUR_SHELL_LIT_HLSL

#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "./Param.hlsl"
#include "../Common/Common.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    half3 normalOS : NORMAL;
    half4 tangentOS : TANGENT;
    float2 texcoord : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    // half4 color : COLOR; // Alpha value will be discarded
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    half3 normalWS : TEXCOORD1;
    half4 tangentWS : TEXCOORD2;
    float2 uv : TEXCOORD4;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
    half4 fogFactorAndVertexLight : TEXCOORD6; // x: fogFactor, yzw: vertex light
    // half4 colorAndLayer : COLOR; // Layer is packed into the alpha channel
    half layer : TEXCOORD7;
};

Attributes vert(Attributes input)
{
    return input;
}

void AppendShellVertex(inout TriangleStream<Varyings> stream, Attributes input, int index)
{
    Varyings output = (Varyings)0;

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    float moveFactor = pow(abs((float)index / _ShellAmount), _BaseMove.w);
    half3 windAngle = _Time.w * _WindFreq.xyz;
    half3 windMove = moveFactor * _WindMove.xyz * sin(windAngle + vertexInput.positionWS * _WindMove.w);
    half3 move = moveFactor * _BaseMove.xyz;
    half3 touchMove = vertexInput.positionWS - _TouchPosition;
    touchMove = SafeNormalize(saturate((_TouchThreshold - length(touchMove))) * touchMove);
    touchMove = touchMove * saturate(_TouchThreshold2 - dot(touchMove, normalInput.normalWS)) * _TouchMove;
    half3 shellDir = move + windMove + touchMove;
    shellDir = shellDir * saturate(dot(shellDir+normalInput.normalWS, normalInput.normalWS));
    shellDir = SafeNormalize(normalInput.normalWS * max(1,length(shellDir)) + shellDir);
    float3 viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;
    
    half3 objectScale = float3(
        length(unity_ObjectToWorld._m00_m10_m20),
        length(unity_ObjectToWorld._m01_m11_m21),
        length(unity_ObjectToWorld._m02_m12_m22));

    half scale = max(objectScale.x, max(objectScale.y, objectScale.z));

    output.positionWS = vertexInput.positionWS + shellDir * (_ShellStep * index);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.uv = TRANSFORM_TEX(input.texcoord * scale.xx, _BaseMap);
    output.normalWS = normalize(shellDir);

    output.tangentWS.xyz = normalInput.tangentWS;
    output.tangentWS.w = input.tangentOS.w;
    // output.colorAndLayer = input.color; //Color
    // output.colorAndLayer.a = (float)index / (_ShellAmount); //Layer
    output.layer = (float)index / (_ShellAmount); 

    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    output.fogFactorAndVertexLight = float4(fogFactor, vertexLight);

    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    stream.Append(output);
}

[maxvertexcount(42)]
void geom(triangle Attributes input[3], inout TriangleStream<Varyings> stream)
{
    [loop] for (float i = 0; i < _ShellAmount; ++i)
    {
        [unroll] for (float j = 0; j < 3; ++j)
        {
            AppendShellVertex(stream, input[j], i);
        }
        stream.RestartStrip();
    }
}

inline float3 TransformHClipToWorld(float4 positionCS)
{
    return mul(UNITY_MATRIX_I_VP, positionCS).xyz;
}

float4 frag(Varyings input) : SV_Target
{
    float2 furUv = input.uv * (_FurScale);
    float4 furColor = SAMPLE_TEXTURE2D(_FurMap, sampler_FurMap, furUv);

    // return furColor;
    float alpha = furColor.r * (1.0 - input.layer);
    if (input.layer > 0.0 && alpha < _AlphaCutout) discard;

    float3 viewDirWS = SafeNormalize(GetCameraPositionWS() - input.positionWS);
    half normScale = input.layer == 0 ? 0 : _NormalScale;
    float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_FurMap, furUv), normScale);
    float3 bitangent = cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w;   
    float3 normalWS = SafeNormalize(TransformTangentToWorld(normalTS, float3x3(input.tangentWS.xyz, bitangent, input.normalWS)));
    SurfaceData surfaceData = (SurfaceData)0;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);
    // surfaceData.albedo *= input.colorAndLayer.rgb; // Vertex Color tint
    surfaceData.occlusion = lerp(1.0 - _Occlusion, 1.0, input.layer);
    surfaceData.albedo *= surfaceData.occlusion;
    surfaceData.alpha = 0.1;

    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = viewDirWS;
#if (defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)) && !defined(_RECEIVE_SHADOWS_OFF)
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
    inputData.fogCoord = input.fogFactorAndVertexLight.x;
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    

    if (input.layer == 0)
    {
        surfaceData.metallic = 0;
        surfaceData.smoothness = 0;
        surfaceData.alpha = 1;
    }
    // To avoid the underlying shells to sheen through which looks wierd and "plasticky"
    float maxLayer = (_ShellAmount - 1.0) / _ShellAmount;
    float rimCutoffLayer = _RimCutoffLayer / (float)_ShellAmount;
    float rimFac;
    if (rimCutoffLayer >= maxLayer)
        rimFac = 0;
    else
        rimFac = saturate(
            (input.layer - rimCutoffLayer) /
            (maxLayer - rimCutoffLayer)
        );

    float transCutoffLayer = _TransCutoffLayer / (float)_ShellAmount;
    float transFac;
    if (transCutoffLayer >= maxLayer)
        transFac = 0;
    else
        transFac = saturate(
            (input.layer - transCutoffLayer) /
            (maxLayer - transCutoffLayer)
        );

    float3 translucency = 0;
    Light mainLight = GetMainLight(inputData.shadowCoord);

    translucency += CalculateTranslucency(
        mainLight,
        normalWS,
        viewDirWS,
        surfaceData.albedo,
        input.layer,
        transFac);

#ifdef _ADDITIONAL_LIGHTS
    uint lightCount = GetAdditionalLightsCount();

    LIGHT_LOOP_BEGIN(lightCount)

        Light light = GetAdditionalLight(
            lightIndex,
            inputData.positionWS);

        translucency += CalculateTranslucency(
            light,
            normalWS,
            viewDirWS,
            surfaceData.albedo,
            input.layer,
            transFac * 0.75);

    LIGHT_LOOP_END
#endif
    // half4 test = half4(0,0,0,1);
    // return test + translucency.xyzx;
    return UniversalFragmentPBR(inputData, surfaceData);
    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    ApplyRimLight(color.rgb, input.positionWS, viewDirWS, normalWS, rimFac);
    color.rgb += _AmbientColor * color.rgb;
    color.rgb += translucency;
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    // color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));

    return color;
}

#endif
