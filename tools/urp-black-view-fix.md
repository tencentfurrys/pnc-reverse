# URP "black UI / black scene view" fix (applied 2026-10-19)

## Symptom
Scene/Game view renders black. Editor.log floods:

    NullReferenceException
      at UnityEngine.Rendering.Universal.UniversalRenderPipeline..ctor(UniversalRenderPipelineAsset asset) [0x00060]

## Root cause
AssetRipper exported `Packages/manifest.json` with **no packages at all**; the whole runtime
library set existed only as prebuilt DLLs in `Assets/Plugins/`. The serialized URP setup
(`Assets/MonoBehaviour/PC_RPAsset.asset` -> `PC_Renderer.asset`, both pointing at plugin-DLL
script guids) loaded as live objects, but the plugin-based URP assembly could not construct
a working pipeline (no package-side resources/upgrade path).

## Fix applied
1. `Packages/manifest.json` — added (backup at `Packages/manifest.json.bak`):
       "com.unity.render-pipelines.universal": "17.0.3",
       "com.unity.render-pipelines.core": "17.0.3"
   (versions taken from the 2D cross-platform template shipped inside the 6000.3.11f1 editor;
   shadergraph auto-resolves to its editor default, do NOT pin 17.0.3 - it does not exist)
2. Disabled duplicate prebuilt plugins whose assemblies are now provided by packages
   (edited each `.dll.meta`, set `PluginImporter.platformData.Any.enabled: 0`):
       UnityEngine.UI, Unity.TextMeshPro            (com.unity.ugui@2.0.0)
       Unity.Collections, Unity.Collections.LowLevel.ILSupport   (com.unity.collections@2.6.2)
       Unity.Mathematics, Unity.Mathematics.Extensions,
       Unity.Mathematics.Extensions.Hybrid          (com.unity.mathematics@1.3.3)
       Unity.Burst, Unity.Burst.Unsafe              (com.unity.burst@1.8.28)
       Unity.RenderPipelines.Core.Runtime,
       Unity.RenderPipelines.Core.Runtime.Shared,
       Unity.RenderPipelines.Universal.Runtime,
       Unity.RenderPipelines.ShaderGraph.ShaderGraphLibrary
3. Keep-on-as-DOTS-only (no packages pulled for these; leave enabled):
       Unity.Entities*, Unity.Transforms*, Unity.Scenes, Unity.Serialization,
       Unity.Deformations, Unity.InputSystem, DunGen, AstarPathfindingProject,
       PixelCrushers.*, QFSW.QC.*, AllIn1SpriteShader, FronkonGames.Retro.NTSC, mcs, ...

## Alternative fix (if ever needed)
Fresh assets for this exact editor are in the editor's shipped template:
    <editor>\Editor\Data\Resources\PackageManager\ProjectTemplates\com.unity.template.2d-cross-platform-2d-6.1.2.tgz
    (extract -> ProjectData~/Assets/Settings/{UniversalRP.asset, Renderer2D.asset}
     + UniversalRenderPipelineGlobalSettings.asset)
Copy them in and remap `m_CustomRenderPipeline` (GraphicsSettings) + `customRenderPipeline`
(QualitySettings) to the new guid.

## Result
0 compile errors, 0 NullReferenceException, project load ~112 s.
