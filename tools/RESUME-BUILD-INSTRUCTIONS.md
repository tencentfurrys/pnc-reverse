# PNC 0.3.1 APK Rebuild — Resume Instructions

> **Purpose:** complete handoff. If the machine dies mid-build, these instructions reconstruct
> everything from scratch on this machine or any other. Written 2026-10-19 while build #6 was running.
>
> **Goal:** produce `pnc-0.3.1.apk` from `ExportedProject\` (reverse-engineered decompilation),
> with the URP package fix (no black UI) and landscape lock, then upload it to
> `https://github.com/tencentfurrys/pnc-reverse` as a release asset.

---

## 1. Machine setup (already done once — redo on a NEW PC)

| Item | Value |
|---|---|
| Unity editor | `6000.3.11f1` (changeset `3000ef702840`) — must match `ExportedProject\ProjectSettings\ProjectVersion.txt` |
| Editor path | `C:\Program Files\Unity\Hub\Editor\6000.3.11f1` |
| Unity CLI | `C:\Users\RDP\AppData\Local\Unity\bin\unity.exe` (v1.0.0-beta.12+; classic `Unity Hub.exe -- --headless` DOES NOT WORK — it silently no-ops) |
| Licenses | Unity Hub personal license already active (`Unity LicensingClient` warnings in logs are noise; builds proceed) |
| Repo working copy | `D:\pnc-reverse` (moved OFF `C:` — C: was at 0 GB free; D: has ~140 GB free). On a new PC keep the project on a disk with ≥30 GB free |
| Project (Unity project root) | `D:\pnc-reverse\ExportedProject` |

### Android build support (Hub/CLI module install is BROKEN — do it manually)

`unity install-modules --editor-version <v> --list` returns EMPTY (CLI catalog bug), and the Unity
CDN "android-sdk-tools" zip is a 166-byte stub. Assemble the module by hand:

1. **Android Build Support (Target Support Installer)** — 1.54 GB:
   `https://download.unity3d.com/download_unity/3000ef702840/TargetSupportInstaller/UnitySetup-Android-Support-for-Editor-6000.3.11f1.exe`
   Run silently elevated: `UnitySetup-...exe /S /D=C:\Program Files\Unity\Hub\Editor\6000.3.11f1`
   (NSIS: `/D=` must be LAST, unquoted, no trailing slash.)
   This installs `Editor\Data\PlaybackEngines\AndroidPlayer\` incl. gradle 8.13 + engine pieces.

2. **OpenJDK 17.0.18 (118 MB, flat zip → `AndroidPlayer\OpenJDK`)**:
   `https://download.unity3d.com/download_unity/open-jdk/open-jdk-win-x64/jdk17.0.18-8_15e8817d1f5db6db3571ebe7430ef37f7fa8e60e8ff6f3e18ca1cb4c29f78774.zip`

3. **SDK parts (dl.google.com) → `AndroidPlayer\SDK` and `AndroidPlayer\NDK`** (zip roots are flat
   or inside one folder name — layout below. Extract individually; `Move-Item` into a
   pre-created destination dir or the move "renames" wrong):

   | Archive | Extracts to |
   |---|---|
   | `android-ndk-r27c-windows.zip` (773 MB) | `AndroidPlayer\NDK\` (flatten inner `android-ndk-r27c` root; must see `NDK\source.properties`) |
   | `commandlinetools-win-12266719_latest.zip` (143 MB) | `AndroidPlayer\SDK\cmdline-tools\16.0\` |
   | `cmake-3.22.1-windows.zip` (16 MB) | `AndroidPlayer\SDK\cmake\3.22.1\` |
   | `build-tools_r36_windows.zip` (59 MB) | `AndroidPlayer\SDK\build-tools\36.0.0\` (inner `android-16` renamed) |
   | `platform-tools_r37.0.1-win.zip` (8 MB) | `AndroidPlayer\SDK\platform-tools\` (must see `adb.exe`) |
   | `platform-34-ext7_r02.zip` (60 MB) | `AndroidPlayer\SDK\platforms\android-34\` |
   | `platform-36_r02.zip` (63 MB) | `AndroidPlayer\SDK\platforms\android-36\` |
   | `platform-37.0_r02.zip` (64 MB) | `AndroidPlayer\SDK\platforms\android-37\` |

   Safe extraction pattern (pre-create folder, enumerate zip entries directly):
   ```powershell
   Add-Type -AssemblyName System.IO.Compression.FileSystem
   $zip = [IO.Compression.ZipFile]::OpenRead($zipPath)
   foreach ($e in $zip.Entries) {
     if ([string]::IsNullOrEmpty($e.Name)) { continue }
     $rel = $e.FullName -replace '<innerPrefix>/', ''
     $dest = Join-Path $target $rel
     $parent = Split-Path $dest -Parent
     if (-not (Test-Path $parent)) { New-Item -ItemType Directory -Force $parent | Out-Null }
     [IO.Compression.ZipFileExtensions]::ExtractToFile($e, $dest, $true)
   }
   ```

4. **Licenses** — write files with `-NoNewline` into `AndroidPlayer\SDK\licenses\`:
   - `android-sdk-license` = `24333f8a63b6825ea9c5514f83c2829b004d1fee`
   - `android-ndk-license` = `d56f5187479451eabf01fb78af6dfcb131a6481e`
   - `android-sdk-preview-license` = `84831b9409646a918e30573bab4c9c91346d8abd`

5. **Verify** (all must be True/exist):
   `AndroidPlayer\Tools\gradle\lib` (307 jars), `OpenJDK\bin\java.exe`, `NDK\source.properties`,
   `SDK\platform-tools\adb.exe`, `SDK\build-tools\36.0.0\aapt2.exe`,
   `SDK\cmdline-tools\16.0\bin\sdkmanager.bat`, `SDK\cmake\3.22.1\bin\cmake.exe`,
   `SDK\platforms\android-{34,36,37}\android.jar`

---

## 2. Project fixes (ALL ALREADY APPLIED in this working copy)

### a) Package resolution — `ExportedProject\Packages\manifest.json`

The export had an empty manifest; duplicate plugin DLLs + real packages clashed. Exact versions
were pinned using the **shasum trick**: Unity 6 PackageCache folders are named
`com.<pkg>@<first 12 hex of tarball SHA1>` — so the game's own plugin DLLs' embedded PDB paths
revealed the exact revisions, and matching tarball SHA1s identified versions. Registry ties to
the editor's bundled manifest (`Editor\Data\Resources\PackageManager\Editor\manifest.json`).

Current manifest adds (top-lines after URP):
```json
"com.unity.entities": "1.4.2",        // sha matches game: f6e02210e263 — proven byte-exact
"com.unity.inputsystem": "1.19.0",
"com.unity.ai.navigation": "2.0.11",
"com.unity.multiplayer.center": "1.0.1",
"com.unity.postprocessing": "3.5.1",
"com.unity.probuilder": "6.0.9",
"com.unity.profiling.core": "1.0.3",
"com.unity.scriptablebuildpipeline": "2.6.1",
"com.unity.serialization": "3.1.3",
"com.unity.timeline": "1.8.11",
"com.unity.visualscripting": "1.9.10",
```
Plus (from the earlier URP black-view fix, see `tools/urp-black-view-fix.md`):
`render-pipelines.universal`/`core` **17.0.3**. Do NOT pin shadergraph (no such version; it auto-resolves 17.3.0 and the hash matches the game).

### b) Disabled plugin DLLs (replaced by the packages above)

30 additional `.meta` files changed `enabled: 1` → `enabled: 0` (each edited file has a sibling
`*.meta.pkgbak` backup): Unity.Entities(.Hybrid/.Hybrid.HybridComponents/UI), Unity.Deformations,
Unity.Mathematics.Extensions(+Hybrid), Unity.Scenes, Unity.Transforms(+Hybrid),
Unity.InternalAPIEngineBridge.002, Unity.InputSystem(+ForUI), Unity.AI.Navigation,
Unity.Multiplayer.Center.Common, Unity.Postprocessing.Runtime, Unity.ProBuilder{,.Csg,KdTree,
Poly2Tri,Stl}, Unity.Profiling.Core, Unity.ScriptableBuildPipeline, Unity.Serialization,
Unity.Timeline, Unity.VisualScripting{.Core,.DocCodeExamples,.Flow,.State,.Antlr3.Runtime}.

Earlier round (URP fix) ALSO has disabled: UnityEngine.UI, Unity.TextMeshPro, Unity.Collections,
Unity.Collections.LowLevel.ILSupport, Unity.Mathematics, Unity.Burst, Unity.Burst.Unsafe,
Unity.RenderPipelines.Core.Runtime(+Shared), Unity.RenderPipelines.ShaderGraph.ShaderGraphLibrary,
Unity.RenderPipelines.Universal.Runtime.

**KEEP ENABLED** (leave as the working copy has): Unity.RenderPipelines.GPUDriven.Runtime,
Unity.UnifiedRayTracing.Runtime, Unity.RenderPipelines.Core.ShaderLibrary,
Unity.RenderPipeline.Universal.ShaderLibrary, Unity.RenderPipelines.Universal.{2D,Config,Shaders}
and the third-party game plugins (Astar, DunGen, QFSW.QC, PixelCrushers, Clipper2, mcs, etc.).

### c) Decompiled IL2CPP artifacts removed from Assets (moved to `tools\disabled-codegen\`)

These stubs are decompiled output of the ORIGINAL IL2CPP pipeline; with real packages Unity
regenerates them at build time and they only cause compile/IL2CPP errors:

- `Assets\Plugins\Assembly-CSharp-firstpass\Unity\Entities\CodeGeneratedRegistry\`
- `Assets\Scripts\Assembly-CSharp\Unity\Entities\CodeGeneratedRegistry\`
- `Assets\Plugins\Assembly-CSharp-firstpass\__UnmanagedPostProcessorOutput__5821909224075991761.cs`
- `Assets\Scripts\Assembly-CSharp\__UnmanagedPostProcessorOutput__1221673671587648887.cs`
- `Assets\StreamingAssets\EntityScenes\` (AssetRipper leftover `scene_info.bin` conflicts with
  what the EntityScene build processor generates; the build regenerates it into the APK)

### d) Player settings

- `applicationIdentifier: Android: com.tencentfurrys.pnc` (AssetRipper had stripped it — builds
  refuse an empty id; set via the `applicationIdentifier: {}` line in ProjectSettings.asset)
- `defaultScreenOrientation: 3` = **Landscape Right hard-locked** (already correct); autorotate
  mask allows only landscape left/right. Portrait-stuck issue of the first APK cannot reproduce.
- `AndroidMinSdkVersion: 25`, `AndroidTargetSdkVersion: 0` (auto), IL2CPP backend (default).

### e) Build profile — `Assets\Settings\Build Profiles\Android.asset`

AssetRipper's original had `m_PlatformId: 00000000000000000000000000000000` + `rid: -2`
("Could not parse the build target guid"). REBUILT file committed at
`Assets\Settings\Build Profiles\Android.asset` with:
- `m_PlatformId: b9b35072a6f44c2e863f17467ea3dc13` (canonical Android platform guid)
- full `AndroidPlatformBuildSettings` blob (copied from `Library\BuildProfiles\PlatformProfile.b9b35072a6f44c2e863f17467ea3dc13.asset`):
  `m_BuildType: 2, m_BuildSystem: 1, m_BuildAppBundle: 0` (= APK not AAB)
- guid `9f6b0b5c507dcfe4fa22570ae7ab477f` preserved from the original meta
If you ever need to redo it: let ONE build boot with the file deleted, grab the freshly dumped
`Library\BuildProfiles\PlatformProfile.androidid.asset`, then re-serialize it as an asset as above.

---

## 3. The build command

```bat
"C:\Users\RDP\AppData\Local\Unity\bin\unity.exe" build ^
  "D:\pnc-reverse\ExportedProject" ^
  --profile Android ^
  -o "D:\pnc-build\pnc-0.3.1.apk" ^
  --no-tail --non-interactive
```

- Output must NOT be inside the project Assets folder.
- A GUI editor must NOT be open on the same project (project lock).
- `--list-profiles --json` should show `Android` before building.
- Expect: scripts compile → asset-prepare (~18 GB counter, 2 passes, ~40-50 min on 1st run,
  faster reruns) → IL2CPP (`C_Android_arm64 ... .o`, ~2200 objects) → gradle → APK.
- Live log: `ExportedProject\Logs\build-Android-<timestamp>.log` (grep for `error CS`,
  `BuildFailedException`, `Exception`, `Failed to`).

### Failure history (each was fixed; if a new run fails, compare against these)

1. *Extension resolution (IL2CPP strip)* — `Failed to resolve assembly 'Unity.Mathematics.Extensions'`
   → entities package replaces the plugin (see 2a/2b).
2. *Stale Burst cache* — `An item with the same key has already been added. Key: 03df0f31...`
   in `Burst.Compiler.IL.Hashing` → delete `Library\BurstCache` (+ `Library\Bee`).
3. *StreamingAssets conflict* — `scene_info.bin` twice → moved folder out (2c).
4. *IL2CPP sort crash* — duplicate `__UnmanagedPostProcessorOutput__5821909224075991761` type
   instance → stub moved out (2c).
5. If the "COLLECT / comparing" crash returns with a DIFFERENT type pair, apply the same rule:
   find identical decompiled-byproduct types and remove them.
6. *Gradle lintVital crash* — `Execution failed for task ':unityLibrary:lintVitalAnalyzeRelease'`
   → `> For input string: "37.0"` (NumberFormatException inside AndroidLintWorkAction):
   lint does `Integer.parseInt` on `AndroidVersion.ApiLevel=37.0`, the malformed string shipped by
   the SDK archive `platform-37.0_r02.zip` (every other platform writes a plain int, e.g. `36`).
   Fixed twice over: (a) elevated patch of
   `AndroidPlayer\SDK\platforms\android-37\source.properties` → `AndroidVersion.ApiLevel=37`;
   (b) custom gradle templates committed at `Assets\Plugins\Android\{mainTemplate,launcherTemplate}.gradle`
   (copies of the shipped ones) with `lint { abortOnError false; checkReleaseBuilds false }` —
   that removes the lintVital* tasks entirely. Note: `abortOnError false` alone does NOT survive
   this — the crash happens inside the work action before lint reports anything.

---

## 4. Upload to the GitHub release

Repo: `https://github.com/tencentfurrys/pnc-reverse` (public; owner has admin — token not stored
here on purpose; keep it in a secure env var, e.g. `$env:GH_TOKEN`, and `unset` after).

1. Verify `D:\pnc-build\pnc-0.3.1.apk` exists.
2. Create release + upload (this SHA was `pnc-0.3.1-rebuild` tag land desired):
   ```bat
   curl -sS -H "Authorization: Bearer %GH_TOKEN%" -H "Accept: application/vnd.github+json" ^
        https://api.github.com/repos/tencentfurrys/pnc-reverse/releases ^
        -d "{\"tag_name\":\"pnc-0.3.1-rebuild\",\"name\":\"PNC 0.3.1 (rebuilt APK)\",\"body\":\"Rebuilt from ExportedProject with URP 17.0.3 packages + Entities 1.4.2; landscape-locked; black-UI fix applied.\",\"draft\":false,\"prerelease\":false}"
   ```
   Capture `upload_url` and release `id` from the response.
3. Upload the asset:
   ```bat
   curl -sS -H "Authorization: Bearer %GH_TOKEN%" -o NUL ^
        --data-binary "@D:\pnc-build\pnc-0.3.1.apk" ^
        -H "Content-Type: application/vnd.android.package-archive" ^
        "https://uploads.github.com/repos/tencentfurrys/pnc-reverse/releases/<RELEASE_ID>/assets?name=pnc-0.3.1.apk"
   ```
4. Also commit/push the repo's source fixes so this doc stays accurate (see 5).
5. **Security:** the token was pasted in chat — ROTATE it after use (GitHub → Settings →
   Developer settings → tokens).

---

## 5. On-disk backup locations

- `.pkgbak` metas = pre-change state of every plugin dll meta touched in this session (2b).
- `tools\disabled-codegen\` = the decompiled stubs + folders moved out of Assets (2c).
- `Assets\Settings\Build Profiles.bak\` = AssetRipper's original (broken) Android profile.
- `tools\urp-black-view-fix.md` = the earlier URP editor-render fix (after this doc's phase 1).
- `Packages\manifest.json.bak` = before the URP package pins were added.
- Git baseline commit `d94bc74` + our incremental commits carry everything else.

---

## 6. Verify after install on device

1. Main menu renders (not black) — URP pipeline active with the game's own RP assets.
2. Stays landscape (hard-locked LandscapeRight by `defaultScreenOrientation: 3`).
3. If scenes mis-render, check `adb logcat` for the classic `NullReferenceException ... UniversalRenderPipeline`
   (fixed) or Vulkan shader issues — compare with the editor-fix doc.
