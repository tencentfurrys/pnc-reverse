// Replacement for the AssetRipper DummyShaderTextExporter stub of
// "Hidden/ALINE/Outline" (Arsonade's ALINE debug-visual asset; the package's
// editor sources are not shipped, so this is a minimal equivalent pass). ALINE
// draws its line meshes as transparent vertex-colored geometry; the stub body
// was opaque with no vertex color, which corrupted the debug overlay into a
// black fill.
Shader "Hidden/ALINE/Outline" {
	Properties {
		_Color ("Main Color", Vector) = (1,1,1,0.5)
		_FadeColor ("Fade Color", Vector) = (1,1,1,0.3)
		_PixelWidth ("Width (px)", Float) = 4
		_LengthPadding ("Length Padding (px)", Float) = 0
	}
	// DummyShaderTextExporter replacement: transparent unlit vertex-colored lines.
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

			fixed4 _Color;

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				fixed4 color : COLOR0;
			};

			v2f vert(appdata inV)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(inV.vertex);
				o.color = inV.color * _Color;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return i.color;
			}

			ENDCG
		}
	}
}
