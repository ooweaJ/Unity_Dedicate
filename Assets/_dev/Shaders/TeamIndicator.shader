Shader "Custom/TeamIndicator"
{
    Properties
    {
        _Color       ("Color",        Color)        = (1,1,1,1)
        _InnerRadius ("Inner Radius", Range(0,0.95)) = 0.5
        _SoftEdge    ("Soft Edge",    Range(0,0.2))  = 0.06
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector"= "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest  LEqual
        Cull   Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 posOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 posHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _InnerRadius;
                float _SoftEdge;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.posHCS = TransformObjectToHClip(IN.posOS.xyz);
                OUT.uv     = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // UV → 중심 기준 거리 (0 = 중심, 1 = 가장자리)
                float2 c    = IN.uv * 2.0 - 1.0;
                float  dist = length(c);

                // 바깥쪽: 1.0 근처에서 부드럽게 사라짐
                float outer = 1.0 - smoothstep(1.0 - _SoftEdge, 1.0, dist);
                // 안쪽: _InnerRadius 안쪽으로 갈수록 투명 → 링 모양
                float inner = smoothstep(_InnerRadius - _SoftEdge, _InnerRadius, dist);

                float alpha = outer * inner * _Color.a;
                clip(alpha - 0.01);

                return half4(_Color.rgb, alpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
