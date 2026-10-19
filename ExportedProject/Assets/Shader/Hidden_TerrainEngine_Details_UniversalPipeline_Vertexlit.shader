Shader "Hidden/TerrainEngine/Details/UniversalPipeline/Vertexlit" {
Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {  }
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
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
                    return t;
                }
                ENDHLSL
            }
        }
        Fallback Off
}
