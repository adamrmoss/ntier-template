#!/usr/bin/env bash
# Build ntier-template-web for production and deploy static assets to /opt/ntier-template/web.
#
# Runs `npm run build` in ntier-template-web/, then rsyncs
# dist/ntier-template-web/browser/ to the host web root (requires sudo).
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
web_dir="$repo_root/ntier-template-web"
source_dir="$web_dir/dist/ntier-template-web/browser"
target_web_dir="/opt/ntier-template/web"

cd "$web_dir"
npm run build

if [[ ! -d "$source_dir" ]]; then
    echo "Build output not found: $source_dir" >&2
    exit 1
fi

# Publish the production bundle; --delete removes stale assets from prior deploys.
sudo mkdir -p "$target_web_dir"
sudo rsync -a --delete "${source_dir}/" "${target_web_dir}/"
sudo chown -R amoss:amoss "$target_web_dir"
