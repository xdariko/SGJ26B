// CCTV-экран для RawImage: рыбий глаз, хроматическая аберрация, тон, скан-линии, шум, виньетка, помехи.
// Формула искажения продублирована в CctvMath.Distort (для кликов) — меняешь здесь, меняй и там.
Shader "PumpkinPost/CCTV"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _CamTint ("Camera Tint", Color) = (1, 0.82, 0.6, 1)
        _Distortion ("Fisheye", Range(0, 0.5)) = 0.12
        _Aberration ("Chromatic Aberration", Range(0, 0.02)) = 0.004
        _Scanlines ("Scanlines", Range(0, 1)) = 0.25
        _ScanlineCount ("Scanline Count", Float) = 180
        _Noise ("Noise", Range(0, 1)) = 0.35
        _Vignette ("Vignette", Range(0, 2)) = 0.9
        _Glitch ("Glitch", Range(0, 1)) = 0
        _RollBar ("Rolling Bar", Range(0, 1)) = 0.15
        _Brightness ("Brightness", Range(0, 3)) = 1.15

        // UGUI маски
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _CamTint;
            float _Distortion, _Aberration, _Scanlines, _ScanlineCount, _Noise, _Vignette, _Glitch, _RollBar, _Brightness;

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y;

                // Рыбий глаз
                float2 d = i.uv * 2 - 1;
                float r2 = dot(d, d);
                float2 uv = (d * (1 + _Distortion * r2)) * 0.5 + 0.5;

                // Помехи: строки съезжают вбок
                float band = floor(uv.y * 40) + floor(t * 24);
                uv.x += (hash(float2(band, 1)) - 0.5) * _Glitch * 0.12;

                float inside = step(0, uv.x) * step(uv.x, 1) * step(0, uv.y) * step(uv.y, 1);

                float ab = _Aberration * (1 + r2) + _Glitch * 0.01;
                fixed3 col;
                col.r = tex2D(_MainTex, uv + float2(ab, 0)).r;
                col.g = tex2D(_MainTex, uv).g;
                col.b = tex2D(_MainTex, uv - float2(ab, 0)).b;

                // Тон камеры (тёплый свет тыквы), чуть обесцвечиваем
                float luma = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(col, luma.xxx, 0.25) * _CamTint.rgb * _Brightness;

                // Скан-линии и бегущая полоса
                float scan = 0.5 + 0.5 * sin(uv.y * _ScanlineCount * 6.2832);
                col *= 1 - _Scanlines * scan * 0.5;
                float roll = frac(uv.y - t * 0.15);
                col += _RollBar * smoothstep(0.0, 0.08, roll) * smoothstep(0.16, 0.08, roll) * 0.25;

                // Шум
                float n = hash(floor(i.uv * float2(320, 180)) + frac(t * 7.13) * 100);
                col += (n - 0.5) * (_Noise * 0.25 + _Glitch * 0.5);

                // Виньетка
                col *= saturate(1 - _Vignette * r2 * 0.5);

                return fixed4(col * inside, i.color.a) * fixed4(i.color.rgb, 1);
            }
            ENDCG
        }
    }
}
