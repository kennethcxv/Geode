Shader "GeodeEmpire/GeodeShell"
{
    Properties
    {
        _RockColor("Rock Colour", Color) = (0.5, 0.46, 0.42, 1)
        _RockColor2("Rock Colour Dark", Color) = (0.3, 0.27, 0.25, 1)
        _CavityColor("Cavity Wall", Color) = (0.7, 0.68, 0.64, 1)
        _RimColor("Fracture Face", Color) = (0.6, 0.57, 0.53, 1)
        _BandA("Band A", Color) = (0.85, 0.8, 0.75, 1)
        _BandB("Band B", Color) = (0.5, 0.42, 0.36, 1)
        _BandStrength("Band Strength", Range(0, 1)) = 0.5
        _BandFrequency("Band Frequency", Float) = 12
        _BandOffset("Band Offset", Float) = 0
        _HintColor("Exterior Hint", Color) = (0.7, 0.5, 0.9, 1)
        _HintAmount("Hint Amount", Range(0, 1)) = 0
        _Weathering("Weathering", Range(0, 1)) = 0.5
        _CavitySmoothness("Cavity Smoothness", Range(0, 1)) = 0.35
        _CavityDruzy("Cavity Druzy", Range(0, 1)) = 0
        _CavityCrystalColor("Cavity Crystal Colour", Color) = (0.9, 0.85, 0.95, 1)
        _TexScale("Texture Scale", Float) = 14
        _Highlight("Highlight", Range(0, 1)) = 0
        _RockTex("Rock Detail (R)", 2D) = "gray" {}
        _NoiseTex("Noise", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _RockColor, _RockColor2, _CavityColor, _RimColor, _BandA, _BandB, _HintColor, _CavityCrystalColor;
            float _BandStrength, _BandFrequency, _BandOffset, _HintAmount, _Weathering, _CavitySmoothness, _TexScale, _Highlight, _CavityDruzy;
            float4 _RockTex_ST, _NoiseTex_ST;
        CBUFFER_END
        TEXTURE2D(_RockTex); SAMPLER(sampler_RockTex);
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
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 color : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                float3 normalOS : TEXCOORD4;
                float3 uvFog : TEXCOORD5;
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
                OUT.normalOS = IN.normalOS;
                OUT.color = IN.color;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.uvFog = float3(IN.uv, ComputeFogFactor(pos.positionCS.z));
                return OUT;
            }

            float TriplanarR(float3 p, float3 n, float scale)
            {
                float3 bw = pow(abs(n), 4.0);
                bw /= max(1e-4, bw.x + bw.y + bw.z);
                float tx = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, p.yz * scale).r;
                float ty = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, p.xz * scale).r;
                float tz = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, p.xy * scale).r;
                return tx * bw.x + ty * bw.y + tz * bw.z;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 N = normalize(IN.normalWS);
                float3 nOS = normalize(IN.normalOS);
                float3 V = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float4 c = IN.color;

                float rock = TriplanarR(IN.positionOS, nOS, _TexScale);
                float rockFine = TriplanarR(IN.positionOS + 3.1, nOS, _TexScale * 3.7);
                float grain = rock * 0.7 + rockFine * 0.3;
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xz * 2.7 + IN.positionOS.y * 1.3).b;

                // exterior: two-tone rock with dirt in crevices + optional exposed mineral hint
                float3 ext = lerp(_RockColor2.rgb, _RockColor.rgb, grain);
                ext = lerp(ext, ext * 0.55, _Weathering * (1.0 - grain) * 0.6);
                float hintMask = smoothstep(0.58, 0.72, noise) * _HintAmount;
                ext = lerp(ext, _HintColor.rgb * lerp(0.8, 1.0, grain), hintMask);

                // cut face: rind on the outside, bands toward the cavity
                float bandCoord = c.a * _BandFrequency + _BandOffset * 6.2831 + (noise - 0.5) * 1.6;
                float band = smoothstep(0.3, 0.7, sin(bandCoord) * 0.5 + 0.5);
                float3 bandCol = lerp(_BandA.rgb, _BandB.rgb, band);
                float bandMask = saturate(_BandStrength * 1.2) * smoothstep(lerp(0.78, 0.12, _BandStrength), lerp(0.96, 0.45, _BandStrength), c.a);
                float3 rim = lerp(_RimColor.rgb * lerp(0.8, 1.1, grain), bandCol * lerp(0.85, 1.05, rockFine), bandMask);

                // cavity wall: matrix colour with faint continuation of the last band
                float band2 = smoothstep(0.3, 0.7, sin(_BandFrequency + _BandOffset * 6.2831 + c.a * 2.0 + (noise - 0.5)) * 0.5 + 0.5);
                float3 cav = lerp(_CavityColor.rgb, lerp(_BandA.rgb, _BandB.rgb, band2), _BandStrength * 0.45) * lerp(0.82, 1.0, rockFine);
                // druzy floor: fine crystalline glitter in the crystal colour hides bare matrix under carpets
                float dz1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xz * 55.0 + IN.positionOS.y * 31.0).g;
                float dz2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.zy * 47.0 + IN.positionOS.x * 29.0).g;
                float dzFacet = saturate(dz1 * 0.6 + dz2 * 0.6);
                float3 druzyCol = _CavityCrystalColor.rgb * lerp(0.55, 1.15, dzFacet);
                cav = lerp(cav, druzyCol, _CavityDruzy * c.g);

                float3 albedo = ext * c.r + cav * c.g + rim * c.b;
                float smooth = 0.18 * c.r + lerp(_CavitySmoothness, 0.75, _CavityDruzy) * c.g + 0.16 * c.b;
                smooth += (grain - 0.5) * 0.1;

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.positionCS = IN.positionCS;
                inputData.normalWS = N;
                inputData.viewDirectionWS = V;
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = IN.uvFog.z;
                inputData.bakedGI = SampleSH(N);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo;
                s.metallic = 0.0;
                s.specular = half3(0, 0, 0);
                s.smoothness = saturate(smooth);
                s.occlusion = lerp(0.75, 1.0, grain) * lerp(1.0, 0.8, c.g * c.a);
                s.alpha = 1.0;
                s.normalTS = half3(0, 0, 1);
                float dzSpark = pow(saturate(dz1 * dz2 * 2.2), 8.0) * _CavityDruzy * c.g;
                s.emission = _Highlight * float3(1.0, 0.92, 0.7) * 0.22 + dzSpark * _CavityCrystalColor.rgb * 0.6;

                half4 col = UniversalFragmentPBR(inputData, s);
                col.rgb = MixFog(col.rgb, IN.uvFog.z);
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
