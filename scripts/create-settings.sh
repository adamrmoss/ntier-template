#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
created=0
skipped=0

while IFS= read -r -d '' example_file; do
    dir=$(dirname "$example_file")
    base=$(basename "$example_file")
    target_file="$dir/${base#example.}"

    if [[ "$target_file" == "$example_file" ]]; then
        echo "Skipping unexpected example file name: $example_file" >&2
        continue
    fi

    if [[ -f "$target_file" ]]; then
        echo "Already exists: $target_file"
        skipped=$((skipped + 1))
        continue
    fi

    cp "$example_file" "$target_file"
    echo "Created $target_file from $base"
    created=$((created + 1))
done < <(
    find "$repo_root" \
        \( -path '*/node_modules/*' -o -path '*/bin/*' -o -path '*/obj/*' -o -path '*/.git/*' \) -prune \
        -o -name 'example.*.json' -type f -print0
)

if [[ "$created" -eq 0 && "$skipped" -eq 0 ]]; then
    echo "No example.*.json files found under $repo_root"
    exit 0
fi

if [[ "$created" -gt 0 ]]; then
    echo "Edit the new settings files with local values before use."
fi
