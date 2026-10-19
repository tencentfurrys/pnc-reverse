// Replacement for the AssetRipper DummyShaderTextExporter stub of
// "Hidden/ALINE/Surface" (Arsonade's ALINE debug-visual asset). Debug décals
// quads drawn by ALINE additionally sample a texture; the stub body was opaque,
// which corrupted the debug overlay into black fills.
Shader "Hidden/ALINE/Surface" {
	Properties {
		_Color ("Main Color", Vector) = (1,1,1,0.5)
		_MainTex ("Texture", 2D) = "white" {}
		_Scale ("Scale", Float) = 1
		_FadeColor ("Fade Color", Vector) = (1,1,1,0.3)
	}
	// DummyShaderTextExporter replacement: transparent unlit textured & vertex-colored.
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
				return tex2D(_MainTex, i.tex) * i.color;
			}

			ENDCG
		}
	}
}
