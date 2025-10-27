Shader "Hidden/LowHealthVignette"
{
    Properties
    {
        _MainTex ("Base (Screen Texture)", 2D) = "white" {}
        _Intensity ("Intensity", Range(0,1)) = 0
        _Color ("Tint Color", Color) = (1,0,0,1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "PostProcess"
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _MainTex_TexelSize;
            float _Intensity;
            float4 _Color;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 Frag (Varyings i) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center);
                float vignette = smoothstep(0.4, 0.8, dist);

                half4 tinted = lerp(col, lerp(col, _Color, vignette), _Intensity);
                return tinted;
            }
            ENDHLSL
        }
    }
}