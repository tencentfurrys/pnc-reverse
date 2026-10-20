# Device check - rebuilt APK (2026-10-20, 00:15 recording)

Recording: `Downloads/2026_10_20_00_15_07.mp4` (26.6 s, 1080x1920 screen recording of the phone).

## APK under test
- `pnc-0.3.1.apk` = 114,839,737 bytes, arm64-v8a IL2CPP, built green 19 Oct 2026 ~22:33.
- Repo commit: `efaeae9` ("Replace 58 stub/DummyShader shaders ...").
- Release: https://github.com/tencentfurrys/pnc-reverse/releases/tag/pnc-0.3.1-rebuild (asset re-uploaded, old pre-fix asset deleted).

## What the recording shows
| Frame | State |
|---|---|
| `01-menu-room-magenta-opaques-sprites-ok.jpg` | Game running (~6-16 s). **All opaque lit geometry renders flat magenta** (Unity missing-shader pink). **Stained-glass window sprites + wall torch sprites with flame render correctly.** View is letterboxed into a horizontal band (landscape-locked app on portrait-held phone - matches earlier reference screenshot). |
| `02-exit-to-home.jpg` | User exits to home screen (~22 s). Launcher shows "Exported..." app icon. |

No UI/menu canvas, no TMP text and no 3D-to-menu transition were captured in the clip - the sampled run sits inside one stable state then exits.

## What works (proven on device)
- **2D sprite stack renders**: AllIn1SpriteShader/AllIn1SpriteShaderLit materials (torch `peanut head.mat`, stained glass) show textures, alpha and flame frames. The old "3D torch vs 2D torch" bug is gone - torches are the 2D pixel-art sprites now.
- **No black quads** anywhere (the pre-fix "2d black" Sprite-Unlit stub issue).
- Letterboxing/orientation behaves as configured (LandscapeLeft, OS letterboxes on portrait-held phone).

## What is broken (magenta opaques)
Unity ships magenta when the bound shader has no valid/kept variant at runtime. Build log for this run (`Logs/build-Android-1792445736993.log`) contains **zero `Shader error` lines** - every shader compiled - so this is a runtime shader-binding problem, not a compile error.

Ranked hypotheses:

1. **ProBuilder ShaderGraph duplicate-name resolution (most likely).**
   - Two assets declare `Shader "ProBuilder6/Standard Vertex Color"`: our Assets copy (guid `6b16ad3a11343e243aa19ed1afde1312`, hand-written 1-pass) and the package ShaderGraph `PackageCache/com.unity.probuilder@1f279ab829b7/.../Standard Vertex Color.shadergraph` (guid `8547ec39534635c4289065e5c5033f43`).
   - The build log shows the **package** ShaderGraph compiled (pass "Universal Forward", 18.1B full variant space, ~900 s) and its relevant passes ended `After scriptable stripping: 0`.
   - The game creates the dungeon blockout with ProBuilder at runtime; ProBuilder code does `Shader.Find("ProBuilder6/Standard Vertex Color")` which likely resolves to the package graph -> stripped to nothing -> **magenta**.
   - Old (stub) build had no package graph shader winning the name: `Shader.Find` hit the stub -> textured but flat (the look in the old videos).
   - Fix options: (a) remove `com.unity.probuilder` from `Packages/manifest.json` if the game never uses its package scripts, (b) change our file's shader name so `Shader.Find` hits our copy, or (c) keep only one asset with that name. Verify with `adb logcat` material/shader warnings and in-editor inspection of the runtime-generated material.
2. **Fullscreen pass shader / strip settings mismatch** (less likely given sprites render and pink covers only opaques + transparent layer order).
3. Anything my pragma-prune touched for variants URP actually needs (`_CLUSTER_LIGHT_LOOP` etc. were deliberately kept) - low probability, but re-check the stripper log lines `After scriptable stripping: N` for `Universal Render Pipeline/Lit` passes in the build log when diagnosing.

## Next steps (when a rebuild is possible)
1. `adb logcat -s Unity` during launch; look for "Shader" warnings/exceptions on the first frames.
2. Decide on the ProBuilder package (hypothesis 1): likely manifest removal or name dedup, then rebuild.
3. Re-verify on device: menu cross panel + white TMP text, blob shadows, fullscreen vignettes; then compare against the PC reference (`Documents/pcversion.jpg`).

## Images
- `01-menu-room-magenta-opaques-sprites-ok.jpg`
- `02-exit-to-home.jpg`
(Extracted with ffmpeg from the screen recording; timestamps 00:09 and 00:22.)

## PC reference (regenerated 2026-10-20 ~01:05)
The old `Documents/pcversion.jpg` did not survive the machine swap; the PC build from
`Downloads/Post_Nut_Calamity_0.3.1_Win.zip` was launched and screenshotted instead:
`../pc-reference/pc-reference-menu.jpg` — original main menu (altar room): textured
brick walls, wooden floor/ceiling, candle sprites, cross+butt centerpiece, TMP menu
text (Play/Gallery/Unlocks/Options/Exit), supporters panel, version 0.3.1. The menu
runs in a portrait window on PC (1080x2240) - same letterbox behavior Android shows.
