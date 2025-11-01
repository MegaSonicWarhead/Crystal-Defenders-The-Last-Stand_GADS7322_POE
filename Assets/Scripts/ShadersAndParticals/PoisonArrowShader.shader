Shader "CrystalDefenders/PoisonArrowShader"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _PoisonColor ("Poison Color", Color) = (0,1,0,1)
        _PoisonStrength ("Poison Strength", Range(0,1)) = 0
        _GlowIntensity ("Glow Intensity", Range(0,5)) = 1.5
        _CloudSpeed ("Cloud Speed", Range(0,5)) = 1.0
        _CloudScale ("Cloud Scale", Range(0.1,10)) = 3.0
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
                float2 uv : TEXCOORD2;
            };

            fixed4 _BaseColor;
            fixed4 _PoisonColor;
            float _PoisonStrength;
            float _GlowIntensity;
            float _CloudSpeed;
            float _CloudScale;

            // Simple pseudo-random function for cloud noise
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy, float2(12.9898,78.233))) * 43758.5453);
            }

            // 2D noise function
            float noise(float2 p)
            {
                float2 ip = floor(p);
                float2 u = frac(p);
                u = u*u*(3.0-2.0*u);

                float res = lerp(
                    lerp(rand(ip), rand(ip + float2(1.0,0.0)), u.x),
                    lerp(rand(ip + float2(0.0,1.0)), rand(ip + float2(1.0,1.0)), u.x),
                    u.y);
                return res;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
                o.uv = v.uv;
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
                fixed3 glow = baseColor.rgb * glowFactor;

                // CLOUD EFFECT
                float2 cloudUV = i.uv * _CloudScale;
                cloudUV += _Time.y * _CloudSpeed; // use built-in _Time
                float cloud = noise(cloudUV);

                // Mix cloud as extra intensity for poison color
                float cloudIntensity = cloud * _PoisonStrength;
                fixed3 cloudColor = baseColor.rgb * cloudIntensity;

                // Final color with emission and cloud added
                fixed3 finalColor = diffuse + glow + cloudColor;

                return fixed4(finalColor, baseColor.a);
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}