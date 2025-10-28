Shader "CrystalDefenders/PoisonArrowShader"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _PoisonColor ("Poison Color", Color) = (0,1,0,1)
        _PoisonStrength ("Poison Strength", Range(0,1)) = 0
        _GlowIntensity ("Glow Intensity", Range(0,5)) = 1.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Tags { "LightMode"="ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            fixed4 _BaseColor;
            fixed4 _PoisonColor;
            float _PoisonStrength;
            float _GlowIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Simple lambert lighting factor
                float3 N = normalize(i.worldNormal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = saturate(dot(N, L));

                // Mix poison color based on poison strength
                fixed4 baseColor = lerp(_BaseColor, _PoisonColor, _PoisonStrength);

                // Basic diffuse light
                fixed3 diffuse = baseColor.rgb * NdotL;

                // Glow intensity rises with poison strength
                float glowFactor = _PoisonStrength * _GlowIntensity;

                // Add emissive glow (bright poison aura)
                fixed3 glow = baseColor.rgb * glowFactor;

                // Final color with emission added
                fixed3 finalColor = diffuse + glow;

                return fixed4(finalColor, baseColor.a);
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}