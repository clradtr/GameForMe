# Size Optimization Plan

This plan preserves current functionality. The implemented safe pass is recorded below; remaining medium/high-risk items are still recommendations only.

## Implemented Safe Pass

Applied on 2026-06-30.

Implemented:

- Expanded `.gitignore` for Unity generated folders, exported builds, root diagnostics, local editor state, crash dumps, and common exported artifacts.
- Added `.gitattributes` for generated PNG art and future large art/audio assets using Git LFS.
- Added `scripts/size_report.ps1`.
- Added `scripts/size_report.sh`.
- Added `SIZE_BUDGET.md`.
- Removed only root-level generated diagnostic files: prior build/smoke logs and debug screenshots.
- Did not delete `Library/`, `Builds/`, `Assets/`, runtime generated art, raw source art, packages, or gameplay files.
- Did not remove dependencies/packages.
- Did not change Unity import settings.

Before/after from the safe pass:

| Metric | Before | After | Change |
|---|---:|---:|---:|
| Total project folder | 343.83 MiB | 325.33 MiB | -18.50 MiB |
| Root generated logs/screenshots | 3.12 MiB | 0 bytes | -3.12 MiB |
| `Library/` | 167.31 MiB | 151.76 MiB | -15.55 MiB after Unity rebuild/cache refresh |
| `Builds/` | 112.73 MiB | 112.73 MiB | no change |
| `Assets/` | 60.58 MiB | 60.58 MiB | no meaningful change |

Verification:

- Unity batch Windows build succeeded.
- Standalone smoke test succeeded.
- No generated/cache/build folders are tracked by Git; this project currently appears untracked under the parent Git worktree, and `git ls-files` returned no tracked generated folder entries.
- Player/build settings were inspected only. `stripEngineCode` is already enabled; `usePlayerLog` remains enabled; no PlayerSettings were changed.

## Expected Outcomes

Current measured project folder: 343.80 MiB.

Estimated after safe cleanup of local cache/build/debug artifacts:

| Step | Estimated remaining project folder |
|---|---:|
| Current | 343.80 MiB |
| Remove/regenerate local `Library/` only | ~176.49 MiB |
| Also remove local `Builds/` | ~63.76 MiB |
| Also clean root debug logs/screenshots | ~60.64 MiB |
| Also move raw `_Source` art outside project | ~15.04 MiB |

The safest first optimization pass is to improve `.gitignore`, keep `Library/` and `Builds/` out of Git, and clean root debug artifacts. The biggest source-side win is relocating or LFS-tracking `Assets/Art/Generated/_Source`.

## Safe Changes

These are low risk because they do not affect gameplay assets or code when done carefully.

| Change | Expected reduction | Why safe | Commands/settings to apply later |
|---|---:|---|---|
| Ensure generated Unity folders stay ignored: `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, `Builds/` | Prevents 280.04 MiB from entering Git; local disk can regain same amount if removed | Unity regenerates these folders. `Builds/` is exported output. | Add/keep ignore rules, then remove locally only when Unity is closed. |
| Add root debug artifact ignores | Prevents ~3.12 MiB now, more later | Root logs/screenshots are test artifacts, not gameplay. | Add patterns listed below to `.gitignore`. |
| Keep audit/build logs out of commits | Prevents noisy repo growth | Logs are reproducible from test/build runs. | Ignore `/*.log` or narrower patterns. |
| Verify no exported build is committed | Prevents 112.73 MiB repo bloat | Build can be recreated from source. | Use `git status --ignored` once Git access is working. |

Suggested `.gitignore` additions:

```gitignore
# Unity local/editor artifacts
[Mm]emoryCaptures/
[Rr]ecordings/
.vs/
.idea/
.vscode/

# Root-level generated diagnostics
/*.log
/unity_render_check*.png
/generated_art_screen_check*.png
/generated_art_screen_check_visible*.png
/generated_characters_screenshot*.png
/hitbox_growth_screen_check*.png
/visual_check*.png
/generated_assets_contact_sheet.png

# Crash/dump artifacts
*.dmp
*.stackdump

# Common exported builds
*.apk
*.aab
*.app
*.xcarchive
```

Local cleanup commands to run later only after approval and with Unity closed:

```powershell
# Preview first
Get-ChildItem -Force Library,Builds,Logs,UserSettings -ErrorAction SilentlyContinue |
  Select-Object FullName,Mode

# Later cleanup, not part of this audit
Remove-Item -LiteralPath Library -Recurse -Force
Remove-Item -LiteralPath Builds -Recurse -Force
Remove-Item -LiteralPath Logs -Recurse -Force
Remove-Item -LiteralPath UserSettings -Recurse -Force

# Root debug files, review before removing
Get-ChildItem -File *.log,*.png |
  Where-Object {
    $_.Name -like 'batch_*' -or
    $_.Name -like 'standalone_*' -or
    $_.Name -like 'unity_render_check*' -or
    $_.Name -like 'generated_*screen*' -or
    $_.Name -like 'generated_*screenshot*' -or
    $_.Name -like 'visual_check*' -or
    $_.Name -like 'hitbox_growth_screen_check*'
  }
```

Rollback for safe changes:

- Restore `.gitignore` with `git checkout -- .gitignore` if the ignore edit is wrong.
- Reopen Unity to regenerate `Library/`.
- Rebuild with the existing Unity build method to recreate `Builds/ArenaPrototype`.

## Medium-Risk Changes

These should be done only with local playtesting after each change.

| Change | Expected reduction | Risk | Commands/settings to apply later |
|---|---:|---|---|
| Move `Assets/Art/Generated/_Source` outside `Assets` | Reduces Unity project/source payload by 45.60 MiB if moved outside project; also reduces future `Library` import cache | Low-to-medium. These files appear to be raw source art and are not referenced by runtime database, but editor workflows may expect them. | Move to `ArtSourceArchive/Generated_Source` or outside the repo, keeping a manifest. Then rebuild and smoke test. |
| Put generated PNG art into Git LFS | Does not reduce working tree size, but prevents normal Git history bloat | Medium. Requires Git LFS availability and repo policy. | Track `Assets/Art/Generated/**/*.png`, then renormalize. |
| Compress/downscale runtime sprites in Unity import settings | Could reduce build `resources.assets.resS` by roughly 10-22 MiB | Medium. Visual quality could degrade, especially UI icons and VFX alpha edges. | Set Standalone max size to 512 for generated sprites, compression Normal/High, mipmaps off, read/write off. Rebuild and visually inspect. |
| Remove unused Unity built-in modules | Likely 0.5-2 MiB build reduction | Medium. Built-in modules can have hidden dependencies. | Try removing `com.unity.modules.ai`, `com.unity.modules.animation`, `com.unity.modules.physics2d` from `Packages/manifest.json` one at a time, then rebuild/test. |
| Remove or guard screenshot-only `ScreenCapture` build path | Small, probably <1 MiB | Medium. Useful test helper could break. | If screenshot automation is no longer needed, remove `-arenaScreenshot` path and `com.unity.modules.screencapture`, then test. |

Suggested raw source art move later:

```powershell
# Preview references first
rg -n "_Source|source\\.png|GeneratedArt" Assets ProjectSettings Packages

# Later, after approval
New-Item -ItemType Directory -Force -Path ArtSourceArchive | Out-Null
Move-Item -LiteralPath "Assets\Art\Generated\_Source" -Destination "ArtSourceArchive\Generated_Source"
```

Suggested Git LFS commands later:

```powershell
git lfs install
git lfs track "Assets/Art/Generated/**/*.png"
git add .gitattributes
git add --renormalize "Assets/Art/Generated"
git status --short
```

Suggested Unity import optimization later:

1. In Unity, select generated sprite folders:
   - `Assets/Art/Generated/Characters`
   - `Assets/Art/Generated/SkillEffects`
   - `Assets/Art/Generated/SkillIcons`
   - `Assets/Art/Generated/ItemIcons`
   - `Assets/Art/Generated/VFX`
2. For Standalone platform, test:
   - Max Size: 512 for icons/VFX/projectiles; maybe 1024 for large character/VFX if 512 looks soft.
   - Compression: Normal first, High only after visual inspection.
   - Mip Maps: Off.
   - Read/Write: Off.
3. Rebuild and compare:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.3.10f1\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "C:\Users\lyush\Desktop\삶\GameForMe" `
  -executeMethod ArenaPrototypeEditor.ArenaPrototypeSceneBuilder.BuildWindowsPlayer `
  -logFile "C:\Users\lyush\Desktop\삶\GameForMe\build_after_texture_import_change.log"
```

Rollback for medium-risk changes:

- Move `ArtSourceArchive/Generated_Source` back to `Assets/Art/Generated/_Source`.
- Revert `.gitattributes` and renormalization if LFS policy is wrong.
- Use Git to revert `.meta` files after import setting experiments.
- Rebuild and run the standalone smoke test.

## High-Risk Changes

Do not perform these without explicit approval.

| Change | Expected reduction | Why high risk |
|---|---:|---|
| Delete generated source art permanently | Up to 45.60 MiB | Loses provenance/raw generated outputs. Prefer archive or LFS first. |
| Delete or replace runtime generated art | Up to 14.79 MiB | Directly changes visuals and may break references. |
| Rewrite art loading/database architecture | Unknown | Violates current stability goal. Not needed for size audit. |
| Switch scripting backend/build pipeline aggressively, e.g. IL2CPP-only, stripping high | Unknown; can shrink or grow Windows builds | Can break runtime reflection/Unity modules and increase iteration time. |
| Remove UGUI or UI modules | Potentially several MiB | `ArenaHUD.cs` uses `UnityEngine.UI`; gameplay UI would break. |
| Remove `com.unity.modules.screencapture` without changing code | Small | `ArenaGame.cs` references `ScreenCapture`. |

Rollback for high-risk changes:

- Restore from Git.
- Restore archived assets from the external archive.
- Reimport all assets by reopening Unity.
- Rebuild `Builds/ArenaPrototype`.
- Run smoke and manual playtest.

## Verification Checklist For Any Future Optimization Pass

After each approved change:

1. Reopen Unity and confirm no import errors.
2. Rebuild the Windows player.
3. Run standalone smoke test:

```powershell
$exe = "C:\Users\lyush\Desktop\삶\GameForMe\Builds\ArenaPrototype\ArenaPrototype.exe"
$args = @(
  "-batchmode",
  "-nographics",
  "-arenaSmokeTest",
  "-logFile",
  "C:\Users\lyush\Desktop\삶\GameForMe\standalone_size_cleanup_smoke.log"
)
$p = Start-Process -FilePath $exe -ArgumentList $args -Wait -PassThru -WindowStyle Hidden
exit $p.ExitCode
```

4. Run one manual 3-5 minute arena session:
   - Player sprite visible.
   - Enemy sprites visible.
   - Skill icons visible.
   - Projectiles and skill VFX visible.
   - Loot/equipment UI still works.
   - No pink/missing materials.
5. Re-measure:

```powershell
Get-ChildItem -Force -Recurse -File |
  Measure-Object Length -Sum
```

## Recommended First Pass

1. Update `.gitignore` with root debug/log/screenshot patterns.
2. Confirm `Library/`, `Builds/`, `Logs/`, and `UserSettings/` are not tracked.
3. Clean local `Library/`, `Builds/`, root logs, and root debug screenshots after approval.
4. Reopen Unity and rebuild only when needed.

Expected project folder after this safe pass: about 60.64 MiB.

Recommended second pass after playtesting:

1. Move `Assets/Art/Generated/_Source` outside `Assets`, or put it in Git LFS.
2. Apply texture import compression/downscale settings to generated sprites.
3. Rebuild and compare visual quality.

Expected source project after moving raw `_Source` out of project: about 15.04 MiB. Expected Windows build after texture import optimization: likely 90-105 MiB, depending on acceptable sprite quality.
