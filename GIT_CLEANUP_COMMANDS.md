# Git Cleanup Commands

No destructive cleanup commands were run.

Current check result: no tracked files were found under these generated/cache/build folders:

- `Library/`
- `Builds/`
- `Logs/`
- `UserSettings/`
- `Temp/`
- `Obj/`

The project directory currently appears untracked from the parent Git worktree. If you later discover that generated/cache/build files are already tracked in your own Git environment, run the following commands manually from the project root.

## Inspect First

```powershell
git status --short --ignored
git ls-files Library Builds Logs UserSettings Temp Obj
```

## Remove Generated Files From Git Tracking Only

These commands remove files from Git's index but keep local working copies on disk:

```powershell
git rm -r --cached --ignore-unmatch Library
git rm -r --cached --ignore-unmatch Temp
git rm -r --cached --ignore-unmatch Obj
git rm -r --cached --ignore-unmatch Build
git rm -r --cached --ignore-unmatch Builds
git rm -r --cached --ignore-unmatch Logs
git rm -r --cached --ignore-unmatch UserSettings
git rm -r --cached --ignore-unmatch MemoryCaptures
git rm -r --cached --ignore-unmatch Recordings
```

## Stage Gitignore Documentation

```powershell
git add .gitignore .gitattributes GITIGNORE_REPORT.md GIT_CLEANUP_COMMANDS.md
git add SIZE_AUDIT.md SIZE_OPTIMIZATION_PLAN.md SIZE_BUDGET.md
git add scripts/size_report.ps1 scripts/size_report.sh
git status
```

## Commit

```powershell
git commit -m "Add Unity project gitignore"
```

## Verify

```powershell
git status --short --ignored
git ls-files Library Builds Logs UserSettings Temp Obj
```

The final `git ls-files` command should print nothing for those generated folders.

