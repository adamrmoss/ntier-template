#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
example_file="$repo_root/NTierTemplate.Data/example.dbsettings.json"
target_file="$repo_root/NTierTemplate.Data/dbsettings.json"

if [[ ! -f "$example_file" ]]; then
    echo "Missing example settings: $example_file" >&2
    exit 1
fi

if [[ -f "$target_file" ]]; then
    echo "Already exists: $target_file" >&2
    echo "Remove it first if you want to recreate from the example file." >&2
    exit 1
fi

cp "$example_file" "$target_file"
echo "Created $target_file from example.dbsettings.json"
echo "Edit the password (and other values if needed) before running migrations."
