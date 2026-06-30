# Size Audit

Date: 2026-06-30  
Scope: initial audit was inspection-only. The later safe optimization pass removed only root-level generated diagnostic files and did not change gameplay code, runtime assets, import settings, packages, `Library/`, or `Builds/`.

## Summary

This is a Unity project using Unity `6000.3.10f1`.

Measured project size before writing this report:

| Metric | Size |
|---|---:|
| Total project folder | 343.80 MiB / 360,500,503 bytes |
| Total file count | 3,067 files |
| Exported build output | 112.73 MiB |
| Unity generated/cache data | 167.31 MiB |
| Assets folder | 60.58 MiB |
| Generated game art under Assets | 60.39 MiB |
| Raw generated source art under Assets | 45.60 MiB |
| Root debug logs/screenshots | 3.12 MiB |

Current Git-tracked size: not measurable from this sandbox. Git resolves this working tree to a parent repository at `C:/Users/lyush`, and sandboxed Git commands cannot safely inspect that parent worktree. The project-local `.git` view did not expose a normal measurable repository. As a proxy, the intended source/config payload is approximately 60.63 MiB if `Assets`, `Packages`, `ProjectSettings`, `.gitignore`, and `README.md` are tracked.

Main size causes:

1. `Library/` is 167.31 MiB. This is Unity-generated cache/import/build data and should not be committed.
2. `Builds/` is 112.73 MiB. This is an exported Windows player and should not be committed as source.
3. `Assets/Art/Generated/_Source/` is 45.60 MiB. These are raw generated source PNGs, mostly 1254x1254, kept inside `Assets`.
4. Runtime generated sprites are PNG-heavy. Final generated art outside `_Source` is about 14.79 MiB.
5. The current build has a 27.56 MiB `resources.assets.resS`, mirrored in `Library/PlayerDataCache`. This likely comes from imported sprite textures and Unity runtime resources.

## Safe Optimization Pass Results

Applied on 2026-06-30.

Only repository hygiene, LFS policy, repeatable reporting scripts, documentation, and root-level generated diagnostic cleanup were performed. No gameplay code, runtime assets, Unity import settings, dependencies, `Library/`, or `Builds/` were deleted.

| Metric | Before safe pass | After safe pass | Change |
|---|---:|---:|---:|
| Total project folder | 343.83 MiB / 360,531,947 bytes | 325.33 MiB / 341,130,604 bytes | -18.50 MiB |
| `Library/` | 167.31 MiB / 175,435,444 bytes | 151.76 MiB / 159,132,459 bytes | -15.55 MiB |
| `Builds/` | 112.73 MiB / 118,210,805 bytes | 112.73 MiB / 118,210,805 bytes | no change |
| `Assets/` | 60.58 MiB / 63,522,447 bytes | 60.58 MiB / 63,522,439 bytes | no meaningful change |
| Root generated logs/screenshots | 3.12 MiB / 3,273,392 bytes | 0 bytes | -3.12 MiB |
| `Logs/` | negligible | 159.06 KiB / 162,875 bytes | build/test logs kept ignored |

What changed:

- `.gitignore` now covers Unity generated folders, exported builds, local editor state, root logs, root debug screenshots, crash dumps, and common exported package/build artifacts.
- `.gitattributes` now marks generated PNG art and future large art/audio formats for Git LFS.
- `scripts/size_report.ps1` and `scripts/size_report.sh` were added for repeatable size checks.
- `SIZE_BUDGET.md` was added.
- 54 root-level generated diagnostic files were removed. They were prior build/smoke logs and screenshot artifacts identified by this audit as generated output.

Git tracking result:

- `git ls-files` returned no tracked files for `Library/`, `Builds/`, `Logs/`, or `UserSettings`.
- No `git rm --cached` was needed.
- `git status --ignored` still shows `Library/`, `Builds/`, `Logs/`, and `UserSettings/` as ignored.

Build/test result:

- Unity batch Windows build succeeded.
- Standalone smoke test succeeded.
- Log scan found only Unity's non-fatal licensing access-token update message plus `LogAssemblyErrors (0ms)` lines; no build failure or runtime exception was found.

Player/build settings checked:

- `stripEngineCode: 1` is already enabled.
- `usePlayerLog: 1` is enabled. Turning this off could reduce runtime log noise but should be a deliberate release-build setting, not a gameplay-safe cleanup.
- `managedStrippingLevel` and `scriptingBackend` are empty/default in `ProjectSettings.asset`; no build setting was changed in this safe pass.

## Size By Category

| Category | Path or basis | Size | Notes |
|---|---|---:|---|
| Source/config | `Packages`, `ProjectSettings`, scripts, scenes, resources | ~0.25 MiB excluding art | Very small. |
| Asset size | `Assets/Art` | 60.39 MiB | Almost all source-controlled payload if assets are tracked. |
| Raw generated/source art | `Assets/Art/Generated/_Source` | 45.60 MiB | Largest source-side issue. Not needed by runtime unless intentionally archived in project. |
| Runtime generated art | generated art excluding `_Source` | ~14.79 MiB | Used by gameplay/UI. Keep for now. |
| Generated/cache | `Library` | 167.31 MiB | Safe to regenerate locally when Unity is closed. |
| Dependencies/cache | `Library/PackageCache` | 20.87 MiB | Unity package cache. Not source. |
| Exported build | `Builds` | 112.73 MiB | Standalone Windows player. Not source. |
| Root debug artifacts | root `*.log`/debug `*.png` | 3.12 MiB | Build/smoke/screenshot artifacts. |

## Top 50 Largest Files

| Path | Size | Type | Likely safe action |
|---|---:|---|---|
| `Builds\ArenaPrototype\UnityPlayer.dll` | 34.15 MiB | exported build DLL | Ignore or remove exported build later; keep only when shipping/testing |
| `Builds\ArenaPrototype\ArenaPrototype_Data\resources.assets.resS` | 27.56 MiB | exported build data | Ignore or remove exported build later; keep only when shipping/testing |
| `Library\PlayerDataCache\Win642\Data\resources.assets.resS` | 27.56 MiB | Unity player build cache | Ignore/regenerate locally; never commit |
| `Builds\ArenaPrototype\MonoBleedingEdge\EmbedRuntime\mono-2.0-bdwgc.dll` | 7.47 MiB | exported build DLL | Ignore or remove exported build later; keep only when shipping/testing |
| `Builds\ArenaPrototype\ArenaPrototype_Data\Resources\unity default resources` | 5.65 MiB | exported build file | Ignore or remove exported build later; keep only when shipping/testing |
| `Builds\ArenaPrototype\D3D12\D3D12Core.dll` | 4.51 MiB | exported build DLL | Ignore or remove exported build later; keep only when shipping/testing |
| `Library\Bee\artifacts\WinPlayerBuildProgram\ManagedStripped\mscorlib.dll` | 4.42 MiB | Unity Bee build cache | Ignore/regenerate locally; never commit |
| `Builds\ArenaPrototype\ArenaPrototype_Data\Managed\mscorlib.dll` | 4.42 MiB | exported build DLL | Ignore or remove exported build later; keep only when shipping/testing |
| `Library\Bee\artifacts\WinPlayerBuildProgram\ManagedStripped\System.Xml.dll` | 3.01 MiB | Unity Bee build cache | Ignore/regenerate locally; never commit |
| `Builds\ArenaPrototype\ArenaPrototype_Data\Managed\System.Xml.dll` | 3.01 MiB | exported build DLL | Ignore or remove exported build later; keep only when shipping/testing |
| `Library\Artifacts\28\282018f983930128ebc3df6f8f45ac9d` | 2.68 MiB | Unity Library cache | Ignore/regenerate locally; never commit |
| `Library\SplashScreenCache\a1\a1643d33572457fa402d089ba470c058` | 2.68 MiB | Unity Library cache | Ignore/regenerate locally; never commit |
| `Builds\ArenaPrototype\ArenaPrototype_Data\globalgamemanagers.assets.resS` | 2.67 MiB | exported build data | Ignore or remove exported build later; keep only when shipping/testing |
| `Library\PlayerDataCache\Win642\Data\globalgamemanagers.assets.resS` | 2.67 MiB | Unity player build cache | Ignore/regenerate locally; never commit |
| `Library\Bee\artifacts\WinPlayerBuildProgram\ManagedStripped\System.dll` | 2.52 MiB | Unity Bee build cache | Ignore/regenerate locally; never commit |
| `Builds\ArenaPrototype\ArenaPrototype_Data\Managed\System.dll` | 2.52 MiB | exported build DLL | Ignore or remove exported build later; keep only when shipping/testing |
| `Builds\ArenaPrototype\ArenaPrototype_Data\Managed\UnityEngine.UIElementsModule.dll` | 2.31 MiB | exported build DLL | Ignore or remove exported build later; keep only when shipping/testing |
| `Library\Bee\artifacts\WinPlayerBuildProgram\ManagedStripped\UnityEngine.UIElementsModule.dll` | 2.31 MiB | Unity Bee build cache | Ignore/regenerate locally; never commit |
| `Assets\Art\Generated\_Source\Characters\enemy_bulwark_source.png` | 2.09 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\_Source\SkillEffects\skill_circuit_nova_burst_source.png` | 2.08 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Builds\ArenaPrototype\ArenaPrototype_Data\Managed\System.Data.dll` | 2.03 MiB | exported build DLL | Ignore or remove exported build later; keep only when shipping/testing |
| `Library\Bee\artifacts\WinPlayerBuildProgram\ManagedStripped\System.Data.dll` | 2.03 MiB | Unity Bee build cache | Ignore/regenerate locally; never commit |
| `Library\PackageCache\com.unity.ugui@bb329a87fcdc\Package Resources\TMP Examples & Extras.unitypackage` | 2.01 MiB | Unity package cache/dependency | Ignore/regenerate locally; never commit |
| `Library\ArtifactDB` | 2.00 MiB | Unity Library cache | Ignore/regenerate locally; never commit |
| `Library\SourceAssetDB` | 2.00 MiB | Unity Library cache | Ignore/regenerate locally; never commit |
| `Assets\Art\Generated\_Source\frost_chain.source.png` | 1.96 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\_Source\guard_plate.source.png` | 1.94 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\_Source\SkillEffects\skill_resonance_ward_aura_source.png` | 1.92 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Builds\ArenaPrototype\ArenaPrototype_Data\Managed\UnityEngine.CoreModule.dll` | 1.89 MiB | exported build DLL | Ignore or remove exported build later; keep only when shipping/testing |
| `Library\Bee\artifacts\WinPlayerBuildProgram\ManagedStripped\UnityEngine.CoreModule.dll` | 1.89 MiB | Unity Bee build cache | Ignore/regenerate locally; never commit |
| `Assets\Art\Generated\_Source\circuit_nova.source.png` | 1.88 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\_Source\spark_core.source.png` | 1.87 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\SkillEffects\skill_circuit_nova_burst.png` | 1.86 MiB | runtime generated sprite | Keep for gameplay; consider texture compression/LFS later |
| `Assets\Art\Generated\Characters\enemy_bulwark.png` | 1.79 MiB | runtime generated sprite | Keep for gameplay; consider texture compression/LFS later |
| `Assets\Art\Generated\_Source\hunter_boots.source.png` | 1.79 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\_Source\resonance_ward.source.png` | 1.76 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\_Source\soul_barrier.source.png` | 1.73 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\_Source\tempo_charm.source.png` | 1.69 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\SkillEffects\skill_resonance_ward_aura.png` | 1.69 MiB | runtime generated sprite | Keep for gameplay; consider texture compression/LFS later |
| `Assets\Art\Generated\_Source\cast_circle.source.png` | 1.67 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\_Source\shadow_cleave.source.png` | 1.66 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\_Source\shockwave_ring.source.png` | 1.64 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\_Source\vector_dash.source.png` | 1.57 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\_Source\telegraph_marker.source.png` | 1.55 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Builds\ArenaPrototype\UnityCrashHandler64.exe` | 1.54 MiB | exported build EXE | Ignore or remove exported build later; keep only when shipping/testing |
| `Assets\Art\Generated\_Source\Characters\ally_resonator_source.png` | 1.49 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\_Source\blood_lance.source.png` | 1.48 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\_Source\slash_arc.source.png` | 1.47 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\_Source\Characters\enemy_striker_source.png` | 1.46 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |
| `Assets\Art\Generated\_Source\Characters\enemy_skirmisher_source.png` | 1.42 MiB | raw generated source art | Move outside Assets or Git LFS later; not needed in player build |

## Top 30 Largest Folders

| Path | Size | Likely cause | Likely safe action |
|---|---:|---|---|
| `Library` | 167.31 MiB | Unity cache/import/build artifacts | Ignore/regenerate locally; safe cleanup when Unity closed |
| `Builds` | 112.73 MiB | Exported Windows player | Ignore/remove later when build artifact not needed |
| `Builds\ArenaPrototype` | 112.73 MiB | Exported Windows player | Ignore/remove later when build artifact not needed |
| `Library\Artifacts` | 64.92 MiB | Unity cache/import/build artifacts | Ignore/regenerate locally; safe cleanup when Unity closed |
| `Builds\ArenaPrototype\ArenaPrototype_Data` | 63.21 MiB | Exported Windows player | Ignore/remove later when build artifact not needed |
| `Assets` | 60.58 MiB | Project/dependency content | Review before changing |
| `Assets\Art` | 60.39 MiB | Generated PNG game art | Keep; compress/downscale/import optimize after playtest |
| `Assets\Art\Generated` | 60.39 MiB | Generated PNG game art | Keep; compress/downscale/import optimize after playtest |
| `Assets\Art\Generated\_Source` | 45.60 MiB | Raw generated source PNGs | Move outside Assets or LFS later |
| `Library\Bee` | 40.24 MiB | Unity cache/import/build artifacts | Ignore/regenerate locally; safe cleanup when Unity closed |
| `Library\PlayerDataCache\Win642` | 31.34 MiB | Unity cache/import/build artifacts | Ignore/regenerate locally; safe cleanup when Unity closed |
| `Library\PlayerDataCache` | 31.34 MiB | Unity cache/import/build artifacts | Ignore/regenerate locally; safe cleanup when Unity closed |
| `Library\PlayerDataCache\Win642\Data` | 31.30 MiB | Unity cache/import/build artifacts | Ignore/regenerate locally; safe cleanup when Unity closed |
| `Library\Bee\artifacts` | 30.53 MiB | Unity cache/import/build artifacts | Ignore/regenerate locally; safe cleanup when Unity closed |
| `Library\Bee\artifacts\WinPlayerBuildProgram` | 26.26 MiB | Unity cache/import/build artifacts | Ignore/regenerate locally; safe cleanup when Unity closed |
| `Library\Bee\artifacts\WinPlayerBuildProgram\ManagedStripped` | 26.26 MiB | Unity cache/import/build artifacts | Ignore/regenerate locally; safe cleanup when Unity closed |
| `Builds\ArenaPrototype\ArenaPrototype_Data\Managed` | 26.26 MiB | Exported Windows player | Ignore/remove later when build artifact not needed |
| `Library\PackageCache` | 20.87 MiB | Unity cache/import/build artifacts | Ignore/regenerate locally; safe cleanup when Unity closed |
| `Library\PackageCache\com.unity.ugui@bb329a87fcdc` | 20.82 MiB | Unity cache/import/build artifacts | Ignore/regenerate locally; safe cleanup when Unity closed |
| `Library\PackageCache\com.unity.ugui@bb329a87fcdc\Documentation~` | 11.88 MiB | Unity cache/import/build artifacts | Ignore/regenerate locally; safe cleanup when Unity closed |
| `Library\PackageCache\com.unity.ugui@bb329a87fcdc\Documentation~\images` | 11.34 MiB | Unity cache/import/build artifacts | Ignore/regenerate locally; safe cleanup when Unity closed |
| `Builds\ArenaPrototype\MonoBleedingEdge` | 8.70 MiB | Exported Windows player | Ignore/remove later when build artifact not needed |
| `Builds\ArenaPrototype\MonoBleedingEdge\EmbedRuntime` | 8.05 MiB | Exported Windows player | Ignore/remove later when build artifact not needed |
| `Assets\Art\Generated\_Source\Characters` | 7.85 MiB | Raw generated source PNGs | Move outside Assets or LFS later |
| `Assets\Art\Generated\_Source\SkillEffects` | 7.61 MiB | Raw generated source PNGs | Move outside Assets or LFS later |
| `Builds\ArenaPrototype\ArenaPrototype_Data\Resources` | 5.96 MiB | Exported Windows player | Ignore/remove later when build artifact not needed |
| `Assets\Art\Generated\SkillEffects` | 5.10 MiB | Generated PNG game art | Keep; compress/downscale/import optimize after playtest |
| `Assets\Art\Generated\Characters` | 4.98 MiB | Generated PNG game art | Keep; compress/downscale/import optimize after playtest |
| `Library\Artifacts\28` | 4.78 MiB | Unity cache/import/build artifacts | Ignore/regenerate locally; safe cleanup when Unity closed |
| `Builds\ArenaPrototype\D3D12` | 4.51 MiB | Exported Windows player | Ignore/remove later when build artifact not needed |

## Suspicious Generated/Cache/Build Folders

| Path | Size | Finding |
|---|---:|---|
| `Library/` | 167.31 MiB | Unity generated cache. Should be ignored and regenerated locally. |
| `Library/Artifacts/` | 64.92 MiB | Imported asset artifacts. Safe to regenerate. |
| `Library/Bee/` | 40.24 MiB | Unity build pipeline cache. Safe to regenerate. |
| `Library/PlayerDataCache/` | 31.34 MiB | Cached player build data. Safe to regenerate. |
| `Library/PackageCache/` | 20.87 MiB | Unity package cache. Safe to regenerate. |
| `Builds/` | 112.73 MiB | Exported Windows player. Should not be source-tracked. |
| `Logs/` | <1 KiB | Ignored by `.gitignore`; negligible. |
| `UserSettings/` | <1 KiB | User-local settings. Should remain ignored. |
| Root logs/screenshots | 3.12 MiB | Build/test artifacts in project root. `.gitignore` only ignores `batch_build*.log`, not most current root logs/screenshots. |

## Large Binary Assets

All large source assets are PNGs. No audio assets were found.

Large asset folders:

| Path | Size | Notes |
|---|---:|---|
| `Assets/Art/Generated/_Source` | 45.60 MiB | Raw source PNGs, mostly 1254x1254. Not referenced by runtime database. |
| `Assets/Art/Generated/SkillEffects` | 5.10 MiB | Runtime skill VFX sprites. |
| `Assets/Art/Generated/Characters` | 4.98 MiB | Runtime character/enemy sprites. |
| `Assets/Art/Generated/SkillIcons` | 2.19 MiB | Runtime UI icons. |
| `Assets/Art/Generated/ItemIcons` | 1.34 MiB | Runtime item icons. |
| `Assets/Art/Generated/VFX` | 1.19 MiB | Runtime VFX sprites/prefabs. |

Texture dimensions observed:

| Asset group | Dimensions | Notes |
|---|---|---|
| Most generated source/final character and VFX PNGs | 1254x1254 | Larger than necessary for current top-down prototype. |
| `skill_pulse_bolt_projectile` | 1672x941 | Wide projectile sprite. |
| Contact sheet | 1100x500 | Debug/reference image only. |
| Existing skill/item/VFX icons | mostly 512x512 | Reasonable source dimensions. |

Import setting findings:

- Final generated sprite `.meta` files are imported as Sprites with mipmaps disabled.
- Some final sprite `.meta` files have `DefaultTexturePlatform` max size 512 but `Standalone` platform max size 2048 with compression enabled. That means Windows builds may include larger texture data than intended.
- Raw `_Source` PNGs are imported as regular textures with mipmaps enabled, because they live under `Assets`. Even if not included in builds, they bloat `Library` and source checkout size.

## Duplicate Assets

Exact hash duplicate scan across image/audio-style assets found no exact duplicate asset hashes.

There are intentional near-duplicates:

- `_Source/*.png` raw generated images.
- Final processed transparent PNGs under `Characters`, `SkillEffects`, `SkillIcons`, `VFX`, and `ItemIcons`.

These are not byte-identical, but the raw `_Source` copies are the largest removable/movable candidates.

## Large Dependencies

| Path | Size | Notes |
|---|---:|---|
| `Library/PackageCache/com.unity.ugui@bb329a87fcdc` | 20.82 MiB | Unity UI package cache; generated by Unity Package Manager. Not source. |
| `Library/PackageCache/com.unity.ugui@bb329a87fcdc/Documentation~` | 11.88 MiB | Documentation/images in package cache. Not source. |
| `Library/PackageCache/.../TMP Examples & Extras.unitypackage` | 2.01 MiB | Package cache example payload. Not source. |

Package manifest:

- Direct dependencies: `com.unity.ugui`, `com.unity.modules.ai`, `animation`, `audio`, `physics`, `physics2d`, `screencapture`, `ui`.
- Source reference search found `UnityEngine.UI` in `ArenaHUD.cs` and `ScreenCapture` in `ArenaGame.cs`.
- No source references were found for AI/NavMesh, Animation/Animator, or Physics2D APIs. These are possible unused package candidates, but removing Unity built-in modules should be playtested.

## Debug Symbols

`.pdb` files found are in `Library/ScriptAssemblies` and `Library/Bee` only. They total about 1.84 MiB and are cache/build artifacts, not source files. No `.mdb` files were found.

## Oversized Audio

No `.wav`, `.mp3`, `.ogg`, `.aiff`, or `.aif` assets were found.

## Gitignore Findings

Current `.gitignore` already covers:

- `Library/`
- `Temp/`
- `Obj/`
- `Build/`
- `Builds/`
- `Logs/`
- `UserSettings/`
- common Unity/Mono project files and debug symbols
- `batch_build*.log`

Safe pass update:

- Resolved: root build/test logs, root screenshot/debug PNGs, `MemoryCaptures/`, `Recordings/`, `.vs/`, `.idea/`, `.vscode/`, crash/dump artifacts, and common exported build artifacts are now ignored.
- Policy note: `.unitypackage` is now ignored as an exported artifact. If a future third-party `.unitypackage` must be versioned as source, explicitly unignore that file or place it in a documented vendor folder.

## Git LFS Candidates

If these files are tracked in normal Git, they should be moved to Git LFS or relocated outside the repo:

1. `Assets/Art/Generated/_Source/**/*.png` - 45.60 MiB total; raw generated source art.
2. `Assets/Art/Generated/Characters/*.png` - 4.98 MiB total; runtime character sprites.
3. `Assets/Art/Generated/SkillEffects/*.png` - 5.10 MiB total; runtime skill VFX sprites.
4. Any future `.psd`, `.blend`, `.fbx`, `.wav`, `.mp3`, `.ogg`, or large `.png` assets over 1 MiB.

Recommended LFS policy later:

```powershell
git lfs track "*.psd" "*.blend" "*.fbx" "*.wav" "*.mp3" "*.ogg"
git lfs track "Assets/Art/Generated/**/*.png"
git add .gitattributes
```

Do not run this until Git LFS availability and repository policy are confirmed.

## Engine-Specific Findings

- Unity cache folders dominate local workspace size.
- Exported Windows builds are present inside the project root. They are useful for local testing but should not be source-controlled.
- Raw generated art is inside `Assets`, causing Unity to import it and cache it even though the game uses processed final sprites.
- Runtime art currently uses real generated PNGs. This is correct for functionality but should be import-optimized.
- The build includes many managed DLLs and Unity runtime DLLs; some are normal for a Mono Windows build.
- `UnityEngine.Physics2DModule.dll` and `UnityEngine.AnimationModule.dll` appear in the build even though source references were not found. Package/module trimming may reduce a small amount after testing.
