Shader "CrystalDefenders/PlacementPreview"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0, 1, 0, 0.5) // Green, semi-transparent
        _GlowColor("Glow Color", Color) = (0, 1, 0, 1)
        _GlowStrength("Glow Strength", Range(0, 5)) = 1.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : NORMAL;
            };

            fixed4 _BaseColor;
            fixed4 _GlowColor;
            float _GlowStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = normalize(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Simple glow based on normal facing up
                float glow = saturate(i.normal.y) * _GlowStrength;
                fixed4 col = _BaseColor + _GlowColor * glow;
                col.a = _BaseColor.a; // keep semi-transparent
                return col;
            }
            ENDCG
        }
    }
}