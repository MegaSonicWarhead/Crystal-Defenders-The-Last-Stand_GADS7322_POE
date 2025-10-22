Shader "CrystalDefenders/TowerUpgradeGlow_Animated"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (0, 1, 1, 1)
        _GlowStrength ("Glow Strength", Range(0, 5)) = 0
        _PulseSpeed ("Pulse Speed", Range(0.1, 5)) = 1.5
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _BaseColor;
        fixed4 _GlowColor;
        float _GlowStrength;
        float _PulseSpeed;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex) * _BaseColor;
            float pulse = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
            fixed3 glow = _GlowColor.rgb * (_GlowStrength * pulse);
            o.Albedo = tex.rgb + glow;
            o.Alpha = tex.a;
        }
        ENDCG
    }

    FallBack "Diffuse"
}