Shader "GeodeEmpire/Crystal"
{
    Properties
    {
        _BaseColor("Surface Colour", Color) = (0.8, 0.7, 0.9, 1)
        _DeepColor("Deep Colour", Color) = (0.4, 0.2, 0.6, 1)
        _ZoneColor("Zone Colour", Color) = (0.3, 0.1, 0.5, 1)
        _RimColor("Rim Colour", Color) = (1, 1, 1, 1)
        _Smoothness("Smoothness", Range(0, 1)) = 0.93
        _Metallic("Metallic", Range(0, 1)) = 0
        _Translucency("Translucency", Range(0, 1)) = 0.6
        _RimPower("Rim Power", Range(0.5, 8)) = 3.5
        _RimStrength("Rim Strength", Range(0, 2)) = 0.6
        _Sparkle("Sparkle", Range(0, 3)) = 0.6
        _SparkleScale("Sparkle Scale", Float) = 45
        _ZoningStrength("Zoning", Range(0, 1)) = 0.4
        _Inclusions("Inclusions", Range(0, 1)) = 0.2
        _Highlight("Highlight", Range(0, 1)) = 0
        _NoiseTex("Noise", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor, _DeepColor, _ZoneColor, _RimColor;
            float _Smoothness, _Metallic, _Translucency, _RimPower, _RimStrength, _Sparkle, _SparkleScale, _ZoningStrength, _Inclusions, _Highlight;
            float4 _NoiseTex_ST;
        CBUFFER_END
        TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.color = IN.color;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 N = normalize(IN.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float ndv = saturate(dot(N, V));
                float fres = pow(1.0 - ndv, _RimPower);

                float3 surf = _BaseColor.rgb * IN.color.rgb;
                float3 deep = _DeepColor.rgb * IN.color.rgb;
                // the deep colour shows through at every angle, strongest looking straight into a face
                float3 body = lerp(surf, deep, _Translucency * (0.4 + 0.6 * ndv));
                float zone = smoothstep(0.4, 0.95, IN.color.a);
                body = lerp(body, _ZoneColor.rgb * IN.color.rgb, _ZoningStrength * zone);
                // bases sit in the crowd; tips catch the light (cheap contact shadow that grounds the carpet)
                float baseAO = lerp(0.5, 1.0, smoothstep(0.0, 0.8, IN.color.a));
                float inc = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xz * 7.0 + IN.positionOS.y * 2.3).r;
                body = lerp(body, surf * 1.05, _Inclusions * inc * 0.8);

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.positionCS = IN.positionCS;
                inputData.normalWS = N;
                inputData.viewDirectionWS = V;
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = IN.fogFactor;
                inputData.bakedGI = SampleSH(N);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData s = (SurfaceData)0;
                s.albedo = body;
                s.metallic = _Metallic;
                s.specular = half3(0, 0, 0);
                s.smoothness = _Smoothness;
                s.occlusion = baseAO;
                s.alpha = 1.0;
                s.normalTS = half3(0, 0, 1);

                Light mainLight = GetMainLight(inputData.shadowCoord);
                float3 H = normalize(mainLight.direction + V);
                float spec = pow(saturate(dot(N, H)), 32.0) * mainLight.shadowAttenuation;
                float2 spUV = (IN.positionWS.xz * 0.7 + IN.positionWS.yx * 0.45) * _SparkleScale + V.xy * 2.5 + N.xz * 1.7;
                float sp = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, spUV).g;
                sp = pow(saturate(sp), 16.0) * _Sparkle;
                float3 emis = fres * _RimColor.rgb * _RimStrength * (mainLight.color * 0.45 + 0.15) * lerp(surf, 1.0, 0.4);
                emis += sp * mainLight.color * (0.35 + spec * 1.5) * mainLight.shadowAttenuation;
                float back = pow(saturate(dot(-mainLight.direction, V) * 0.5 + 0.5), 5.0) * _Translucency * 0.4;
                emis += back * deep * mainLight.color * mainLight.shadowAttenuation;
                emis += _Highlight * float3(1.0, 0.92, 0.7) * 0.28;
                s.emission = emis;

                half4 col = UniversalFragmentPBR(inputData, s);
                col.rgb = MixFog(col.rgb, IN.fogFactor);
                col.a = 1.0;
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct A { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; };

            V ShadowVert(A IN)
            {
                V OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 ShadowFrag(V IN) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct A { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 positionCS : SV_POSITION; };

            V DepthVert(A IN)
            {
                V OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half DepthFrag(V IN) : SV_Target { return IN.positionCS.z; }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
