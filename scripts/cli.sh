#!/usr/bin/env bash
# Run NTierTemplate.Cli using a launch profile from Properties/launchSettings.json.
# Requires NTierTemplate.Cli/clisettings.json (create via ./scripts/create-settings.sh).
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
profile="${1:-help}"
project="$repo_root/NTierTemplate.Cli/NTierTemplate.Cli.csproj"

cd "$repo_root"

# Build quietly; on failure, rerun with errors only so problems are visible.
if ! dotnet build "$project" --nologo --verbosity quiet >/dev/null 2>&1; then
    dotnet build "$project" --nologo -clp:ErrorsOnly >&2
    exit 1
fi

# --no-build avoids repeating MSBuild output; profile supplies commandLineArgs.
dotnet run --project "$project" --launch-profile "$profile" --no-build --verbosity quiet
