#!/usr/bin/env bash
# Build NTierTemplate.Api for production and deploy to /opt/ntier-template/api.
#
# Runs `dotnet publish` and rsyncs the output to the host API directory (requires sudo).
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/NTierTemplate.Api/NTierTemplate.Api.csproj"
staging_dir="$repo_root/bin/publish/api"
target_api_dir="/opt/ntier-template/api"
api_binary="$target_api_dir/NTierTemplate.Api"

cd "$repo_root"
dotnet publish "$project" -c Release -o "$staging_dir"

if [[ ! -f "$staging_dir/NTierTemplate.Api" ]]; then
    echo "Publish output not found: $staging_dir/NTierTemplate.Api" >&2
    exit 1
fi

# Publish the release bundle; --delete removes stale assemblies from prior deploys.
sudo mkdir -p "$target_api_dir"
sudo rsync -a --delete "${staging_dir}/" "${target_api_dir}/"
sudo chown -R amoss:amoss "$target_api_dir"

# Ensure systemd ExecStart target remains executable after publish.
sudo chmod +x "$api_binary"
