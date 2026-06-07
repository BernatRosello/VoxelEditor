#ifndef FUR_SHELL_PARAM_HLSL
#define FUR_SHELL_PARAM_HLSL

int _ShellAmount;
float _ShellStep;
float _AlphaCutout;
float _Occlusion;
float _FurScale;
float4 _BaseMove;
float4 _TouchMove;
float _TouchThreshold;
float _TouchThreshold2;
float4 _WindFreq;
float4 _WindMove;
float3 _AmbientColor;
float _FaceViewProdThresh;
int _RimCutoffLayer;
float _TranslucencyStrength;
float _TranslucencyPower;
float4 _TranslucencyTint;
int _TransCutoffLayer;

TEXTURE2D(_FurMap); 
SAMPLER(sampler_FurMap);
float4 _FurMap_ST;

TEXTURE2D(_NormalMap); 
float4 _NormalMap_ST;
float _NormalScale;

uniform float4 _TouchPosition;

#endif