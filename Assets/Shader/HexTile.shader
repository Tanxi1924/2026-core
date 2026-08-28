// 程序化正六边形格子着色器（URP，pointy-top）
// HexSDF 在 [-1,1]×[-1,1] UV 空间中定义圆半径=1 的正六边形：
//   所有顶点距中心距离相等（=1），宽高比 = √3 : 2 ≈ 0.866 : 1
//   UV 直接对应 1×1 世界单位 Sprite，无需长宽比修正。
Shader "Custom/HexTile"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        _BorderColor    ("Border Color",       Color)              = (0.08, 0.08, 0.08, 1)
        _BorderWidth    ("Border Width",       Range(0, 0.15))     = 0.05
        _Feather        ("Edge Feather",       Range(0.001, 0.05)) = 0.012
        _DiagThreshold  ("Diagonal Inset",     Range(1.0, 2.0))    = 2.0
    }

    SubShader
    {
        Tags
        {
            "Queue"          = "Transparent"
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BorderColor;
                float  _BorderWidth;
                float  _Feather;
                float  _DiagThreshold;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color;
                return OUT;
            }

            // Pointy-top 正六边形 SDF，p ∈ [-1,1]×[-1,1]
            // 六个顶点：(0,±1)、(±√3/2, ±1/2)，所有顶点到原点距离 = 1
            // 三条对称约束（折叠到第一象限）：
            //   |y| ≤ 1            — 顶/底水平边
            //   |x| ≤ √3/2         — 左/右竖直边
            //   √3|x| + |y| ≤ 2    — 四条斜边
            float HexSDF(float2 p)
            {
                p = abs(p);
                return max(max(p.y - 1.0,
                               p.x - 0.866025),
                           1.732051 * p.x + p.y - _DiagThreshold);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // UV [0,1] → [-1,1]，旋转 90°（pointy-top → flat-top）
                float2 p = IN.uv * 2.0 - 1.0;
                p = float2(-p.y, p.x);

                float d = HexSDF(p);

                // 六边形外侧透明
                float alpha = 1.0 - smoothstep(-_Feather, _Feather, d);
                clip(alpha - 0.001);

                // 内侧描边
                float border = 1.0 - smoothstep(-_BorderWidth - _Feather, -_BorderWidth, d);
                float4 col   = lerp(IN.color, _BorderColor, border * _BorderColor.a);
                col.a = IN.color.a * alpha;

                return col;
            }
            ENDHLSL
        }
    }
}
