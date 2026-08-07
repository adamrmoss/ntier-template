#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
profile="${1:-help}"

cd "$repo_root"
dotnet run --project NTierTemplate.Cli/ --launch-profile "$profile"
