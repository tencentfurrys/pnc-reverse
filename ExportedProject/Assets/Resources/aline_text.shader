// Replacement for the AssetRipper DummyShaderTextExporter stub of
// "Hidden/ALINE/Font" (Arsonade's ALINE debug-visual asset, used for volume
// labels drawn in debug view). Text glyphs are transparent vertex-colored
// quads sampling a font atlas; the stub body was opaque.
Shader "Hidden/ALINE/Font" {
	Properties {
		_Color ("Main Color", Vector) = (1,1,1,0.5)
		_FadeColor ("Fade Color", Vector) = (1,1,1,0.3)
		_MainTex ("Texture", 2D) = "white" {}
		_FallbackTex ("Fallback Texture", 2D) = "white" {}
		_FallbackAmount ("Fallback Amount", Range(0, 1)) = 1
		_TransitionPoint ("Transition Point", Range(0, 5)) = 0.6
		_MipBias ("Mip Bias", Range(-2, 0)) = -1
		_GammaCorrection ("Gamma Correction", Range(0, 2)) = 1
	}
	// DummyShaderTextExporter replacement: transparent unlit, atlas sampled with
	// alpha-based glyph mask and vertex color multiplier.
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
		LOD 200
		ZWrite Off
		ZTest Always
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 tex : TEXCOORD0;
				float4 color : COLOR0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 tex : TEXCOORD0;
				fixed4 color : COLOR0;
			};

			v2f vert(appdata inV)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(inV.vertex);
				o.tex = TRANSFORM_TEX(inV.tex, _MainTex);
				o.color = inV.color * _Color;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 tex = tex2D(_MainTex, i.tex);
				fixed4 col = i.color;
				// Glyphs encode coverage in the RGB channels of the atlas
				col.rgb = lerp(col.rgb, col.rgb * tex.rgb, tex.a);
				col.a *= tex.a;
				return col;
			}

			ENDCG
		}
	}
}
