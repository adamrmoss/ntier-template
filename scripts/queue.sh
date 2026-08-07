#!/usr/bin/env bash
# Run the NTierTemplate.Queue RabbitMQ worker host.
#
# Requires NTierTemplate.Queue/queuesettings.json (create via ./scripts/create-settings.sh)
# and a running RabbitMQ broker (see README.md).
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/NTierTemplate.Queue/NTierTemplate.Queue.csproj"

cd "$repo_root"

if ! dotnet build "$project" --nologo --verbosity quiet >/dev/null 2>&1; then
    dotnet build "$project" --nologo -clp:ErrorsOnly >&2
    exit 1
fi

dotnet run --project "$project" --no-build --verbosity quiet
