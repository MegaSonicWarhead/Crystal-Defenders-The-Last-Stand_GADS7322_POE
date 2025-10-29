Shader "CrystalDefenders/TerrainShockwave"
{
    Properties
    {
        _MainTex("Base Texture", 2D) = "white" {}
        _ShockwaveCenter("Shockwave Center", Vector) = (0,0,0,0)
        _ShockwaveRadius("Shockwave Radius", Float) = 0
        _ShockwaveStrength("Shockwave Strength", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        float4 _ShockwaveCenter;
        float _ShockwaveRadius;
        float _ShockwaveStrength;

        struct Input
        {
            float2 uv_MainTex;
        };

        // Vertex function: displace vertices based on shockwave radius
        void vert(inout appdata_full v)
        {
            if (_ShockwaveStrength <= 0.001) return;

            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float dist = distance(worldPos.xz, _ShockwaveCenter.xz);

            if (dist < _ShockwaveRadius)
            {
                float t = 1.0 - saturate(dist / _ShockwaveRadius);
                float displacement = sin(t * 3.1415) * _ShockwaveStrength;
                v.vertex.y += displacement;
            }
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            o.Metallic = 0;
            o.Smoothness = 0.2;
        }
        ENDCG
    }

    FallBack "Diffuse"
}