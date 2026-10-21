// MoleTown 三渲二着色器（URP 17 / Unity 6）
// 色阶光照 + 投影色阶化 + 硬边高光 + 屏幕空间深度边缘光 + 自发光 + 外扩描边
// 支持 Forward 与 Forward+（Cluster Light Loop）两种渲染路径
Shader "MoleTown/Toon"
{
    Properties
    {
        [Header(Base)]
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor ("Base Color", Color) = (1,1,1,1)

        [Header(Cel Shading)]
        _ShadowColor ("Shadow Tint (乘在固有色上)", Color) = (0.72, 0.68, 0.86, 1)
        _ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.5
        _ShadowSmooth ("Shadow Smooth", Range(0.001, 0.5)) = 0.02
        _ReceiveShadowStrength ("Cast Shadow Strength", Range(0, 1)) = 1
        [Toggle(_USE_RAMP)] _UseRamp ("Use Ramp Map", Float) = 0
        _RampMap ("Ramp Map (横向渐变)", 2D) = "white" {}
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.3

        [Header(Specular)]
        [HDR] _SpecColor ("Specular Color", Color) = (1,1,1,1)
        _SpecSize ("Specular Size", Range(0, 0.2)) = 0.008

        [Header(Rim Light)]
        [Toggle(_DEPTH_RIM)] _DepthRim ("Screen Space Depth Rim (需要深度预渲染)", Float) = 1
        [HDR] _RimColor ("Rim Color (A=强度)", Color) = (1,1,1,0.25)
        _RimWidth ("Depth Rim Width (像素, 1080p)", Range(0, 12)) = 4
        _RimThreshold ("Depth Rim Threshold (米)", Range(0.01, 2)) = 0.3
        _RimMin ("Fresnel Rim Min", Range(0, 1)) = 0.8
        _RimMax ("Fresnel Rim Max", Range(0, 1)) = 0.82
        _RimShadowStrength ("Rim In Shadow", Range(0, 1)) = 0.25

        [Header(Emission)]
        [HDR] _EmissionColor ("Emission", Color) = (0,0,0,1)

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0.15, 0.1, 0.1, 1)
        _OutlineWidth ("Outline Width", Range(0, 1)) = 0.3
        _OutlineMaxDistance ("Outline Full Width Distance", Range(1, 100)) = 14
        [Toggle] _UseSmoothNormal ("Use Smooth Normal (存于 Tangent)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_RampMap); SAMPLER(sampler_RampMap);

        // 所有 Pass 共用同一个 CBUFFER，保证 SRP Batcher 兼容
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half4  _ShadowColor;
            half   _ShadowThreshold;
            half   _ShadowSmooth;
            half   _ReceiveShadowStrength;
            half   _AmbientStrength;
            half4  _SpecColor;
            half   _SpecSize;
            half4  _RimColor;
            float  _RimWidth;
            float  _RimThreshold;
            half   _RimMin;
            half   _RimMax;
            half   _RimShadowStrength;
            half4  _EmissionColor;
            half4  _OutlineColor;
            float  _OutlineWidth;
            float  _OutlineMaxDistance;
            float  _UseSmoothNormal;
        CBUFFER_END
        ENDHLSL

        // ---------------- 主光照 Pass ----------------
        Pass
        {
            Name "ToonForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _USE_RAMP
            #pragma shader_feature_local_fragment _DEPTH_RIM
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #if defined(_DEPTH_RIM)
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float  eyeDepth   : TEXCOORD3;
                half   fogCoord   : TEXCOORD4;
            };

            Varyings vert (Attributes input)
            {
                Varyings o;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                o.uv         = TRANSFORM_TEX(input.uv, _BaseMap);
                o.eyeDepth   = -pos.positionVS.z;
                o.fogCoord   = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            // 把 0~1 的光照值映射成色阶：Ramp 贴图或两色硬边
            half3 CelShade(half3 albedo, half lightValue)
            {
            #if defined(_USE_RAMP)
                return albedo * SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, float2(lightValue, 0.5)).rgb;
            #else
                return lerp(albedo * _ShadowColor.rgb, albedo, lightValue);
            #endif
            }

            half CelStep(half value)
            {
                return smoothstep(_ShadowThreshold - _ShadowSmooth, _ShadowThreshold + _ShadowSmooth, value);
            }

            // 附加光源同样做色阶化，避免出现写实的柔和过渡
            half3 ToonAdditionalLight(Light light, float3 N, half3 albedo)
            {
                half lit = CelStep(dot(N, light.direction) * 0.5 + 0.5);
                half atten = saturate(light.distanceAttenuation * light.shadowAttenuation);
                return albedo * light.color * (lit * atten);
            }

            // 屏幕空间深度边缘光：沿法线方向偏移采样深度，后面是远处背景的像素就是轮廓边缘
            // 比菲涅尔边缘光稳定，平面在掠射角也不会整面发白
            half RimMask(Varyings i, float3 N, float3 V)
            {
            #if defined(_DEPTH_RIM)
                float2 screenUV = GetNormalizedScreenSpaceUV(i.positionCS);
                float3 normalVS = TransformWorldToViewNormal(N, true);
                float pixels = _RimWidth * _ScaledScreenParams.y / 1080.0;
                float2 offsetUV = normalVS.xy * pixels / _ScaledScreenParams.xy;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV + offsetUV), _ZBufferParams);
                return step(_RimThreshold, sceneDepth - i.eyeDepth);
            #else
                return smoothstep(_RimMin, _RimMax, 1.0 - saturate(dot(N, V)));
            #endif
            }

            half4 frag (Varyings i) : SV_Target
            {
                half3 albedo = (SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor).rgb;
                float3 N = normalize(i.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);

                // 主光源：先把 N·L 色阶化，再把投影也压成硬边，两者取暗
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half halfLambert = dot(N, mainLight.direction) * 0.5 + 0.5;
                half castShadow = lerp(1.0, smoothstep(0.5 - _ShadowSmooth, 0.5 + _ShadowSmooth, mainLight.shadowAttenuation), _ReceiveShadowStrength);
            #if defined(_USE_RAMP)
                half lit = halfLambert * castShadow;
                half3 color = CelShade(albedo, lit) * mainLight.color;
            #else
                half lit = CelStep(halfLambert) * castShadow;
                half3 color = CelShade(albedo, lit) * mainLight.color;
            #endif

                // 硬边高光，只出现在亮部
                float3 H = normalize(mainLight.direction + V);
                half spec = step(1.0 - _SpecSize, saturate(dot(N, H))) * step(0.5, lit);
                color += spec * _SpecColor.rgb * mainLight.color;

            #if defined(_ADDITIONAL_LIGHTS)
                InputData inputData = (InputData)0;
                inputData.positionWS = i.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
                uint lightCount = GetAdditionalLightsCount();
                #if USE_CLUSTER_LIGHT_LOOP
                [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                {
                    color += ToonAdditionalLight(GetAdditionalLight(lightIndex, i.positionWS, half4(1, 1, 1, 1)), N, albedo);
                }
                #endif
                LIGHT_LOOP_BEGIN(lightCount)
                    color += ToonAdditionalLight(GetAdditionalLight(lightIndex, i.positionWS, half4(1, 1, 1, 1)), N, albedo);
                LIGHT_LOOP_END
            #endif

                // 环境光（球谐）
                color += albedo * SampleSH(N) * _AmbientStrength;

                // 边缘光：亮部满强度，暗部保留一点
                half rim = RimMask(i, N, V) * lerp(_RimShadowStrength, 1.0, lit);
                color += rim * _RimColor.rgb * _RimColor.a;

                color += _EmissionColor.rgb;
                color = MixFog(color, i.fogCoord);
                return half4(color, 1);
            }
            ENDHLSL
        }

        // ---------------- 描边 Pass（背面外扩） ----------------
        // URP 会在 UniversalForward 之后自动绘制 SRPDefaultUnlit 这个 LightMode
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half   fogCoord   : TEXCOORD0;
            };

            Varyings vert (Attributes input)
            {
                Varyings o;
                // 硬边模型直接用法线外扩会断开，可把平滑法线烘进 tangent（见 SmoothNormalUtility）
                float3 normalOS = lerp(input.normalOS, input.tangentOS.xyz, _UseSmoothNormal);

                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalCS = TransformWorldToHClipDir(TransformObjectToWorldNormal(normalOS));

                // 近处描边保持固定像素宽度；超过 _OutlineMaxDistance 后随透视变细，远景不会糊成一团
                float widthScale = min(positionCS.w, _OutlineMaxDistance);
                float2 offset = normalize(normalCS.xy + 1e-5) * (_OutlineWidth * 0.01) * widthScale;
                offset.x *= _ScreenParams.y / _ScreenParams.x; // 修正宽高比
                positionCS.xy += offset;

                o.positionCS = positionCS;
                o.fogCoord = ComputeFogFactor(positionCS.z);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return half4(MixFog(_OutlineColor.rgb, i.fogCoord), 1);
            }
            ENDHLSL
        }

        // ---------------- 投射阴影 ----------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            float4 vert (Attributes input) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDir = normalize(_LightPosition - positionWS);
            #else
                float3 lightDir = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            half4 frag () : SV_Target { return 0; }
            ENDHLSL
        }

        // ---------------- 深度 Pass（深度预渲染 / 深度边缘光需要） ----------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 vert (float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half frag () : SV_Target { return 0; }
            ENDHLSL
        }

        // ---------------- 深度法线 Pass（SSAO 等屏幕空间效果需要） ----------------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
            };

            Varyings vert (Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(i.normalWS), 0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
