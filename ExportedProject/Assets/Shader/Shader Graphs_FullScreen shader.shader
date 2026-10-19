// Replacement for the AssetRipper DummyShaderTextExporter stub of
// "Shader Graphs/FullScreen shader" (multi-ShaderGraph Fullscreen-target effect,
// drawn by URP FullScreenPassRendererFeature instances: FullScreenShader_{Hurt,
// Buffs, Overheated} renderer features at injection point 450, with
// fetchColorBuffer = true and Color|Depth requirements).
//
// URP draws this as a procedural fullscreen triangle (DrawProcedural with an
// identity matrix), so the vertex stage builds the triangle from SV_VertexID
// exactly like the generated Fullscreen ShaderGraph code. The material drives
// the blend/depth states through the serialized _Fullscreen_* floats, which are
// preserved and applied in the pass header. The visual content is rebuilt as
// the base look: a radial vignette mask in _Color (HDR), fading from the screen
// edges with _Vingette_Power/_Vingette_Intensity and _Vingette_Alpha. The
// optional noise/cells (_Voronoi*) "speed-lines" variant part is not
// reproduced, but the property names are kept so materials keep their data.
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
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "RenderPipeline"="UniversalPipeline" }
		LOD 100

		Pass
		{
			Name "FullScreenPass"

			Cull Off
			ZWrite [_Fullscreen_DepthWrite]
			ZTest [_Fullscreen_DepthTest]
			Blend [_Fullscreen_SrcColorBlend] [_Fullscreen_DstColorBlend], [_Fullscreen_SrcAlphaBlend] [_Fullscreen_DstAlphaBlend]
			BlendOp [_Fullscreen_ColorBlendOperation], [_Fullscreen_AlphaBlendOperation]

			HLSLPROGRAM
			#pragma target 4.5
			#pragma vertex vert
			#pragma fragment frag

			float4 _Color;
			float _Vingette_Power;
			float _Vingette_Intensity;
			float _Vingette_Alpha;

			struct VertInput
			{
				uint vertexID : SV_VertexID;
			};

			struct VertOutput
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			// Same fullscreen triangle setup as URP's generated Fullscreen ShaderGraph:
			// the fullscreen pass issues 3 procedural vertices with an identity matrix.
			VertOutput vert(VertInput input)
			{
				VertOutput output;
				float2 v = float2((input.vertexID << 1) & 2, input.vertexID & 2);
				output.pos = float4(v * 2.0 - 1.0, 0.0, 1.0);
				output.uv = v * 0.5 + 0.5;
				return output;
			}

			float4 frag(VertOutput input) : SV_Target
			{
				float2 centeredUV = (input.uv - 0.5) * 2.0;
				// 0 at the screen center, ~1 at the borders (diagonal corners go to ~1.41)
				float d = length(centeredUV);
				float vignette = saturate(pow(saturate(d), _Vingette_Power) * _Vingette_Intensity);
				float alpha = saturate(vignette * _Vingette_Alpha * _Color.a);
				return float4(_Color.rgb, alpha);
			}

			ENDHLSL
		}
	}
	Fallback "Hidden/Shader Graph/FallbackError"
	//CustomEditor "UnityEditor.Rendering.Fullscreen.ShaderGraph.FullscreenShaderGUI"
}
