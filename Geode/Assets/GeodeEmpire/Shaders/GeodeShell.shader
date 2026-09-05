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
        // fracture overlay (driven per specimen through a property block)
        _ImpactCount("Impact Count", Float) = 0
        _SeamVisible("Seam Guide", Float) = 0.4
        _SurfR("Surface Radius", Float) = 0.06
        _CrackFade("Crack Fade", Float) = 1
        // exterior character (per specimen through a property block)
        _TexFamily("Texture Family", Float) = 0
        _Dirt("Clay Coating", Range(0, 1)) = 0
        _Stain("Iron Staining", Range(0, 1)) = 0
        _Chip("Natural Chip (lon, lat, radius m, amount)", Vector) = (0, 0, 0, 0)
        _Polish("Cut Face Polish", Range(0, 1)) = 0
        // saw: the planned cut plane (object-space normal xyz, height w) and the kerf so far (feed axis xyz, reach w)
        _CutPlane("Cut Plane", Vector) = (0, 1, 0, 0)
        _CutFeed("Cut Feed", Vector) = (1, 0, 0, -10)
        _CutShow("Cut Preview", Range(0, 1)) = 0
        _CutDepth("Cut Depth", Vector) = (0, 1, 0, 100)
        _CutDone("Cut Done", Vector) = (0, 1, 0, -100)
        _Wet("Wetness", Range(0, 1)) = 0
        _Dust("Dust", Range(0, 1)) = 0
        // V6 material pipeline: tileable detail sets (Tools/Blender/gen_textures.py), triplanar in object space
        [NoScaleOffset] _RindAlbedo("Rind Albedo", 2D) = "gray" {}
        [NoScaleOffset] _RindNormal("Rind Normal", 2D) = "bump" {}
        [NoScaleOffset] _RindMask("Rind Mask (G occlusion, A smoothness)", 2D) = "gray" {}
        [NoScaleOffset] _FracAlbedo("Fracture Albedo", 2D) = "gray" {}
        [NoScaleOffset] _FracNormal("Fracture Normal", 2D) = "bump" {}
        [NoScaleOffset] _FracMask("Fracture Mask", 2D) = "gray" {}
        [NoScaleOffset] _CavAlbedo("Cavity Albedo", 2D) = "gray" {}
        [NoScaleOffset] _CavNormal("Cavity Normal", 2D) = "bump" {}
        [NoScaleOffset] _CavMask("Cavity Mask", 2D) = "gray" {}
        [NoScaleOffset] _DruseAlbedo("Druse Albedo", 2D) = "white" {}
        [NoScaleOffset] _DruseNormal("Druse Normal", 2D) = "bump" {}
        [NoScaleOffset] _DruseMask("Druse Mask", 2D) = "gray" {}
        _DetailScale("Detail Scale (tiles per metre)", Float) = 9
        _DetailStrength("Detail Normal Strength", Range(0, 2)) = 1
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
            // fracture overlay scalars (set per specimen through a property block)
            float _ImpactCount;
            float _SeamVisible;         // faint natural seam guide, stronger under the inspection lamp
            float _SurfR;               // mean equator radius (m), for metric distances on the surface
            float _CrackFade;           // 1 on a closed rock, lower once opened
            float _TexFamily;           // 0 coarse matrix, 1 weathered rind, 2 layered skin, 3 volcanic crust
            float _Dirt;                // clay coating still on the rock (washing lowers it)
            float _Stain;               // iron-oxide streaking
            float4 _Chip;               // natural chip: longitude fraction, signed latitude fraction, radius (m), amount
            float _Polish;              // finish on sawn faces: 0 saw-marked matte, 1 mirror
            float4 _CutPlane;           // object-space plane: normal xyz, height w
            float4 _CutFeed;            // object-space feed axis xyz; w = how far along it the kerf has reached
            float _CutShow;             // 0 hidden, 1 preview line drawn
            float4 _CutDepth;           // object-space up axis xyz of the current pass; w = the projection the kerf reaches up to (one blade pass)
            float4 _CutDone;            // a region already cut (up axis xyz, limit w; w < -9: none)
            float _Wet;                 // fresh from the tub or the saw: darker, richer, with a water sheen; dries off
            float _Dust;                // rock dust on a freshly broken interior until it is rinsed
            float _DetailScale, _DetailStrength;
        CBUFFER_END

        // Per-region clay (§7.3). Twenty-four patches — eight around, three up — packed six to a float4 so the
        // whole shell's state is one small array. Outside UnityPerMaterial: it is set through a property block,
        // and specimens are not SRP-batched anyway. w of each element is unused padding.
        float4 _RegionClean[6];
        float _RegionDirtOn;        // 0 = fall back to the single _Dirt value (old saves, scenery, thumbnails)

        // Clay at a point on the shell, blended between the four regions it lies between so the patches read as
        // a wiped surface rather than as tiles.
        float ClayAt(float3 nOS)
        {
            if (_RegionDirtOn < 0.5) return _Dirt;
            float3 d = normalize(nOS);
            float lon = (atan2(d.z, d.x) / 6.2831853 + 0.5) * 8.0 - 0.5;    // -0.5 .. 7.5, centres on region middles
            float band = saturate((d.y + 0.62) / 1.24) * 2.0;                // 0 lower .. 2 upper
            float lf = frac(lon + 8.0);
            int l0 = ((int)floor(lon) + 8) % 8;
            int l1 = (l0 + 1) % 8;
            float bf = frac(band);
            int b0 = clamp((int)floor(band), 0, 2);
            int b1 = min(b0 + 1, 2);
            // _RegionClean is 24 floats packed 4-to-a-float4
            #define REGION_CLEAN(i) (_RegionClean[(i) >> 2][(i) & 3])
            float c00 = REGION_CLEAN(b0 * 8 + l0), c10 = REGION_CLEAN(b0 * 8 + l1);
            float c01 = REGION_CLEAN(b1 * 8 + l0), c11 = REGION_CLEAN(b1 * 8 + l1);
            float clean = lerp(lerp(c00, c10, lf), lerp(c01, c11, lf), bf);
            #undef REGION_CLEAN
            return _Dirt * (1.0 - saturate(clean));
        }
        // fracture overlay arrays: kept outside the per-material block so property-block arrays reach them
        float _SectorCrack[16];         // seam stress per sector, >= 1 is an open crack
        float4 _Impacts[32];            // chisel marks: longitude fraction, signed latitude fraction, radius (m), strength
        float _LoupeBoost;              // global: 1 while the player looks through the loupe
        float _GeodeDebug;              // global dev switch: 1 = albedo only (material diagnosis)
        TEXTURE2D(_RockTex); SAMPLER(sampler_RockTex);
        TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);
        TEXTURE2D(_RindAlbedo); TEXTURE2D(_RindNormal); TEXTURE2D(_RindMask);
        TEXTURE2D(_FracAlbedo); TEXTURE2D(_FracNormal); TEXTURE2D(_FracMask);
        TEXTURE2D(_CavAlbedo); TEXTURE2D(_CavNormal); TEXTURE2D(_CavMask);
        TEXTURE2D(_DruseAlbedo); TEXTURE2D(_DruseNormal); TEXTURE2D(_DruseMask);
        SAMPLER(sampler_RindAlbedo);
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
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
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
                float2 uv2 : TEXCOORD6;
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
                OUT.uv2 = IN.uv2;
                return OUT;
            }

            float Noise1(float x, float row)
            {
                return SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, float2(x, row), 0).r;
            }

            // Persistent fracture marks drawn in the shell surface itself: the jagged seam line per cracked sector, a
            // dotted hairline where a sector is stressed, chips with radiating hairlines where the chisel stood, and a
            // hairline creeping from each chip toward the seam as its sector loads up.
            void FractureOverlay(float2 uv2, float grain, bool face, out float dark, out float frost, out float guide)
            {
                dark = 0.0; frost = 0.0; guide = 0.0;
                float lonF = uv2.x;
                float latF = uv2.y;
                float R = max(0.01, _SurfR);
                float rs = clamp(R / 0.06, 0.45, 1.4);     // seam widths scale with the rock
                float mPerLat = 1.5708 * R;
                float mPerLon = 6.2832 * R * max(0.2, cos(latF * 1.5708));

                float seamLat = (Noise1(lonF * 7.0, 0.31) - 0.5) * 0.09 + (Noise1(lonF * 29.0, 0.77) - 0.5) * 0.03;
                float dSeam = abs(latF - seamLat) * mPerLat;
                int sector = (int)floor(frac(lonF) * 16.0);
                float st = _SectorCrack[sector];
                float cracked = smoothstep(0.82, 1.0, st);
                float hair = smoothstep(0.3, 0.82, st);
                float widthNoise = Noise1(lonF * 23.0, 0.12);
                float halfW = lerp(0.0006, 0.0024, cracked) * lerp(0.6, 1.4, widthNoise) * rs;
                float seamLine = 1.0 - smoothstep(halfW * 0.5, halfW * 1.6, dSeam);
                float dots = smoothstep(0.38, 0.62, Noise1(lonF * 90.0, 0.55));
                float seamA = seamLine * (cracked + hair * (1.0 - cracked) * dots * 0.8);
                float lip = (1.0 - smoothstep(halfW * 1.4, halfW * 2.6, dSeam)) * cracked * 0.45 * (0.5 + 0.5 * widthNoise);
                // the fracture face itself lies ON the seam (its surface coordinate is the seam latitude): the seam
                // line, lip and guide belong to the exterior only, otherwise they paint the whole broken face
                if (face) { seamA = 0.0; lip = 0.0; }
                // the natural seam: a soft, slightly darker weathered band a real geode shows, clearer under the lamp
                float gNoise = Noise1(lonF * 17.0, 0.66);
                guide = face ? 0.0 : (1.0 - smoothstep(0.0012 * rs, (0.0032 + 0.0015 * gNoise) * rs, dSeam)) * min(1.0, _SeamVisible + 0.5 * _LoupeBoost) * (1.0 - cracked) * (0.45 + 0.3 * gNoise);
                dark += seamA * _CrackFade;
                frost += lip * _CrackFade;

                int n = (int)_ImpactCount;
                for (int k = 0; k < n; k++)
                {
                    float4 im = _Impacts[k];
                    float du = frac(lonF - im.x + 0.5) - 0.5;
                    float dx = du * mPerLon;
                    float dy = (latF - im.y) * mPerLat;
                    float dist = sqrt(dx * dx + dy * dy);
                    float r = im.z;
                    float ang = atan2(dy, dx);
                    // ragged chip outline: two noise octaves around the rim, never a clean disc
                    float rn = r * (0.55 + 0.45 * Noise1(ang * 0.55 + k * 0.37, 0.5) + 0.3 * (Noise1(ang * 2.1 + k * 0.91, 0.85) - 0.5));
                    float inside = 1.0 - smoothstep(rn * 0.55, rn, dist);
                    float ring = smoothstep(rn * 0.72, rn * 1.02, dist) * (1.0 - smoothstep(rn * 1.02, rn * 1.35, dist));
                    float rays = pow(saturate(cos(ang * 3.0 + k * 1.7)), 22.0) * (1.0 - smoothstep(r * 1.0, r * 2.9, dist)) * step(rn * 0.9, dist);
                    frost += inside * im.w * (0.7 + 0.5 * grain);
                    dark += (ring * 0.9 + rays * 0.8) * im.w;
                    // hairline from the chip to the seam, growing with that sector's stress
                    float ist = _SectorCrack[(int)floor(frac(im.x) * 16.0)];
                    float toSeam = seamLat - im.y;
                    float along = (latF - im.y) / (abs(toSeam) < 1e-4 ? 1e-4 : toSeam);
                    float wig = (Noise1(latF * 23.0 + k * 0.5, 0.2) - 0.5) * 0.0035;
                    float hl = step(0.0, along) * step(along, saturate(ist)) * (1.0 - smoothstep(0.0005, 0.0014, abs(dx + wig))) * step(rn * 0.9, dist);
                    dark += hl * im.w * 0.85;
                }
                dark = saturate(dark);
                frost = saturate(frost);
            }

            // One detail set sampled three ways in object space and blended by the surface normal: albedo, normal
            // (whiteout blend, so no tangents are needed on the procedural mesh), and the metallic/occlusion/smoothness
            // mask. Object space keeps the detail glued to the rock through every pose.
            struct Detail { float3 albedo; float3 normalOS; float occlusion; float smoothness; };
            Detail SampleDetail(TEXTURE2D_PARAM(albedoTex, ss), TEXTURE2D(normalTex), TEXTURE2D(maskTex), float3 p, float3 nOS, float scale, float strength)
            {
                Detail d;
                float3 bw = pow(abs(nOS), 4.0);
                bw /= max(1e-4, bw.x + bw.y + bw.z);
                // projection UVs match the tangent frame the whiteout swizzle assumes (x plane: zy, y plane: xz, z plane: xy),
                // and the u axis mirrors on the back-facing sides so a knob never lights as a dent there
                float3 axisSign = sign(nOS);
                float2 uvX = p.zy * scale, uvY = p.xz * scale, uvZ = p.xy * scale;
                uvX.x *= axisSign.x; uvY.x *= axisSign.y; uvZ.x *= -axisSign.z;
                float3 ax = SAMPLE_TEXTURE2D(albedoTex, ss, uvX).rgb, ay = SAMPLE_TEXTURE2D(albedoTex, ss, uvY).rgb, az = SAMPLE_TEXTURE2D(albedoTex, ss, uvZ).rgb;
                d.albedo = ax * bw.x + ay * bw.y + az * bw.z;
                float4 mx = SAMPLE_TEXTURE2D(maskTex, ss, uvX), my = SAMPLE_TEXTURE2D(maskTex, ss, uvY), mz = SAMPLE_TEXTURE2D(maskTex, ss, uvZ);
                float4 m = mx * bw.x + my * bw.y + mz * bw.z;
                d.occlusion = m.g;
                d.smoothness = m.a;
                float3 tx = UnpackNormalScale(SAMPLE_TEXTURE2D(normalTex, ss, uvX), strength);
                float3 ty = UnpackNormalScale(SAMPLE_TEXTURE2D(normalTex, ss, uvY), strength);
                float3 tz = UnpackNormalScale(SAMPLE_TEXTURE2D(normalTex, ss, uvZ), strength);
                tx.x *= axisSign.x; ty.x *= axisSign.y; tz.x *= -axisSign.z;
                tx = float3(tx.xy + nOS.zy, abs(tx.z) * nOS.x);
                ty = float3(ty.xy + nOS.xz, abs(ty.z) * nOS.y);
                tz = float3(tz.xy + nOS.xy, abs(tz.z) * nOS.z);
                d.normalOS = normalize(tx.zyx * bw.x + ty.xzy * bw.y + tz.xyz * bw.z);
                return d;
            }

            float TriplanarR(float3 p, float3 n, float scale)
            {
                float3 bw = pow(abs(n), 4.0);
                bw /= max(1e-4, bw.x + bw.y + bw.z);
                float tx = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, p.zy * scale).r;
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
                float rockMicro = TriplanarR(IN.positionOS + 7.3, nOS, _TexScale * 11.0);   // sand-grain scale for close-ups
                float grain = rock * 0.55 + rockFine * 0.3 + rockMicro * 0.15;
                // micro relief: bend the normal by the detail height gradient so the exterior stops reading as smooth
                // clay (screen-space derivatives of the height, scaled by the surface's own tangent frame)
                // V6: real micro relief from the baked detail tiles. The exterior wears the weathered-rind set (its
                // pits and knobs), a natural fracture face the conchoidal set, the cavity the chalcedony-wall set;
                // a sawn face is flat and only keeps a little frost until it is polished.
                bool sawnEarly = IN.uv2.y < -1.5;
                float detScale = _DetailScale;
                int texFamB = (int)(_TexFamily + 0.5);
                float rindStrength = _DetailStrength * (texFamB == 1 ? 0.75 : texFamB == 3 ? 1.35 : texFamB == 0 ? 1.15 : 1.0) * (1.0 - 0.35 * _Weathering);
                Detail det;
                float detSmooth = 0.5, detOcc = 1.0; float3 detAlbedo = float3(0.5, 0.5, 0.5);
                if (c.r > 0.5)
                {
                    det = SampleDetail(TEXTURE2D_ARGS(_RindAlbedo, sampler_RindAlbedo), _RindNormal, _RindMask, IN.positionOS, nOS, detScale, rindStrength * (1.0 + 0.5 * saturate(ClayAt(nOS))));
                    N = TransformObjectToWorldNormal(det.normalOS);
                }
                else if (c.b > 0.5 && !sawnEarly)
                {
                    det = SampleDetail(TEXTURE2D_ARGS(_FracAlbedo, sampler_RindAlbedo), _FracNormal, _FracMask, IN.positionOS + 2.7, nOS, detScale * 1.6, _DetailStrength * 1.2);
                    N = TransformObjectToWorldNormal(det.normalOS);
                }
                else if (c.g > 0.5)
                {
                    det = SampleDetail(TEXTURE2D_ARGS(_CavAlbedo, sampler_RindAlbedo), _CavNormal, _CavMask, IN.positionOS + 5.1, nOS, detScale * 2.2, _DetailStrength * 0.9);
                    if (_CavityDruzy > 0.01)
                    {
                        // under a carpet the wall is a mosaic of tiny terminations (the druse tile), not bare chalcedony
                        Detail dd = SampleDetail(TEXTURE2D_ARGS(_DruseAlbedo, sampler_RindAlbedo), _DruseNormal, _DruseMask, IN.positionOS + 8.3, nOS, detScale * 3.0, _DetailStrength * 1.1);
                        float dzB = saturate(_CavityDruzy);
                        det.normalOS = normalize(lerp(det.normalOS, dd.normalOS, dzB));
                        det.albedo = lerp(det.albedo, dd.albedo, dzB);
                        det.smoothness = lerp(det.smoothness, dd.smoothness, dzB);
                        det.occlusion = lerp(det.occlusion, dd.occlusion, dzB);
                    }
                    N = TransformObjectToWorldNormal(det.normalOS);
                }
                else
                {
                    // sawn: the frost of the blade marks fades with the polish
                    det = SampleDetail(TEXTURE2D_ARGS(_FracAlbedo, sampler_RindAlbedo), _FracNormal, _FracMask, IN.positionOS + 2.7, nOS, detScale * 3.0, _DetailStrength * 0.25 * (1.0 - _Polish));
                    N = normalize(lerp(N, TransformObjectToWorldNormal(det.normalOS), 1.0 - _Polish));
                }
                detSmooth = det.smoothness; detOcc = det.occlusion; detAlbedo = det.albedo;
                float detLum = dot(detAlbedo, float3(0.3, 0.59, 0.11));
                float3 detTint = detAlbedo / max(0.05, detLum);   // the tile's colour breakup, brightness taken out
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xz * 2.7 + IN.positionOS.y * 1.3).b;

                // exterior: two-tone rock with dirt in crevices + optional exposed mineral hint
                int texFam = (int)(_TexFamily + 0.5);
                float grainX = grain;
                if (texFam == 0) grainX = saturate((grain - 0.5) * 1.1 + 0.5);             // coarse matrix: a little more grain contrast
                else if (texFam == 1) grainX = saturate((grain - 0.5) * 0.55 + 0.55);      // weathered rind: soft, even, a little pale
                float3 ext = lerp(_RockColor2.rgb, _RockColor.rgb, grainX);
                ext *= lerp(0.72, 1.28, saturate(detLum * 1.6));                    // pits dark, knobs pale (from the tile)
                ext = lerp(ext, ext * detTint, 0.45);                                // staining and mineral colour breakup
                // mottling: slow colour drift over the rock (no rock is one flat tone), crevices go dark, pits go darker
                float mottle = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xz * 1.7 + IN.positionOS.y * 1.1 + 0.61).r;
                float mottle2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.yz * 3.1 + IN.positionOS.x * 2.2 + 0.23).b;
                ext *= lerp(0.7, 1.15, mottle) * lerp(0.9, 1.08, mottle2);
                ext = lerp(ext, ext * float3(0.92, 0.85, 0.78), smoothstep(0.55, 0.75, mottle2) * 0.6);
                ext = lerp(ext, ext * 0.5, pow(saturate(1.0 - grain), 2.5) * 0.45);
                // pits and creases come from the rind tile's occlusion (never from thresholded per-texel noise: that
                // streaks under anisotropic filtering on any foreshortened face)
                float pits = smoothstep(0.62, 0.3, detOcc);
                ext = lerp(ext, ext * 0.5, pits * 0.6);
                ext = lerp(ext, ext * 0.55, _Weathering * (1.0 - grain) * 0.6);
                if (texFam == 1)
                {
                    // weathered rind: slightly bleached skin with fine pitting
                    ext = lerp(ext, ext * float3(1.08, 1.05, 1.0) + 0.015, 0.4);
                    ext = lerp(ext, ext * 0.7, pits * 0.5);
                }
                else if (texFam == 2)
                {
                    // layered skin: faint growth layers wrapping the rock along its latitude, broken up by the grain
                    float layer = sin(IN.uv2.y * 34.0 + (noise - 0.5) * 4.0 + IN.uv2.x * 3.0) * 0.5 + 0.5;
                    float lay = smoothstep(0.3, 0.7, layer) * smoothstep(0.25, 0.6, grain + noise * 0.3);
                    ext = lerp(ext * 0.96, ext * 1.04 + 0.01, lay);
                }
                else if (texFam == 3)
                {
                    // volcanic crust: dark, with vesicles (gas holes) and the odd pale mineral fleck
                    ext = lerp(ext, ext * float3(0.5, 0.48, 0.5), 0.7);
                    float ves = smoothstep(0.6, 0.7, SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.yz * 41.0 + IN.positionOS.x * 27.0).b);
                    ext = lerp(ext, ext * 0.35, ves);
                }
                // iron staining: soft rust patches seeping out of the pits, darker in the low grain
                float stainN = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xz * 4.2 + IN.positionOS.y * 3.1 + 0.37).r;
                float stainN2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.zy * 9.0 + IN.positionOS.x * 5.0).a;
                float stainMask = _Stain * smoothstep(0.48, 0.78, stainN * 0.7 + stainN2 * 0.3 + (1.0 - grain) * 0.12);
                float3 rust = lerp(float3(0.5, 0.3, 0.16), float3(0.36, 0.2, 0.1), 1.0 - grain);
                ext = lerp(ext, lerp(ext, rust, 0.75), stainMask);
                // exposed mineral: a faint hint at arm's length; under the loupe the veins and a speckle of tiny
                // exposed crystals in the mineral's colour come up (still only what is on the outside)
                float hintAmt = _HintAmount * (1.0 + 1.6 * _LoupeBoost);
                float hintMask = smoothstep(0.58 - 0.08 * _LoupeBoost, 0.72, noise) * saturate(hintAmt);
                float3 hintCol = lerp(ext, _HintColor.rgb * lerp(0.8, 1.0, grain), 0.4 + 0.45 * _LoupeBoost);
                ext = lerp(ext, hintCol, hintMask);
                float speck = pow(saturate(SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xy * 61.0 + IN.positionOS.z * 37.0).a * 1.1), 9.0);
                ext = lerp(ext, _HintColor.rgb * 1.15, speck * _HintAmount * 2.5 * _LoupeBoost);
                // a natural chip: a ragged little window where the rind broke away and the interior shows through
                float chipAmt = 0.0;
                if (_Chip.w > 0.001)
                {
                    float R = max(0.01, _SurfR);
                    float cdu = frac(IN.uv2.x - _Chip.x + 0.5) - 0.5;
                    float cdx = cdu * 6.2832 * R * max(0.2, cos(IN.uv2.y * 1.5708));
                    float cdy = (IN.uv2.y - _Chip.y) * 1.5708 * R;
                    float cdist = sqrt(cdx * cdx + cdy * cdy);
                    float cang = atan2(cdy, cdx);
                    float crn = _Chip.z * (0.6 + 0.4 * Noise1(cang * 0.6 + 2.1, 0.5) + 0.25 * (Noise1(cang * 2.3, 0.85) - 0.5));
                    float cin = 1.0 - smoothstep(crn * 0.7, crn, cdist);
                    float cring = smoothstep(crn * 0.8, crn * 1.05, cdist) * (1.0 - smoothstep(crn * 1.05, crn * 1.3, cdist));
                    chipAmt = cin * _Chip.w;
                    float3 window = lerp(_CavityColor.rgb, _CavityCrystalColor.rgb, 0.65 + 0.3 * grain) * lerp(0.7, 1.05, grain);
                    ext = lerp(ext, window, chipAmt);
                    ext = lerp(ext, ext * 0.45, cring * _Chip.w);
                }

                // fracture overlay: only the exterior and natural fracture faces carry it; sawn faces are flat
                bool sawn = IN.uv2.y < -1.5;
                float crackDark = 0.0, crackFrost = 0.0, seamGuide = 0.0;
                if ((c.r > 0.5 || c.b > 0.5) && !sawn) FractureOverlay(IN.uv2, grain, c.b > 0.5, crackDark, crackFrost, seamGuide);
                float3 frostCol = lerp(ext, float3(0.86, 0.84, 0.79) * lerp(0.85, 1.0, grain), 0.5);
                ext = lerp(ext, frostCol, crackFrost * 0.7);
                // clay coating: sits in the low grain first and leaves the high points as it is scrubbed away; while
                // it is on, it hides the seam, the staining and the mineral hints the shell would otherwise give away
                float dirtN = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xz * 6.5 + IN.positionOS.y * 4.0).r;
                float dirtFine = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xy * 23.0 + IN.positionOS.z * 17.0).b;
                // V6: quarry clay is a crust, not a spatter: it packs the pits and low ground of the rind first (the tile's
                // occlusion), thins over the knobs, and breaks into a cracked skin at the boundary
                float clayHere = ClayAt(nOS);
                float dirtMask = clayHere > 0.001 ? smoothstep(0.05, 0.4, clayHere * 1.25 - (grain * 0.25 + detOcc * 0.55 + dirtN * 0.25 + dirtFine * 0.08) + 0.38) : 0.0;
                // dried quarry clay: ochre-brown, caked thick in the hollows and thin and dusty over the high points
                float3 clay = lerp(float3(0.26, 0.2, 0.13), float3(0.36, 0.29, 0.2), grain) * lerp(0.82, 1.1, dirtN) * lerp(0.88, 1.06, dirtFine);
                clay = lerp(clay, clay * 0.5, smoothstep(0.55, 0.7, dirtFine));        // cracked, crumbly patches
                clay *= lerp(0.65, 1.05, grain);                                         // crevices in the crust stay dark
                clay = lerp(clay, clay * float3(0.85, 0.8, 0.75), smoothstep(0.4, 0.6, dirtN) * 0.5);
                float3 dust = lerp(ext, float3(0.42, 0.36, 0.28), 0.45);
                ext = lerp(ext, lerp(dust, clay, dirtMask), saturate(dirtMask * 1.6));
                seamGuide *= 1.0 - dirtMask * 0.9;
                ext = lerp(ext, ext * 0.55, seamGuide);
                ext = lerp(ext, ext * 0.2, crackDark);
                // the saw: a chalk-thin guide line where the blade will pass, and the dark wet kerf it has cut so far
                if (_CutShow > 0.001)
                {
                    float dPlane = abs(dot(IN.positionOS, _CutPlane.xyz) - _CutPlane.w);
                    float along = dot(IN.positionOS, _CutFeed.xyz);
                    float guide = (1.0 - smoothstep(0.0009, 0.0018, dPlane)) * _CutShow;
                    // the kerf: as far along as the blade has reached, no higher than the blade passes in this pass,
                    // plus whatever a previous pass already cut (a turned-over rock keeps its first kerf)
                    float depthNow = step(dot(IN.positionOS, _CutDepth.xyz), _CutDepth.w) * step(along, _CutFeed.w);
                    float depthDone = step(dot(IN.positionOS, _CutDone.xyz), _CutDone.w);
                    float kerf = (1.0 - smoothstep(0.0014, 0.0022, dPlane)) * saturate(depthNow + depthDone) * _CutShow;
                    ext = lerp(ext, float3(0.95, 0.92, 0.8), guide * 0.85);
                    ext = lerp(ext, float3(0.08, 0.075, 0.07), kerf);
                }

                // cut face (V6 §14): layers from the outside in. A broken geode face reads as the exterior skin (0-0.08),
                // the weathered matrix rind (0.08-0.3), then the pale chalcedony that lines every cavity, banded where the
                // family bands (agate: the whole face), and at the inner edge the mineralised zone the crystals grow from.
                // Real faces are chalcedony-dominant: the dark matrix is a thin outer band, never the body of the ring.
                float bandCoord = c.a * _BandFrequency + _BandOffset * 6.2831 + (noise - 0.5) * 1.6 + (rockFine - 0.5) * 0.6;
                float band = smoothstep(0.38, 0.62, sin(bandCoord) * 0.5 + 0.5);
                float bandFine = smoothstep(0.3, 0.7, sin(bandCoord * 4.7 + 1.3 + (rockFine - 0.5) * 2.0) * 0.5 + 0.5);
                float3 bandCol = lerp(_BandA.rgb, _BandB.rgb, band) * lerp(0.86, 1.08, bandFine);   // fine bands inside the coarse ones
                float3 chalc = lerp(_CavityColor.rgb, float3(0.64, 0.65, 0.65), 0.35) * lerp(0.86, 1.06, rockFine) * lerp(0.9, 1.06, bandFine) * lerp(0.9, 1.04, grain);
                float bandMask = saturate(_BandStrength * 1.2) * smoothstep(lerp(0.62, 0.1, _BandStrength), lerp(0.9, 0.38, _BandStrength), c.a);
                float3 rim = lerp(chalc, bandCol, bandMask);
                float rindT = smoothstep(0.02, 0.09, c.a);
                float rindZone = smoothstep(0.06, 0.14, c.a) * (1.0 - smoothstep(0.2, 0.32, c.a));
                float edgeZone = smoothstep(0.8, 0.94, c.a);
                float3 rindCol = lerp(_RimColor.rgb, _RimColor.rgb * float3(1.08, 1.04, 0.98) + 0.05, 0.55) * lerp(0.78, 1.05, grain);
                rim = lerp(rim, rindCol, rindZone * 0.85);
                rim = lerp(rim, lerp(_CavityColor.rgb, _CavityCrystalColor.rgb, 0.45) * lerp(0.85, 1.05, grain), edgeZone * 0.7);
                rim = lerp(ext * 0.9, rim, rindT);
                rim *= lerp(0.85, 1.0, mottle);
                if (!sawn) { rim *= lerp(0.85, 1.15, saturate(detLum * 1.5)); rim = lerp(rim, rim * detTint, 0.25); }
                // chips torn out of the rim by the chisel: pale bruised patches with dark edges, only along the outer
                // edge of the broken face (every strike sits on the seam latitude, so without this the marks would
                // sweep across the whole face as wedges)
                float edgeBruise = 1.0 - rindT;
                rim = lerp(rim, rim * float3(0.9, 0.88, 0.85) + 0.12, crackFrost * 0.6 * edgeBruise);
                rim = lerp(rim, rim * 0.35, crackDark * 0.8 * edgeBruise);
                float sawnSmooth = 0.0;
                if (sawn)
                {
                    // a saw leaves a flat, slightly frosted face with faint arc marks; the bands show fully but dull.
                    // Polishing takes the frost and the marks away and brings the colour and the gloss up.
                    float bandSharp = smoothstep(0.3 + 0.16 * _Polish, 0.7 - 0.16 * _Polish, sin(bandCoord) * 0.5 + 0.5);
                    float3 bandFull = lerp(_BandA.rgb, _BandB.rgb, bandSharp) * lerp(0.9, 1.07, bandFine);
                    float bandFace = saturate(_BandStrength * 1.3) * smoothstep(0.02, 0.35, c.a);
                    float3 face = lerp(chalc * lerp(0.95, 1.08, grain), bandFull, bandFace);
                    face = lerp(face, rindCol, rindZone * 0.85);
                    face = lerp(face, lerp(_CavityColor.rgb, _CavityCrystalColor.rgb, 0.45), edgeZone * 0.6);
                    face = lerp(ext * 0.9, face, rindT);
                    float marks = 0.5 + 0.5 * sin(c.a * 170.0 + IN.uvFog.x * 9.0 + noise * 4.0);
                    float frost = (1.0 - _Polish) * (0.42 + 0.1 * marks + 0.12 * grain);
                    face = lerp(face, face * 0.45 + 0.5, frost * 0.5);                 // frosted, milky
                    face = lerp(face, face * face * 1.35, _Polish * 0.6);             // polish deepens the colour
                    rim = face;
                    sawnSmooth = lerp(0.22, 0.92, _Polish);
                }

                // cavity wall: matrix colour with faint continuation of the last band
                float band2 = smoothstep(0.3, 0.7, sin(_BandFrequency + _BandOffset * 6.2831 + c.a * 2.0 + (noise - 0.5)) * 0.5 + 0.5);
                float3 cav = lerp(_CavityColor.rgb, lerp(_BandA.rgb, _BandB.rgb, band2), _BandStrength * 0.45) * lerp(0.82, 1.0, rockFine);
                cav *= lerp(0.78, 1.18, saturate(detLum * 1.6));
                cav = lerp(cav, cav * detTint, 0.25);
                // druse floor: the druse tile's mosaic of terminations in the crystal colour (tips bright, bases dark)
                float dz1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xz * 55.0 + IN.positionOS.y * 31.0).a;
                float dz2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.zy * 47.0 + IN.positionOS.x * 29.0).a;
                float dzFacet = saturate(detLum * 1.3);
                // the druse floor sits a shade paler and less saturated than the points above it (tiny crystals scatter more)
                float dzLum = dot(_CavityCrystalColor.rgb, float3(0.3, 0.59, 0.11));
                float3 druzyCol = lerp(_CavityCrystalColor.rgb, float3(dzLum, dzLum, dzLum) * 1.2, 0.3) * lerp(0.5, 1.3, dzFacet) * lerp(0.92, 1.05, dz1);
                cav = lerp(cav, druzyCol, _CavityDruzy * c.g);
                // rock flour from the break: a pale matte powder that lies in the low ground until the rinse
                float dustAmt = saturate(_Dust) * saturate(0.3 + 0.6 * dz2) * lerp(0.7, 1.0, 1.0 - detOcc);
                cav = lerp(cav, float3(0.56, 0.53, 0.49), dustAmt * 0.55);

                // wet: water darkens and saturates the stone and lays a sheen over it; a fresh sawn face carries a film
                // of grey coolant slurry until it dries
                float wet = saturate(_Wet);
                if (sawn) rim = lerp(rim, rim * 0.75 + float3(0.12, 0.12, 0.11), wet * 0.45);
                float3 albedo = ext * c.r + cav * c.g + rim * c.b;
                // water soaks into porous rock (rind, fracture) and only films over a sealed sawn or polished face
                float porosity = c.r * 1.0 + c.g * 0.7 + c.b * (sawn ? 0.3 * (1.0 - _Polish) : 0.85);
                albedo = lerp(albedo, albedo * albedo * 1.35 + albedo * 0.15, wet * 0.65 * porosity);
                float extSmooth = texFam == 1 ? 0.24 : texFam == 3 ? 0.1 : 0.18;
                extSmooth = lerp(extSmooth, lerp(0.05, 0.55, detSmooth), 0.6);              // the tile's roughness map carries the micro variation
                extSmooth = lerp(extSmooth, 0.06, dirtMask) + chipAmt * 0.3;
                // druse scatters (thousands of tiny facets): satin, not gloss, or the floor goes black off the highlight
                float cavSmooth = lerp(lerp(_CavitySmoothness, 0.62, _CavityDruzy), lerp(0.1, 0.7, detSmooth), 0.5) * (1.0 - 0.6 * dustAmt);
                float rimSmooth = sawn ? sawnSmooth : lerp(0.16, lerp(0.05, 0.5, detSmooth), 0.6);
                float smooth = extSmooth * c.r + cavSmooth * c.g + rimSmooth * c.b;
                smooth += (grain - 0.5) * (sawn ? 0.02 : 0.06) + crackFrost * 0.06 * c.r;
                smooth = lerp(smooth, max(smooth, 0.72 + 0.1 * grain), wet);
                // a wet film lies over the pits: the micro relief flattens toward the smooth vertex normal
                N = normalize(lerp(N, normalize(IN.normalWS), wet * 0.55 * porosity));

                if (_GeodeDebug > 0.5)
                {
                    // dev diagnosis: 1 albedo, 2 detail tile albedo, 3 the y-plane projection uv, 4 the triplanar blend weights, 5 object-space normal
                    if (_GeodeDebug < 1.5) return half4(albedo, 1.0);
                    if (_GeodeDebug < 2.5) return half4(detAlbedo, 1.0);
                    if (_GeodeDebug < 3.5) return half4(frac(IN.positionOS.xz * _DetailScale), 0.0, 1.0);
                    if (_GeodeDebug < 4.5) { float3 bwD = pow(abs(nOS), 4.0); bwD /= max(1e-4, bwD.x + bwD.y + bwD.z); return half4(bwD, 1.0); }
                    return half4(nOS * 0.5 + 0.5, 1.0);
                }
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
                s.occlusion = lerp(0.75, 1.0, grain) * lerp(1.0, 0.8, c.g * c.a) * lerp(1.0, detOcc, 0.7);
                s.alpha = 1.0;
                s.normalTS = half3(0, 0, 1);
                float dzSpark = pow(saturate(detLum * 1.25), 10.0) * pow(saturate(dz1 * 1.3), 4.0) * _CavityDruzy * c.g * (1.0 - dustAmt);
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
