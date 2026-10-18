Shader "Shader Graphs/FullScreen shader" {
	Properties {
		[HDR] _Color ("Color", Vector) = (1,0,0,1)
		_Vingette_Power ("Vingette Power", Float) = 5
		_Vingette_Intensity ("Vingette Intensity", Float) = 2
		_Vingette_Alpha ("Vingette Alpha", Float) = 1
		_VoronoiSpeed ("VoronoiSpeed", Range(0, 10)) = 2
		_VoronoiCellDensity ("VoronoiCellDensity", Range(0, 200)) = 5
		[ToggleUI] _Voronoi ("Voronoi?", Float) = 1
		[ToggleUI] _VoronoiTime ("VoronoiTime", Float) = 0
		[ToggleUI] _Cells ("Cells?", Float) = 0
		_NoiseSpeed ("NoiseSpeed", Vector) = (0,0,0,0)
		[ToggleUI] _Tiling_And_offset_movement ("Tiling And offset movement", Float) = 0
		[Enum(UnityEditor.Rendering.Fullscreen.ShaderGraph.FullscreenBlendMode)] [HideInInspector] _Fullscreen_BlendMode ("Float", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] [HideInInspector] _Fullscreen_SrcColorBlend ("Float", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] [HideInInspector] _Fullscreen_DstColorBlend ("Float", Float) = 10
		[Enum(UnityEngine.Rendering.BlendMode)] [HideInInspector] _Fullscreen_SrcAlphaBlend ("Float", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] [HideInInspector] _Fullscreen_DstAlphaBlend ("Float", Float) = 10
		[Enum(UnityEditor.ShaderGraph.BlendOp)] [HideInInspector] _Fullscreen_ColorBlendOperation ("Float", Float) = 0
		[Enum(UnityEditor.ShaderGraph.BlendOp)] [HideInInspector] _Fullscreen_AlphaBlendOperation ("Float", Float) = 0
		[ToggleUI] [HideInInspector] _Fullscreen_DepthWrite ("Boolean", Float) = 0
		[Enum(UnityEngine.Rendering.CompareFunction)] [HideInInspector] _Fullscreen_DepthTest ("Float", Float) = 8
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
	}
	//DummyShaderTextExporter
	SubShader{
		Tags { "RenderType"="Opaque" }
		LOD 200

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4x4 unity_ObjectToWorld;
			float4x4 unity_MatrixVP;

			struct Vertex_Stage_Input
			{
				float4 pos : POSITION;
			};

			struct Vertex_Stage_Output
			{
				float4 pos : SV_POSITION;
			};

			Vertex_Stage_Output vert(Vertex_Stage_Input input)
			{
				Vertex_Stage_Output output;
				output.pos = mul(unity_MatrixVP, mul(unity_ObjectToWorld, input.pos));
				return output;
			}

			float4 _Color;

			float4 frag(Vertex_Stage_Output input) : SV_TARGET
			{
				return _Color; // RGBA
			}

			ENDHLSL
		}
	}
	Fallback "Hidden/Shader Graph/FallbackError"
	//CustomEditor "UnityEditor.Rendering.Fullscreen.ShaderGraph.FullscreenShaderGUI"
}