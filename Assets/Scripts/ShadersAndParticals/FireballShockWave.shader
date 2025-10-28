Shader "CrystalDefenders/FireballShockWave"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,0.5,0,1)
        _ImpactPosition ("Impact Position", Vector) = (0,0,0,0)
        _Radius ("Effect Radius", Float) = 3.0
        _Height ("Max Displacement", Float) = 1.0
        _Falloff ("Falloff", Float) = 2.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
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
                float2 uv : TEXCOORD0;
            };

            float4 _ImpactPosition;
            float _Radius;
            float _Height;
            float _Falloff;
            float4 _BaseColor;

            v2f vert (appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                // Compute distance to impact
                float dist = distance(worldPos.xz, _ImpactPosition.xz);

                // Only affect vertices within radius
                float displacement = 0;
                if (dist < _Radius)
                {
                    float factor = pow(1.0 - (dist / _Radius), _Falloff);
                    displacement = factor * _Height;
                }

                worldPos.y += displacement;

                o.pos = UnityObjectToClipPos(float4(worldPos,1.0));
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _BaseColor;
            }
            ENDCG
        }
    }
}