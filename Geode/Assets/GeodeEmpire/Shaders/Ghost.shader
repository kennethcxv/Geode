Shader "GeodeEmpire/Ghost"
{
    // Build mode's placement volume: a flat translucent colour that reads over any surface and never writes depth.
    Properties
    {
        _BaseColor ("Colour", Color) = (0.3, 0.95, 0.42, 0.3)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Ghost"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionHCS : SV_POSITION; float fresnel : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(IN.normalOS);
                o.positionHCS = p.positionCS;
                float3 v = normalize(GetWorldSpaceViewDir(p.positionWS));
                // edges read stronger than faces, so the volume looks like a marked-out footprint rather than a solid block
                o.fresnel = saturate(1.0 - abs(dot(n.normalWS, v)));
                return o;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half a = _BaseColor.a * (0.55 + 0.85 * IN.fresnel);
                return half4(_BaseColor.rgb, saturate(a));
            }
            ENDHLSL
        }
    }
}
