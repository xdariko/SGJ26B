// Фон ведьминой книги: медленные «разводы краски» (как фон в Балатро, своя реализация).
// Полярная закрутка + несколько шагов искажения координат синусами, затем раскраска в 3 цвета.
// Ставится материалом на Image/RawImage на весь экран.
Shader "PumpkinPost/WitchSwirl"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _ColorDark ("Тёмный (основа)", Color) = (0.09, 0.055, 0.12, 1)
        _ColorMid ("Средний (фиолет)", Color) = (0.23, 0.12, 0.31, 1)
        _ColorAccent ("Акцент (зелье)", Color) = (0.10, 0.25, 0.23, 1)

        _Aspect ("Соотношение сторон", Float) = 1.7778
        _Scale ("Масштаб разводов", Range(1, 30)) = 9
        _Twist ("Закрутка", Range(0, 10)) = 3
        _Speed ("Скорость", Range(0, 2)) = 0.25
        _Contrast ("Контраст", Range(0.5, 4)) = 1.6
        _Shine ("Блик на краях", Range(0, 1)) = 0.12
        _Pixel ("Пикселизация (0 = нет)", Float) = 0

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

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            fixed4 _Color, _ColorDark, _ColorMid, _ColorAccent;
            float _Aspect, _Scale, _Twist, _Speed, _Contrast, _Shine, _Pixel;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            float2 rot(float2 p, float a)
            {
                float s = sin(a), c = cos(a);
                return float2(c * p.x - s * p.y, s * p.x + c * p.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                if (_Pixel > 0)
                    uv = (floor(uv * float2(_Pixel * _Aspect, _Pixel)) + 0.5) / float2(_Pixel * _Aspect, _Pixel);

                float t = _Time.y * _Speed;

                // центр экрана, учёт пропорций
                float2 p = (uv - 0.5) * float2(_Aspect, 1);

                // полярная закрутка: чем дальше от центра, тем сильнее
                float len = length(p);
                float ang = atan2(p.y, p.x) + t * 0.15 - _Twist * len;
                p = len * float2(cos(ang), sin(ang)) * _Scale;

                // искажение координат: каждый шаг сдвигает точку по синусам от предыдущего
                for (int k = 0; k < 5; k++)
                {
                    float fk = k;
                    p += 0.55 * float2(sin(p.y * 0.83 + t + fk * 1.7), cos(p.x * 0.97 - t * 0.7 + fk * 2.3));
                    p = rot(p, 0.37) * 1.02;
                }

                // полосы краски
                float v = 0.5 + 0.5 * sin(p.x * 0.45 + p.y * 0.31);
                float w = 0.5 + 0.5 * sin(length(p) * 0.22 - t * 0.5);

                float midW = saturate((v - 0.35) * _Contrast + 0.5);
                float accW = saturate((w * v - 0.45) * _Contrast * 1.5);
                float3 col = lerp(_ColorDark.rgb, _ColorMid.rgb, midW);
                col = lerp(col, _ColorAccent.rgb, accW);

                // тонкий блик на границе цветов, как мазок краски
                float edge = 1 - abs(midW - 0.5) * 2;
                col += _Shine * edge * edge * 0.25;

                // лёгкая виньетка
                float2 d = i.uv - 0.5;
                col *= 1 - dot(d, d) * 0.6;

                return fixed4(col, 1) * i.color;
            }
            ENDCG
        }
    }
}
