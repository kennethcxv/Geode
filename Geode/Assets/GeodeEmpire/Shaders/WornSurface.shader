Shader "GeodeEmpire/WornSurface"
{
    // V6 machine and fixture material: a painted / coated surface over bare metal, worn where the mesh's baked
    // vertex-colour says it is handled (R convex edges), grimed where it is recessed (G), dusted where it faces up
    // (B), with coolant / rust staining running down the sides. Tiles from Tools/Blender/gen_textures.py.
    Properties
    {
        _BaseColor("Paint Colour", Color) = (0.5, 0.52, 0.42, 1)
        _BaseMap("Paint Albedo", 2D) = "white" {}
        [NoScaleOffset] _BumpMap("Paint Normal", 2D) = "bump" {}
        [NoScaleOffset] _MaskMap("Paint Mask (R metal, G occlusion, A smoothness)", 2D) = "gray" {}
        _MetalColor("Metal Colour", Color) = (0.75, 0.74, 0.72, 1)
        [NoScaleOffset] _MetalMap("Metal Albedo", 2D) = "gray" {}
        [NoScaleOffset] _MetalBump("Metal Normal", 2D) = "bump" {}
        [NoScaleOffset] _MetalMask("Metal Mask", 2D) = "gray" {}
        _Tiling("Tiling (repeats per box-uv unit)", Float) = 1
        _BumpScale("Normal Strength", Range(0, 2)) = 1
        _SmoothnessScale("Smoothness Scale", Range(0, 1.5)) = 1
        _Wear("Edge Wear", Range(0, 1)) = 0.5
        _Chips("Paint Chips", Range(0, 1)) = 0.25
        _Grime("Grime", Range(0, 1)) = 0.5
        _GrimeColor("Grime Colour", Color) = (0.32, 0.29, 0.25, 1)
        _Stain("Staining", Range(0, 1)) = 0.3
        _StainColor("Stain Colour", Color) = (0.42, 0.33, 0.22, 1)
        _Dust("Dust", Range(0, 1)) = 0.3
        _DustColor("Dust Colour", Color) = (0.62, 0.58, 0.5, 1)
        _NoiseTex("Noise", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor, _MetalColor, _GrimeColor, _StainColor, _DustColor;
            float4 _BaseMap_ST, _NoiseTex_ST;
            float _Tiling, _BumpScale, _SmoothnessScale, _Wear, _Chips, _Grime, _Stain, _Dust;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap); TEXTURE2D(_MaskMap);
        TEXTURE2D(_MetalMap); TEXTURE2D(_MetalBump); TEXTURE2D(_MetalMask);
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
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float4 color : TEXCOORD3;
                float3 uvFog : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS = nrm.normalWS;
                real sign = IN.tangentOS.w * GetOddNegativeScale();
                OUT.tangentWS = float4(nrm.tangentWS, sign);
                OUT.color = IN.color;
                OUT.uvFog = float3(IN.uv * _Tiling, ComputeFogFactor(pos.positionCS.z));
                OUTPUT_LIGHTMAP_UV(IN.staticLightmapUV, unity_LightmapST, OUT.staticLightmapUV);
                OUTPUT_SH(nrm.normalWS, OUT.vertexSH);
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                float2 uv = IN.uvFog.xy;
                float4 wear = IN.color;                       // r edges, g cavities, b up
                float3 V = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                // paint and bare metal layers
                float3 paint = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv).rgb * _BaseColor.rgb;
                float3 paintN = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, uv), _BumpScale);
                float4 paintM = SAMPLE_TEXTURE2D(_MaskMap, sampler_BaseMap, uv);
                float3 metal = SAMPLE_TEXTURE2D(_MetalMap, sampler_BaseMap, uv * 1.7).rgb * _MetalColor.rgb;
                float3 metalN = UnpackNormalScale(SAMPLE_TEXTURE2D(_MetalBump, sampler_BaseMap, uv * 1.7), _BumpScale);
                float4 metalM = SAMPLE_TEXTURE2D(_MetalMask, sampler_BaseMap, uv * 1.7);

                // breakup noise at two scales (smooth channels only: the per-texel channel streaks under anisotropic filtering)
                float4 nz = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * 0.37 + IN.positionWS.xz * 0.11);
                float4 nz2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, uv * 1.9 + IN.positionWS.yx * 0.23);
                float breakup = nz.r * 0.6 + nz2.b * 0.4;

                // edge wear: paint gone from the handled edges and corners, ragged by the breakup, plus chips on the flats
                float edge = saturate(wear.r * (0.55 + 0.9 * breakup));
                float wearMask = smoothstep(0.62 - 0.45 * _Wear, 0.78 - 0.45 * _Wear, edge);
                float chips = smoothstep(0.78 - 0.18 * _Chips, 0.9 - 0.18 * _Chips, nz2.a * 0.7 + nz.a * 0.3) * _Chips * (1.0 - wear.g);
                wearMask = saturate(wearMask + chips);
                float3 albedo = lerp(paint, metal, wearMask);
                float3 nTS = normalize(lerp(paintN, metalN, wearMask));
                float smooth = lerp(paintM.a, metalM.a, wearMask) * _SmoothnessScale;
                float metallic = lerp(paintM.r, metalM.r, wearMask);
                float occ = lerp(paintM.g, metalM.g, wearMask);
                // a fresh chip edge: a thin brighter rim of primer / bare metal round the worn patch
                float rim = smoothstep(0.0, 0.25, wearMask) * (1.0 - smoothstep(0.25, 0.6, wearMask));
                albedo = lerp(albedo, albedo * 1.25 + 0.05, rim * 0.5);

                // grime in the seams and recesses (the tile's occlusion and the baked cavity mask), less on wear
                float grime = _Grime * saturate(wear.g * 1.1 + (1.0 - occ) * 0.6) * (0.7 + 0.5 * breakup) * (1.0 - 0.5 * wearMask);
                grime = saturate(grime);
                albedo = lerp(albedo, albedo * _GrimeColor.rgb * 1.6, grime * 0.6);
                smooth *= 1.0 - 0.35 * grime;

                // stains running down the sides (coolant, rust): streaks in uv.y, not on the tops, thicker under recesses
                float streak = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, float2(uv.x * 2.3, uv.y * 0.35) + nz.g * 0.1).b;
                float stain = _Stain * smoothstep(0.5, 0.85, streak * 0.75 + wear.g * 0.25) * (1.0 - wear.b * 0.9);
                albedo = lerp(albedo, albedo * _StainColor.rgb * 1.5, stain * 0.45);
                smooth = lerp(smooth, smooth * 0.8 + 0.1, stain * 0.5);

                // dust and dried slurry on the tops
                float dust = _Dust * saturate(wear.b * 1.2 - 0.15) * (0.55 + 0.8 * nz.b) * (1.0 - 0.6 * wearMask);
                albedo = lerp(albedo, _DustColor.rgb * lerp(0.8, 1.05, nz2.r), dust * 0.6);
                smooth = lerp(smooth, 0.12, dust * 0.8);
                metallic *= 1.0 - dust * 0.8;

                float3 bitangent = IN.tangentWS.w * cross(IN.normalWS, IN.tangentWS.xyz);
                float3 N = normalize(TransformTangentToWorld(nTS, half3x3(IN.tangentWS.xyz, bitangent, IN.normalWS)));

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.positionCS = IN.positionCS;
                inputData.normalWS = N;
                inputData.viewDirectionWS = V;
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = IN.uvFog.z;
                inputData.bakedGI = SAMPLE_GI(IN.staticLightmapUV, IN.vertexSH, N);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(IN.staticLightmapUV);

                SurfaceData s = (SurfaceData)0;
                s.albedo = albedo;
                s.metallic = saturate(metallic);
                s.specular = half3(0, 0, 0);
                s.smoothness = saturate(smooth);
                s.occlusion = lerp(1.0, occ, 0.8);
                s.alpha = 1.0;
                s.normalTS = nTS;
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
