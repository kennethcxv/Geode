Shader "GeodeEmpire/CrackLine"
{
    Properties
    {
        _Color("Colour", Color) = (0.05, 0.04, 0.03, 1)
        _NoiseTex("Noise", 2D) = "gray" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _NoiseTex_ST;
            CBUFFER_END
            TEXTURE2D(_NoiseTex); SAMPLER(sampler_NoiseTex);

            struct A { float4 positionOS : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float4 color : TEXCOORD0; float2 uv : TEXCOORD1; };

            V Vert(A IN)
            {
                V OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.color = IN.color;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 Frag(V IN) : SV_Target
            {
                float n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, IN.uv * float2(6.0, 1.0)).r;
                // jagged edge: fade across the strip width with noise, strongest in the middle
                float w = 1.0 - abs(IN.uv.y * 2.0 - 1.0);
                float edge = smoothstep(0.15, 0.6, w + (n - 0.5) * 0.6);
                float a = IN.color.a * edge;
                return half4(_Color.rgb * IN.color.rgb, a);
            }
            ENDHLSL
        }
    }
}
