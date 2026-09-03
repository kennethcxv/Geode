Shader "GeodeEmpire/LoupeLens"
{
    // The lens of the hand loupe: shows the opaque scene behind it magnified about the lens centre, with a little
    // chromatic fringe and a darkened edge so it reads as thick glass. Needs the camera opaque texture.
    Properties
    {
        _Magnify("Magnification", Float) = 2.2
        _LensCenter("Lens Centre (viewport)", Vector) = (0.5, 0.5, 0, 0)
        _LensCenterOS("Lens Centre (object)", Vector) = (0, 0.112, -0.007, 0)
        _LensRadius("Lens Radius (object)", Float) = 0.0168
        _Tint("Tint", Color) = (0.94, 0.97, 1.0, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Lens"
            Tags { "LightMode" = "UniversalForward" }
            Blend One Zero
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Magnify;
                float4 _LensCenter;
                float4 _LensCenterOS;
                float _LensRadius;
                float4 _Tint;
            CBUFFER_END

            struct A { float4 positionOS : POSITION; };
            struct V { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; };

            V Vert(A IN)
            {
                V OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            half4 Frag(V IN) : SV_Target
            {
                float2 uv = GetNormalizedScreenSpaceUV(IN.positionCS);
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 c = _LensCenter.xy;
                float2 d = uv - c;
                d.x *= aspect;
                float2 rel = IN.positionOS.xy - _LensCenterOS.xy;
                float r = saturate(length(rel) / max(1e-4, _LensRadius));
                // barrel: a little more magnification in the middle than at the edge, like a real lens
                float mag = _Magnify * (1.0 - 0.18 * r * r);
                float2 m = d / mag;
                float fringe = 0.006 * r * r;
                float2 uvR = c + (m * (1.0 + fringe)) / float2(aspect, 1.0);
                float2 uvG = c + m / float2(aspect, 1.0);
                float2 uvB = c + (m * (1.0 - fringe)) / float2(aspect, 1.0);
                float3 col;
                col.r = SampleSceneColor(saturate(uvR)).r;
                col.g = SampleSceneColor(saturate(uvG)).g;
                col.b = SampleSceneColor(saturate(uvB)).b;
                col *= _Tint.rgb;
                // glass: edge darkening, a soft ring of reflection near the rim
                col *= 1.0 - smoothstep(0.82, 1.0, r) * 0.55;
                col += smoothstep(0.9, 0.97, r) * (1.0 - smoothstep(0.97, 1.0, r)) * 0.18;
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
