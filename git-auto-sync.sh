#!/usr/bin/env bash
set -euo pipefail

# GameForMe automatic Git pull/commit/push helper.
# Usage:
#   ./git-auto-sync.sh ["commit message"]
#
# What it does:
#   1. Verifies this folder is a Git repository with a configured remote.
#   2. Pulls the current branch from its upstream using rebase + autostash.
#   3. Commits local changes, if any, with the provided message.
#   4. Pushes the current branch to its upstream.

COMMIT_MESSAGE=${1:-"Auto sync game prototype"}

if ! git rev-parse --is-inside-work-tree >/dev/null 2>&1; then
  echo "ERROR: Run this script inside the GameForMe Git repository." >&2
  exit 1
fi

REPO_ROOT=$(git rev-parse --show-toplevel)
cd "$REPO_ROOT"

CURRENT_BRANCH=$(git branch --show-current)
if [[ -z "$CURRENT_BRANCH" ]]; then
  echo "ERROR: Detached HEAD 상태입니다. 브랜치로 체크아웃한 뒤 다시 실행하세요." >&2
  exit 1
fi

if ! git remote | grep -q .; then
  echo "ERROR: Git remote가 설정되어 있지 않습니다." >&2
  echo "먼저 예: git remote add origin <repository-url> 를 실행한 뒤 다시 시도하세요." >&2
  exit 1
fi

UPSTREAM=$(git rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>/dev/null || true)
if [[ -z "$UPSTREAM" ]]; then
  DEFAULT_REMOTE=$(git remote | head -n 1)
  echo "현재 브랜치 '$CURRENT_BRANCH'에 upstream이 없어 '$DEFAULT_REMOTE/$CURRENT_BRANCH'로 설정합니다."
  git branch --set-upstream-to="$DEFAULT_REMOTE/$CURRENT_BRANCH" "$CURRENT_BRANCH" 2>/dev/null || true
  UPSTREAM=$(git rev-parse --abbrev-ref --symbolic-full-name '@{u}' 2>/dev/null || true)
fi

if [[ -n "$UPSTREAM" ]]; then
  echo "==> Pulling latest changes from $UPSTREAM"
  git pull --rebase --autostash
else
  echo "WARNING: upstream 브랜치를 확인할 수 없어 pull을 건너뜁니다. push 시 upstream을 설정합니다."
fi

if [[ -n "$(git status --porcelain)" ]]; then
  echo "==> Committing local changes"
  git add -A
  git commit -m "$COMMIT_MESSAGE"
else
  echo "==> No local changes to commit"
fi

echo "==> Pushing $CURRENT_BRANCH"
if [[ -n "$UPSTREAM" ]]; then
  git push
else
  DEFAULT_REMOTE=$(git remote | head -n 1)
  git push -u "$DEFAULT_REMOTE" "$CURRENT_BRANCH"
fi

echo "==> Sync complete"
