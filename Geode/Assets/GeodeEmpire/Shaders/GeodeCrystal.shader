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
        _Dust("Dust", Range(0, 1)) = 0
        _NoiseTex("Noise", 2D) = "gray" {}
        // V6 §20 mineral identity: reflectance, clarity, phantom banding, prism striation, inclusions, a wet film
        _F0("Reflectance F0", Range(0.02, 0.12)) = 0.04
        _Clarity("Clarity", Range(0, 1)) = 0.7
        _ZoneBands("Phantom Bands", Float) = 0
        _Striation("Striation", Range(0, 1)) = 0
        _InclusionColor("Inclusion Colour", Color) = (0.9, 0.88, 0.85, 1)
        _Wet("Wetness", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor, _DeepColor, _ZoneColor, _RimColor;
            float _Smoothness, _Metallic, _Translucency, _RimPower, _RimStrength, _Sparkle, _SparkleScale, _ZoningStrength, _Inclusions, _Highlight, _Dust;
            float4 _NoiseTex_ST;
            float _F0, _Clarity, _ZoneBands, _Striation, _Wet;
            float4 _InclusionColor;
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

            // specular workflow: F0 comes from the mineral (quartz 0.04, garnet 0.08, metals their own colour), not a fixed 4%
            #define _SPECULAR_SETUP 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;      // archetype-local (x, z): prism striations run with it
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
                float2 uv : TEXCOORD5;
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
                OUT.uv = IN.uv;
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
                // a crystal is a body, not a painted surface: looking into a face you see the deep colour, the paler
                // surface tint only survives at grazing angles; non-metallic minerals sit darker so the specular
                // highlights and the reflection probe carry the glassiness
                // an opaque render of a translucent crystal has to carry its colour in the body: too dark and a cluster
                // reads as black glass, so the surface tint stays in the mix and the body is only lightly darkened
                float3 body = lerp(deep, surf, saturate(fres * 0.7 + 0.28 + (1.0 - _Translucency) * 0.22));
                body *= lerp(0.82, 1.0, _Metallic);
                // clarity: a cloudy crystal is milky and scatters light near the surface; a clear one carries its colour deep
                float milkLum = dot(surf, float3(0.3, 0.59, 0.11));
                float3 milky = lerp(surf, float3(milkLum, milkLum, milkLum) * 0.5 + 0.45, 0.6);
                body = lerp(milky, body, saturate(_Clarity * 0.85 + 0.15));
                float zone = smoothstep(0.4, 0.95, IN.color.a);
                body = lerp(body, _ZoneColor.rgb * IN.color.rgb, _ZoningStrength * zone);
                // colour concentrates toward the tip; the base runs pale and milky (amethyst, citrine and fluorite all do)
                float baseFade = (1.0 - smoothstep(0.15, 0.7, IN.color.a)) * _ZoningStrength * (1.0 - _Metallic);
                body = lerp(body, milky * 1.05, baseFade * 0.7);
                // phantom / colour bands along the growth axis (fluorite, rhodochrosite, malachite, tourmaline): each
                // crystal's bands sit at their own phase, taken from where it stands
                if (_ZoneBands > 0.5)
                {
                    float phase = frac(dot(floor(IN.positionOS * 13.0), float3(0.31, 0.57, 0.19)));
                    float bandT = sin((IN.color.a + phase) * _ZoneBands * 6.2832);
                    float bands = smoothstep(0.25, 0.75, bandT * 0.5 + 0.5);
                    body = lerp(body, _ZoneColor.rgb * IN.color.rgb * 0.9, _ZoningStrength * bands * 0.8);
                }
                // bases sit in the crowd; tips catch the light (cheap contact shadow that grounds the carpet)
                float baseAO = lerp(0.5, 1.0, smoothstep(0.0, 0.8, IN.color.a));
                float inc = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xz * 7.0 + IN.positionOS.y * 2.3).r;
                float incMask = _Inclusions * smoothstep(0.35, 0.8, inc);
                body = lerp(body, _InclusionColor.rgb * lerp(0.8, 1.05, inc), incMask * 0.85);
                // prism striations: fine lines along the growth axis break the highlight the way a real quartz prism does
                float stria = 1.0;
                if (_Striation > 0.001)
                {
                    float sl = sin(IN.uv.x * 140.0 + IN.uv.y * 9.0) * 0.5 + 0.5;
                    stria = lerp(1.0, 0.55 + 0.45 * smoothstep(0.35, 0.65, sl), _Striation);
                }
                // rock dust from the break sits in the cavity until it is rinsed: a grey matte film, patchy with the noise
                float dustN = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xy * 9.0 + IN.positionOS.z * 4.0).r;
                float dust = saturate(_Dust) * saturate(0.35 + 0.65 * dustN);
                float luma = dot(body, float3(0.3, 0.59, 0.11));
                body = lerp(body, float3(luma, luma, luma) * 0.5 + float3(0.3, 0.285, 0.26), dust * 0.55);

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

                float wet = saturate(_Wet);
                SurfaceData s = (SurfaceData)0;
                s.albedo = body * (1.0 - _Metallic);
                s.metallic = 0.0;
                s.specular = lerp(_F0.xxx, surf * lerp(0.9, 1.0, _Clarity), _Metallic);
                float smoothV = _Smoothness * (1.0 - 0.55 * dust) * lerp(0.93, 1.0, stria) * lerp(0.9, 1.0, _Clarity);
                s.smoothness = lerp(smoothV, max(smoothV, 0.9), wet * 0.7);
                s.occlusion = baseAO;
                s.alpha = 1.0;
                s.normalTS = half3(0, 0, 1);

                Light mainLight = GetMainLight(inputData.shadowCoord);
                float3 H = normalize(mainLight.direction + V);
                float spec = pow(saturate(dot(N, H)), 32.0) * mainLight.shadowAttenuation;
                float2 spUV = (IN.positionWS.xz * 0.7 + IN.positionWS.yx * 0.45) * _SparkleScale + V.xy * 2.5 + N.xz * 1.7;
                // glints from the smooth noise channel (the per-texel channel streaks under anisotropic filtering)
                float sp = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, spUV).a;
                sp = pow(saturate(sp * 1.15), 14.0) * _Sparkle;
                float3 emis = fres * _RimColor.rgb * _RimStrength * (mainLight.color * 0.45 + 0.15) * lerp(surf, 1.0, 0.4);
                emis += sp * mainLight.color * (0.35 + spec * 1.5) * mainLight.shadowAttenuation * stria;
                // transmission: light through the body toward the eye, strongest where the crystal is thin (the tips),
                // in the deep colour, plus a little sky fill from behind; a cloudy crystal glows instead of passing light
                float thick = lerp(1.0, 0.3, IN.color.a);
                float transT = pow(saturate(dot(V, -(mainLight.direction + N * 0.35))), 3.5) * mainLight.shadowAttenuation;
                float transAmt = _Translucency * (1.0 - 0.35 * dust) * lerp(0.45, 1.0, _Clarity);
                float3 transCol = lerp(deep, surf, 1.0 - _Clarity) * mainLight.color;
                emis += transCol * transAmt * (transT * 0.9 + 0.12 * thick) * (1.0 - _Metallic);
                emis += SampleSH(-N) * deep * transAmt * 0.18 * (1.0 - _Metallic);
                emis += SampleSH(N) * lerp(deep, surf, 0.5) * transAmt * 0.12 * (1.0 - _Metallic);   // ambient scattered inside the body
                float back = pow(saturate(dot(-mainLight.direction, V) * 0.5 + 0.5), 5.0) * _Translucency * 0.15;
                emis += back * deep * mainLight.color * mainLight.shadowAttenuation;
                emis *= 1.0 - 0.7 * dust;
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

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DNVert
            #pragma fragment DNFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_fragment _ _WRITE_RENDERING_LAYERS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

            // the geometry's own smooth normal: SSAO and any other normals-texture reader see the rock as it is, not
            // a normal reconstructed from a displaced depth buffer (which streaks over the knobs and pits)
            struct DNA { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct DNV { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };

            DNV DNVert(DNA IN)
            {
                DNV OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = NormalizeNormalPerVertex(TransformObjectToWorldNormal(IN.normalOS));
                return OUT;
            }

            void DNFrag(DNV IN, out half4 outNormalWS : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(IN);
            #if defined(_GBUFFER_NORMALS_OCT)
                float3 normalWS = normalize(IN.normalWS);
                float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
                outNormalWS = half4(packedNormalWS, 0.0);
            #else
                outNormalWS = half4(NormalizeNormalPerPixel(IN.normalWS), 0.0);
            #endif
            #ifdef _WRITE_RENDERING_LAYERS
                outRenderingLayers = EncodeMeshRenderingLayer();
            #endif
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
