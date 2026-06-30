#!/usr/bin/env bash
set -euo pipefail

ROOT="${1:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"
TOP_FILES="${TOP_FILES:-50}"
TOP_FOLDERS="${TOP_FOLDERS:-30}"

format_kib() {
  local kib="$1"
  awk -v kib="$kib" 'BEGIN {
    bytes = kib * 1024
    if (bytes >= 1073741824) printf "%.2f GiB", bytes / 1073741824
    else if (bytes >= 1048576) printf "%.2f MiB", bytes / 1048576
    else if (bytes >= 1024) printf "%.2f KiB", bytes / 1024
    else printf "%d B", bytes
  }'
}

folder_kib() {
  local path="$1"
  if [ -e "$path" ]; then
    du -sk "$path" 2>/dev/null | awk '{print $1}'
  else
    echo 0
  fi
}

total_kib="$(folder_kib "$ROOT")"
file_count="$(find "$ROOT" -type f 2>/dev/null | wc -l | tr -d ' ')"

echo "# Size Report"
echo
echo "Root: $ROOT"
echo "Generated: $(date '+%Y-%m-%d %H:%M:%S')"
echo "Total: $(format_kib "$total_kib")"
echo "Files: $file_count"
echo

echo "## Selected Folders"
echo
echo "| Path | Size |"
echo "|---|---:|"
for folder in \
  "Assets" \
  "Assets/Art" \
  "Assets/Art/Generated" \
  "Assets/Art/Generated/_Source" \
  "Packages" \
  "ProjectSettings" \
  "Library" \
  "Builds" \
  "Logs" \
  "UserSettings"; do
  kib="$(folder_kib "$ROOT/$folder")"
  echo "| \`$folder\` | $(format_kib "$kib") |"
done
echo

echo "## Top $TOP_FILES Files"
echo
echo "| Path | Size |"
echo "|---|---:|"
find "$ROOT" -type f -printf '%s\t%p\n' 2>/dev/null |
  sort -nr |
  head -n "$TOP_FILES" |
  while IFS=$'\t' read -r bytes path; do
    rel="${path#$ROOT/}"
    kib=$(( (bytes + 1023) / 1024 ))
    echo "| \`$rel\` | $(format_kib "$kib") |"
  done
echo

echo "## Top $TOP_FOLDERS Folders"
echo
echo "| Path | Size |"
echo "|---|---:|"
find "$ROOT" -type d 2>/dev/null |
  while read -r dir; do
    [ "$dir" = "$ROOT" ] && continue
    kib="$(folder_kib "$dir")"
    rel="${dir#$ROOT/}"
    printf '%s\t%s\n' "$kib" "$rel"
  done |
  sort -nr |
  head -n "$TOP_FOLDERS" |
  while IFS=$'\t' read -r kib rel; do
    echo "| \`$rel\` | $(format_kib "$kib") |"
  done

