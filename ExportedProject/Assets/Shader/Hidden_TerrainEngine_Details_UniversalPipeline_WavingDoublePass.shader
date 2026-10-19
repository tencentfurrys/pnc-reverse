Shader "Hidden/TerrainEngine/Details/UniversalPipeline/WavingDoublePass" {
Properties
    {
        _WavingTint ("Fade Color", Color) = (.7,.6,.5, 0)
        _MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
        _WaveAndDistance ("Wave and distance", Vector) = (12, 3.6, 1, 1)
        _Cutoff ("Cutoff", float) = 0.5
    }
        SubShader
        {
            Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
            Pass
            {
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
                float4 _MainTex_ST;
                float _Cutoff;

                struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
                struct Varyings { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

                Varyings vert(Attributes IN)
                {
                    Varyings OUT;
                    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                    OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                    return OUT;
                }

                half4 frag(Varyings IN) : SV_Target
                {
                    half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                    clip(t.a - _Cutoff);
                    return t;
                }
                ENDHLSL
            }
        }
        Fallback Off
}
