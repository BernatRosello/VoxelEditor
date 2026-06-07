Shader "Custom/Snow Static" {
    Properties{
        [Header(Main)]
        _Noise("Snow Noise", 2D) = "gray" {}
        _NoiseScale("Noise Scale", Range(0,2)) = 0.1
        _NoiseWeight("Noise Weight", Range(0,2)) = 0.1
        [HDR]_ShadowColor("Shadow Color", Color) = (0.5,0.5,0.5,1)
 
        [Space]
        [Header(Tesselation)]
        _MaxTessDistance("Max Tessellation Distance", Range(10,100)) = 50
        _Tess("Tessellation", Range(1,32)) = 20
 
        [Space]
        [Header(Snow)]
        [HDR]_Color("Snow Color", Color) = (0.5,0.5,0.5,1)
        _MainTex("Snow Texture", 2D) = "white" {}
        _SnowHeight("Snow Height", Range(0,2)) = 0.3
        _SnowTextureOpacity("Snow Texture Opacity", Range(0,2)) = 0.3
        _SnowTextureScale("Snow Texture Scale", Range(0,2)) = 0.3
 
        [Space]
        [Header(Sparkles)]
        _SparkleScale("Sparkle Scale", Range(0,20)) = 10
        _SparkCutoff("Sparkle Cutoff", Range(0,10)) = 0.8
        _SparkleNoise("Sparkle Noise", 2D) = "gray" {}
        _SparkleScrollScale("Sparkle Scroll Scale", Range(0,1)) = 0.05
        _SparkleScrollSpeed("Sparkle Scroll Speed", Range(0,10)) = 0.5
        _SparkleScrollNoise("Sparkle Scroll Noise", 2D) = "gray" {}
        _CameraDistanceSparkleScrollFac("Sparkle Camera Distance Scroll Factor", Range(-30,30)) = 10
        _CameraDirectionSparkleScrollFac("Sparkle Camera Direction Scroll Factor", Range(-1,1)) = 0.2
 
        [Space]
        [Header(Rim)]
        _RimPower("Rim Power", Range(0,20)) = 20
        [HDR]_RimColor("Rim Color Snow", Color) = (0.5,0.5,0.5,1)
    }
    HLSLINCLUDE
 
    // Includes
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
    #include "Assets/Shaders/SnowTrails/SnowTessellation.hlsl"
    #pragma require tessellation tessHW
    #pragma vertex TessellationVertexProgram
    #pragma hull hull
    #pragma domain domain
    // Keywords
 
    #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
    #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
    #pragma multi_compile _ _SHADOWS_SOFT
    #pragma multi_compile_fog
 
 
    ControlPoint TessellationVertexProgram(Attributes v)
    {
        ControlPoint p;
        p.vertex = v.vertex;
        p.uv = v.uv;
        p.normal = v.normal;
        return p;
    }
    ENDHLSL
 
    SubShader{
        Tags{ "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
 
        Pass{
            Tags { "LightMode" = "UniversalForward" }
 
            HLSLPROGRAM
            // vertex happens in snowtessellation.hlsl
            #pragma fragment frag
            #pragma target 4.0
 
 
            sampler2D _MainTex, _SparkleNoise, _SparkleScrollNoise;
            float4 _Color, _RimColor;
            float _RimPower;
            float _SparkleScale, _SparkCutoff, _SparkleScrollScale, _SparkleScrollSpeed, _CameraDistanceSparkleScrollFac, _CameraDirectionSparkleScrollFac;
            float _SnowTextureOpacity, _SnowTextureScale;
            float4 _ShadowColor;
 
 
            half4 frag(Varyings IN) : SV_Target{
                // worldspace Noise texture
                float3 topdownNoise = tex2D(_Noise, IN.uv * _NoiseScale).rgb;
 
                // worldspace Snow texture
                float3 snowtexture = tex2D(_MainTex, IN.uv * _SnowTextureScale).rgb;
 
                //lerp between snow color and snow texture
                float3 snowTex = lerp(_Color.rgb,snowtexture * _Color.rgb, _SnowTextureOpacity);
 
                // lighting and shadow information
                float shadow = 1;
                half4 shadowCoord = TransformWorldToShadowCoord(IN.worldPos);
 
                #if _MAIN_LIGHT_SHADOWS_CASCADE || _MAIN_LIGHT_SHADOWS
                    Light mainLight = GetMainLight(shadowCoord);
                    shadow = mainLight.shadowAttenuation;
                #else
                    Light mainLight = GetMainLight();
                #endif
 
                // extra point lights support
                float3 extraLights;
                int pixelLightCount = GetAdditionalLightsCount();
                for (int j = 0; j < pixelLightCount; ++j) {
                    Light light = GetAdditionalLight(j, IN.worldPos, half4(1, 1, 1, 1));
                    float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
                    extraLights += attenuatedLightColor;			
                }
 
                float4 litMainColors = float4(snowTex,1) ;
                extraLights *= litMainColors.rgb;
                // add in the sparkles
                float invDist = length(IN.worldPos - GetCameraPositionWS())/(length(_Position - GetCameraPositionWS())*_CameraDistanceSparkleScrollFac);
                float scrollFac = tex2D(_SparkleScrollNoise, (IN.uv  + (-GetViewForwardDir()+GetCameraPositionWS()*_CameraDirectionSparkleScrollFac)*invDist + _SinTime * _SparkleScrollSpeed) * _SparkleScrollScale).r + 0.08;
                float sparklesStatic = tex2D(_SparkleNoise, (IN.uv) * _SparkleScale).r * saturate(scrollFac + 0.65);
                float cutoffSparkles = step(_SparkCutoff,sparklesStatic);				
                litMainColors += cutoffSparkles * 4;
 
 
                // add rim light
                half rim = 1.0 - dot((IN.viewDir), TransformObjectToWorldNormal(IN.normal)) * topdownNoise.r;
                litMainColors += _RimColor * pow(abs(rim), _RimPower);
 
                // ambient and mainlight colors added
                half4 extraColors;
                extraColors.rgb = litMainColors.rgb * mainLight.color.rgb * (shadow + unity_AmbientSky.rgb);
                extraColors.a = 1;
 
                // colored shadows
                float3 coloredShadows = (shadow + (_ShadowColor.rgb * (1-shadow)));
                litMainColors.rgb = litMainColors.rgb * mainLight.color * (coloredShadows);
                // everything together
                float4 final = litMainColors+ extraColors + float4(extraLights,0);
                // add in fog
                final.rgb = MixFog(final.rgb, IN.fogFactor);
                return final;
 
            }
            ENDHLSL
 
        }
 
        // casting shadows is a little glitchy, I've turned it off, but maybe in future urp versions it works better?
        // Shadow Casting Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            Cull Off
 
            HLSLPROGRAM
            #pragma target 3.0
 
            // Support all the various light  ypes and shadow paths
            #pragma multi_compile_shadowcaster
 
            // Register our functions
 
            #pragma fragment frag
            // A custom keyword to modify logic during the shadow caster pass
 
            half4 frag(Varyings IN) : SV_Target{
                return 0;
            }
 
            ENDHLSL
        }
    }
}