// Replacement for the AssetRipper DummyShaderTextExporter stub of
// "ProBuilder6/Standard Vertex Color" (ProBuilder's runtime shader; used by
// Resources/materials/ProBuilderDefault.mat on blockout meshes). Recreates the
// original look: opaque unlit surface multiplied by the texture and the vertex
// colors baked on the ProBuilder mesh.
Shader "ProBuilder6/Standard Vertex Color" {
	Properties {
		[NoScaleOffset] _MainTex ("Albedo", 2D) = "white" {}
		[HideInInspector] _Color ("Color", Vector) = (1,1,1,1)
		[HideInInspector] _QueueOffset ("_QueueOffset", Float) = 0
		[HideInInspector] _QueueControl ("_QueueControl", Float) = -1
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
		[HideInInspector] _BUILTIN_QueueOffset ("Float", Float) = 0
		[HideInInspector] _BUILTIN_QueueControl ("Float", Float) = -1
	}
	// DummyShaderTextExporter replacement: opaque unlit, textured + vertex color.
	SubShader
	{
		Tags { "Queue"="Geometry" "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
		LOD 200

		Pass
		{
			Name "ProBuilderFallback"

			Cull Back
			ZWrite On
			ZTest LEqual

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"

			sampler2D _MainTex;
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
				UNITY_FOG_COORDS(1)
			};

			v2f vert(appdata inV)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(inV.vertex);
				o.tex = inV.tex;
				o.color = inV.color * _Color;
				UNITY_TRANSFER_FOG(o, o.pos);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.tex) * i.color;
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}

			ENDCG
		}
	}
	Fallback "Hidden/Shader Graph/FallbackError"
	//CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
}
