#ifndef FUR_SHELL_DEPTH_NORMALS_HLSL
#define FUR_SHELL_DEPTH_NORMALS_HLSL

#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"
#include "./Param.hlsl"

struct Attributes
{
    half4 positionOS : POSITION;
    half3 normalOS : NORMAL;
    half4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
    float  layer : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    float3 tangentWS : TEXCOORD3;
    float3 posWS : TEXCOORD4;
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
    float3 posWS = vertexInput.positionWS + shellDir * (_ShellStep * index);
    float4 posCS = TransformWorldToHClip(posWS);
   
    half3 objectScale = float3(
        length(unity_ObjectToWorld._m00_m10_m20),
        length(unity_ObjectToWorld._m01_m11_m21),
        length(unity_ObjectToWorld._m02_m12_m22));

    half scale = max(objectScale.x, max(objectScale.y, objectScale.z));


    output.vertex = posCS;
    output.posWS = posWS;
    output.uv = TRANSFORM_TEX(input.uv * scale.xx, _FurMap);
    output.layer = (float)index / (_ShellAmount);
    output.normalWS = normalize(shellDir);
    output.tangentWS = normalInput.tangentWS;

    stream.Append(output);
}

[maxvertexcount(63)]
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

float4 frag(Varyings input) : SV_Target
{
    float2 furUV = input.uv / _BaseMap_ST.xy * _FurScale;

    float4 furColor = SAMPLE_TEXTURE2D(_FurMap, sampler_FurMap, furUV);
    float alpha = furColor.r * (1.0 - input.layer);
    if (input.layer > 0.0 && alpha < _AlphaCutout) discard;

    float3 viewDirWS = SafeNormalize(GetCameraPositionWS() - input.posWS);
    half3 normalTS = UnpackNormalScale(
        SAMPLE_TEXTURE2D(_NormalMap, sampler_FurMap, furUV),
        _NormalScale);
    float3 bitangent = SafeNormalize(viewDirWS.y * cross(input.normalWS, input.tangentWS));
    float3 normalWS = SafeNormalize(TransformTangentToWorld(
        normalTS,
        float3x3(input.tangentWS, bitangent, input.normalWS)));

    // Output NormalWS to "_CameraNormalsTexture", no depth needed in forward renderer.
    return float4(NormalizeNormalPerPixel(normalWS), 0.0);

}

#endif
