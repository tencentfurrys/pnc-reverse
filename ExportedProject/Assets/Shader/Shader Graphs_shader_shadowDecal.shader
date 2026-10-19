// Replacement for the AssetRipper DummyShaderTextExporter stub of
// "Shader Graphs/shader_shadowDecal" (original was a ShaderGraph Decal-target
// shader used by URP's DecalRendererFeature for blob-shadow projector decals,
// see Assets/Scripts/Assembly-CSharp/BlobShadowDecal/ShadowDecal.cs).
//
// URP draws ScreenSpace projector decals itself: DecalScreenSpaceRenderPass
// (after opaque, depth-tested) draws the decal cube mesh with the projector
// matrix and a per-decal _NormalToWorld instance matrix; the shader's pass must
// be named/tagged "DecalScreenSpaceProjector". This pass recreates that
// projection in shader code: reconstruct the world position from the scene
// depth, transform it back into the projector cube space, sample the base map
// and fade by the projector's fadeFactor encoded in _NormalToWorld[0][3]
// (identical encoding to URP's DecalShaderGraph decal template).
Shader "Shader Graphs/shader_shadowDecal" {
	Properties {
		_Color ("Color", Vector) = (1,0,0,1)
		[NoScaleOffset] _BaseMap ("BaseMap", 2D) = "white" {}
		[HideInInspector] _DrawOrder ("Draw Order", Range(-50, 50)) = 0
		[Enum(Default, 0, Depth Bias, 0, View Bias, 1)] [HideInInspector] _DecalMeshBiasType ("DecalMesh BiasType", Float) = 0
		[HideInInspector] _DecalMeshDepthBias ("DecalMesh DepthBias", Float) = 0
		[HideInInspector] _DecalMeshViewBias ("DecalMesh ViewBias", Float) = 0
		[HideInInspector] _DecalAngleFadeSupported ("Decal Angle Fade Supported", Float) = 1
		[HideInInspector] [NoScaleOffset] unity_Lightmaps ("unity_Lightmaps", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_LightmapsInd ("unity_LightmapsInd", 2DArray) = "" {}
		[HideInInspector] [NoScaleOffset] unity_ShadowMasks ("unity_ShadowMasks", 2DArray) = "" {}
	}
	SubShader
	{
		// DecalScreenSpaceRenderPass uses FilteringSettings(RenderQueueRange.opaque)
		// and the game materials sit at queue 1950, so the queue must stay in the
		// opaque range for the renderer list to pick the decal meshes up.
		Tags
		{
			"Queue" = "Geometry-50"
			"RenderType" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderPipeline" = "UniversalPipeline"
		}
		LOD 200

		Pass
		{
			// Found by name/tag via material.FindPass("DecalScreenSpaceProjector")
			Name "DecalScreenSpaceProjector"
			Tags { "LightMode" = "DecalScreenSpaceProjector" }

			Cull Off
			ZWrite Off
			ZTest LEqual
			Blend SrcAlpha OneMinusSrcAlpha

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

			TEXTURE2D(_BaseMap);
			SAMPLER(sampler_BaseMap);
			float4 _Color;
			float4x4 _NormalToWorld;

			struct Attributes
			{
				float4 positionOS : POSITION;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
			};

			Varyings vert(Attributes input)
			{
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
				// Reconstruct the opaque-surface world position from the scene depth
				// (same reconstruction URP's screen-space decals use).
				float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
				float rawDepth = SampleSceneDepth(uv);
				float3 positionWS = ComputeWorldSpacePosition(uv, rawDepth, unity_MatrixInvVP);

				// Into the decal cube's local space (unit cube spans -0.5..0.5^3)
				float3 positionDS = TransformWorldToObject(float4(positionWS, 1.0)).xyz;

				// Skip pixels projected outside the projector volume
				float outside = max(max(abs(positionDS.x), abs(positionDS.y)), abs(positionDS.z));
				clip(0.5 + 1e-4 - outside);

				// Same _NormalToWorld packing as the URP own decal template:
				// [0][3] = fadeFactor, [3].xy = uv scale, [3].zw = uv offset
				float fadeFactor = clamp(_NormalToWorld[0][3], 0.0, 1.0);
				float2 uvScale = float2(_NormalToWorld[3][0], _NormalToWorld[3][1]);
				float2 uvOffset = float2(_NormalToWorld[3][2], _NormalToWorld[3][3]);

				float2 uvDecal = (positionDS.xy + 0.5) * uvScale + uvOffset;
				half4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvDecal);

				half alpha = baseMap.a * _Color.a * fadeFactor;
				clip(alpha - 0.001);
				return half4(_Color.rgb * baseMap.rgb, alpha);
			}

			ENDHLSL
		}
	}
	Fallback "Hidden/Shader Graph/FallbackError"
	//CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
}
