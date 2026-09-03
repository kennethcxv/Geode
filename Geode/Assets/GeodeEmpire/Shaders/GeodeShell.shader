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
        CBUFFER_END
        // fracture overlay arrays: kept outside the per-material block so property-block arrays reach them
        float _SectorCrack[16];         // seam stress per sector, >= 1 is an open crack
        float4 _Impacts[32];            // chisel marks: longitude fraction, signed latitude fraction, radius (m), strength
        float _LoupeBoost;              // global: 1 while the player looks through the loupe
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
            void FractureOverlay(float2 uv2, float grain, out float dark, out float frost, out float guide)
            {
                dark = 0.0; frost = 0.0; guide = 0.0;
                float lonF = uv2.x;
                float latF = uv2.y;
                float R = max(0.01, _SurfR);
                float mPerLat = 1.5708 * R;
                float mPerLon = 6.2832 * R * max(0.2, cos(latF * 1.5708));

                float seamLat = (Noise1(lonF * 7.0, 0.31) - 0.5) * 0.09 + (Noise1(lonF * 29.0, 0.77) - 0.5) * 0.03;
                float dSeam = abs(latF - seamLat) * mPerLat;
                int sector = (int)floor(frac(lonF) * 16.0);
                float st = _SectorCrack[sector];
                float cracked = smoothstep(0.82, 1.0, st);
                float hair = smoothstep(0.3, 0.82, st);
                float widthNoise = Noise1(lonF * 53.0, 0.12);
                float halfW = lerp(0.0006, 0.0024, cracked) * lerp(0.6, 1.4, widthNoise);
                float seamLine = 1.0 - smoothstep(halfW * 0.5, halfW * 1.6, dSeam);
                float dots = smoothstep(0.38, 0.62, Noise1(lonF * 90.0, 0.55));
                float seamA = seamLine * (cracked + hair * (1.0 - cracked) * dots * 0.8);
                float lip = (1.0 - smoothstep(halfW * 1.6, halfW * 4.0, dSeam)) * cracked * 0.6 * widthNoise;
                // the natural seam: a soft, slightly darker weathered band a real geode shows, clearer under the lamp
                float gNoise = Noise1(lonF * 17.0, 0.66);
                guide = (1.0 - smoothstep(0.0012, 0.0032 + 0.0015 * gNoise, dSeam)) * min(1.0, _SeamVisible + 0.5 * _LoupeBoost) * (1.0 - cracked) * (0.45 + 0.3 * gNoise);
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
                // micro relief: bend the normal by the detail height gradient so the exterior stops reading as smooth
                // clay (screen-space derivatives of the height, scaled by the surface's own tangent frame)
                if (c.r > 0.5)
                {
                    float h = rock * 0.6 + rockFine * 0.4;
                    float3 dpx = ddx(IN.positionWS), dpy = ddy(IN.positionWS);
                    float dhx = ddx(h), dhy = ddy(h);
                    float3 tx = dpx - N * dot(dpx, N);
                    float3 ty = dpy - N * dot(dpy, N);
                    float lx = max(1e-5, dot(tx, tx)), ly = max(1e-5, dot(ty, ty));
                    float3 grad = tx * (dhx / lx) + ty * (dhy / ly);
                    float bump = 0.0035 * (1.0 - 0.5 * _Weathering);
                    int texFamB = (int)(_TexFamily + 0.5);
                    bump *= texFamB == 1 ? 0.45 : texFamB == 3 ? 1.6 : texFamB == 0 ? 1.25 : 1.0;
                    N = normalize(N - grad * bump);
                }
                float noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xz * 2.7 + IN.positionOS.y * 1.3).b;

                // exterior: two-tone rock with dirt in crevices + optional exposed mineral hint
                int texFam = (int)(_TexFamily + 0.5);
                float grainX = grain;
                if (texFam == 0) grainX = saturate((grain - 0.5) * 1.35 + 0.5);            // coarse matrix: harder grain contrast
                else if (texFam == 1) grainX = saturate((grain - 0.5) * 0.55 + 0.55);      // weathered rind: soft, even, a little pale
                float3 ext = lerp(_RockColor2.rgb, _RockColor.rgb, grainX);
                ext = lerp(ext, ext * 0.55, _Weathering * (1.0 - grain) * 0.6);
                if (texFam == 1)
                {
                    // weathered rind: bleached skin with fine pitting
                    ext = lerp(ext, ext * float3(1.12, 1.08, 1.0) + 0.05, 0.5);
                    float pit = smoothstep(0.66, 0.74, SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xz * 33.0 + IN.positionOS.y * 21.0).g);
                    ext = lerp(ext, ext * 0.6, pit * 0.7);
                }
                else if (texFam == 2)
                {
                    // layered skin: faint growth layers wrapping the rock along its latitude, broken up by the grain
                    float layer = sin(IN.uv2.y * 34.0 + (noise - 0.5) * 4.0 + IN.uv2.x * 3.0) * 0.5 + 0.5;
                    float lay = smoothstep(0.3, 0.7, layer) * smoothstep(0.25, 0.6, grain + noise * 0.3);
                    ext = lerp(ext * 0.9, ext * 1.08 + 0.015, lay);
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
                float stainN2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.zy * 9.0 + IN.positionOS.x * 5.0).g;
                float stainMask = _Stain * smoothstep(0.48, 0.78, stainN * 0.7 + stainN2 * 0.3 + (1.0 - grain) * 0.12);
                float3 rust = lerp(float3(0.5, 0.3, 0.16), float3(0.36, 0.2, 0.1), 1.0 - grain);
                ext = lerp(ext, lerp(ext, rust, 0.75), stainMask);
                // exposed mineral: a faint hint at arm's length; under the loupe the veins and a speckle of tiny
                // exposed crystals in the mineral's colour come up (still only what is on the outside)
                float hintAmt = _HintAmount * (1.0 + 1.6 * _LoupeBoost);
                float hintMask = smoothstep(0.58 - 0.08 * _LoupeBoost, 0.72, noise) * saturate(hintAmt);
                ext = lerp(ext, _HintColor.rgb * lerp(0.8, 1.0, grain), hintMask);
                float speck = pow(saturate(SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xy * 61.0 + IN.positionOS.z * 37.0).g), 9.0);
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
                if ((c.r > 0.5 || c.b > 0.5) && !sawn) FractureOverlay(IN.uv2, grain, crackDark, crackFrost, seamGuide);
                float3 frostCol = lerp(ext, float3(0.86, 0.84, 0.79) * lerp(0.85, 1.0, grain), 0.62);
                ext = lerp(ext, frostCol, crackFrost * 0.85);
                // clay coating: sits in the low grain first and leaves the high points as it is scrubbed away; while
                // it is on, it hides the seam, the staining and the mineral hints the shell would otherwise give away
                float dirtN = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xz * 6.5 + IN.positionOS.y * 4.0).r;
                float dirtFine = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xy * 23.0 + IN.positionOS.z * 17.0).b;
                float dirtMask = _Dirt > 0.001 ? smoothstep(0.05, 0.4, _Dirt * 1.2 - (grain * 0.6 + dirtN * 0.35 + dirtFine * 0.15) + 0.3) : 0.0;
                // dried quarry clay: ochre-brown, caked thick in the hollows and thin and dusty over the high points
                float3 clay = lerp(float3(0.3, 0.23, 0.15), float3(0.42, 0.34, 0.23), grain) * lerp(0.82, 1.1, dirtN) * lerp(0.88, 1.06, dirtFine);
                clay = lerp(clay, clay * 0.7, smoothstep(0.62, 0.72, dirtFine));       // cracked, crumbly patches
                float3 dust = lerp(ext, float3(0.5, 0.43, 0.33), 0.45);
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
                    float kerf = (1.0 - smoothstep(0.0014, 0.0022, dPlane)) * step(along, _CutFeed.w) * _CutShow;
                    ext = lerp(ext, float3(0.95, 0.92, 0.8), guide * 0.85);
                    ext = lerp(ext, float3(0.08, 0.075, 0.07), kerf);
                }

                // cut face: rind on the outside, bands toward the cavity
                float bandCoord = c.a * _BandFrequency + _BandOffset * 6.2831 + (noise - 0.5) * 1.6;
                float band = smoothstep(0.3, 0.7, sin(bandCoord) * 0.5 + 0.5);
                float3 bandCol = lerp(_BandA.rgb, _BandB.rgb, band);
                float bandMask = saturate(_BandStrength * 1.2) * smoothstep(lerp(0.78, 0.12, _BandStrength), lerp(0.96, 0.45, _BandStrength), c.a);
                float3 rim = lerp(_RimColor.rgb * lerp(0.8, 1.1, grain), bandCol * lerp(0.85, 1.05, rockFine), bandMask);
                // chips torn out of the rim by the chisel: pale bruised patches with dark edges on the cut face
                rim = lerp(rim, rim * float3(0.9, 0.88, 0.85) + 0.12, crackFrost * 0.6);
                rim = lerp(rim, rim * 0.35, crackDark * 0.8);
                float sawnSmooth = 0.0;
                if (sawn)
                {
                    // a saw leaves a flat, slightly frosted face with faint arc marks; the bands show fully but dull.
                    // Polishing takes the frost and the marks away and brings the colour and the gloss up.
                    float3 bandFull = lerp(_BandA.rgb, _BandB.rgb, band);
                    float bandFace = saturate(_BandStrength * 1.3) * smoothstep(0.02, 0.35, c.a);
                    float3 face = lerp(_RimColor.rgb * lerp(0.95, 1.12, grain), bandFull, bandFace);
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
                // druzy floor: fine crystalline glitter in the crystal colour hides bare matrix under carpets
                float dz1 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.xz * 55.0 + IN.positionOS.y * 31.0).g;
                float dz2 = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.positionOS.zy * 47.0 + IN.positionOS.x * 29.0).g;
                float dzFacet = saturate(dz1 * 0.6 + dz2 * 0.6);
                float3 druzyCol = _CavityCrystalColor.rgb * lerp(0.55, 1.15, dzFacet);
                cav = lerp(cav, druzyCol, _CavityDruzy * c.g);

                float3 albedo = ext * c.r + cav * c.g + rim * c.b;
                float extSmooth = texFam == 1 ? 0.24 : texFam == 3 ? 0.1 : 0.18;
                extSmooth = lerp(extSmooth, 0.06, dirtMask) + chipAmt * 0.3;
                float smooth = extSmooth * c.r + lerp(_CavitySmoothness, 0.75, _CavityDruzy) * c.g + (sawn ? sawnSmooth : 0.16) * c.b;
                smooth += (grain - 0.5) * (sawn ? 0.02 : 0.1) + crackFrost * 0.06 * c.r;

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
