# ProBuilder ShaderGraph duplicate-name fix (device pink blockout) — 2026-10-20

## Symptom (device, APK pnc-0.3.1 [efaeae9])

All opaque geometry flat magenta on device; sprites (AllIn1 unwritten/hand-written
shaders) render correctly. Build log had **zero Shader error lines**.
Evidence: `imgs/2026-10-20-device-check/01-menu-room-magenta-opaques-sprites-ok.jpg`.

## Root cause (proven, three linked facts)

1. **Runtime default material path** (package source, `Runtime/Core/BuiltinMaterials.cs`):

   ```csharp
   internal static Material GetDefaultMaterial()
   {
       var material = (Material) Resources.Load("Materials/ProBuilderDefault", typeof(Material));
       material.shader = Shader.Find("ProBuilder6/Standard Vertex Color");   // ALWAYS overrides
       if (material == null || !material.shader.isSupported)
           material = GetLegacyDiffuse();   // engine default material -> PINK under URP
       return material;
   }
   ```

   It does **not** rely on the loaded material's serialized shader: it re-binds the
   shader by `Shader.Find` name lookup. (Verified against the shipped PC build's
   `Unity.ProBuilder.dll` with REA static managed analysis: ldstr "Materials/ProBuilderDefault",
   ldstr "ProBuilder6/Standard Vertex Color", call UnityEngine.Shader::Find.)

2. **Duplicate shader name.** Two assets declared `Shader "ProBuilder6/Standard Vertex Color"`:
   - our compiled hand-written copy (`Assets/Shader/ProBuilder6_Standard Vertex Color.shader`,
     guid `6b16ad3a11343e243aa19ed1afde1312`) — builds green;
   - the package ShaderGraph (`com.unity.probuilder@1f279ab829b7/Content/Shader/
     Standard Vertex Color.shadergraph`, guid `8547ec39534635c4289065e5c5033f43`).
   `Shader.Find` resolved to the package ShaderGraph, whose forward-pass variants
   were reported in the build log as `After scriptable stripping: 0` → stripped to
   nothing → `isSupported == false` at runtime → `GetLegacyDiffuse()` fallback →
   Unity's default-pipeline-fallback material → **magenta**, on every blockout mesh
   that `BuiltinMaterials.defaultMaterial` touches (i.e. every ProBuilder-created
   dungeon/menu-room surface).

3. **Resources.Load case mismatch (pre-existing, independently landmined).** The ripper
   lower-cased Resources subdirectories (`Assets/Resources/materials/`), while ALL
   `Resources.Load` call sites use the original casing (`"Materials/ProBuilderDefault"`,
   `"Materials/FacePicker"`, `"Materials/VertexPicker"`, `"Materials/EdgePicker"`,
   `"Materials/Trigger"`, `"Materials/Collider"`, `"Textures/GridBox_Default"`, and in
   the game's own code: `"LoadingScreen"`, `"InputGlyphs/InputGlyphDatabase"`,
   `"CompletionUnlocks"`, `"ItemData"`, `"PlayerClassData"`, `"RunRestartController"`).
   With the package's own `Content/Resources/Materials/*` (correct case) missing from the
   old manifest pin's build entry there is no guarantee either; player resource keys are
   case-sensitive.

## Fix applied (keeps ProBuilder 6.0.9 code byte-identical)

1. **Embedded the package** into the project:
   - downloaded the exact registry tarball (`shasum 1f279ab829b7…` — matches
     `packages-lock.json`'s PackageCache folder name),
   - unpacked to `ExportedProject/Packages/com.unity.probuilder/` (all file guids
     preserved, so script/component resolution is unchanged),
   - removed `"com.unity.probuilder": "6.0.9"` from `Packages/manifest.json`
     (embedded package now wins; UPM rewrites packages-lock.json on first resolve).
2. **Deleted the ShaderGraph duplicate** (only from the embedded copy):
   `Content/Shader/Standard Vertex Color.shadergraph` (+ `.meta`). Nothing in the
   project referenced its guid `8547ec39534635c4289065e5c5033f43` except package-internal
   materials (patched next). Deliberately NOT stripped-not-reenabled: a ShaderGraph with
   18.1B variants that got stripped to 0 anyway was pure OOM/time pain on this 16 GB box.
3. **Re-bound package-internal materials** that referenced the deleted graph to our
   compiled shader guid `6b16ad3a11343e243aa19ed1afde1312`:
   - `Content/Resources/Materials/ProBuilderDefault.mat` (the one the runtime loads),
   - `Content/Material/Checker.mat` (UV-editor checker; same shader-family look).
4. **Restored Resources casing** so `Resources.Load` hits the right keys (git mv, metas
   untouched, guids unchanged):
   `assets→materials` family: `CompletionUnlocks`, `ItemData`, `PlayerClassData`,
   `InputGlyphs`, `Materials`, `Textures` (see the exact list in the commit message).

## Why this can't regress Shader.Find

After the change, exactly ONE asset (our hand-written shader) owns
`ProBuilder6/Standard Vertex Color`; it has a single kept forward pass (no variants),
renders unlit-textured with vertex-color tint (the classic ProBuilder look), and any
`material.shader = Shader.Find(...)` assignment therefore binds a supported shader.

## Verification checklist for the next device run

1. Menu room blockout NOT magenta (textured ProBuilder grid look).
2. `adb logcat -s Unity` at launch: no `Shader.Find` material-construction errors and
   no `GetLegacyDiffuse`-triggered NRE around `BuiltinMaterials`.
3. Build log: **no** `com.unity.probuilder` ShaderGraph compilation lines at all, and
   far fewer shader-compile minutes (16 GB box should no longer OOM during shader
   compile/sharedAssets).
