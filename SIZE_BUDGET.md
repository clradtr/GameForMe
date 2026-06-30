# Size Budget

This prototype is intentionally small and iteration-friendly. These budgets are guardrails, not hard shipping requirements.

## Targets

| Area | Target | Warning level | Notes |
|---|---:|---:|---|
| Source repository checkout, excluding Unity caches/builds | <= 80 MiB | > 100 MiB | Includes `Assets`, `Packages`, `ProjectSettings`, scripts, docs, and LFS pointer files if LFS is used. |
| `Assets/` total | <= 70 MiB | > 90 MiB | Current asset size is dominated by generated PNG art. |
| `Assets/Art/Generated/_Source` | 0 MiB in normal source checkout | > 10 MiB | Raw generated source art should be archived outside `Assets` or tracked with LFS if kept. |
| Exported Windows build | <= 120 MiB | > 150 MiB | Current build is around 113 MiB. |
| Individual runtime texture PNG | <= 2 MiB | > 4 MiB | Anything over 1 MiB should usually be reviewed for resolution/import settings. |
| Individual raw/source texture | <= 5 MiB | > 10 MiB | Prefer external archive or LFS. |
| Individual audio file | <= 5 MiB | > 10 MiB | No audio assets currently exist. |
| Root generated logs/screenshots | 0 MiB committed | > 1 MiB present | Should be ignored and cleaned after debugging. |

## Required Checks Before Commit

Run the Windows report:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\size_report.ps1
```

On Linux/macOS:

```bash
bash scripts/size_report.sh
```

Check Git status with ignored files visible:

```powershell
git status --short --ignored
```

Expected ignored local folders:

- `Library/`
- `Temp/`
- `Obj/`
- `Build/`
- `Builds/`
- `Logs/`
- `UserSettings/`

## Review Rules

- New generated art over 1 MiB should be checked for actual in-game display size.
- New raw art/source images should not live under `Assets` unless Unity needs to import them.
- New build outputs should stay out of Git.
- Large binary assets that must remain in the project should use Git LFS.
- Import setting changes need a visual smoke test before commit.
