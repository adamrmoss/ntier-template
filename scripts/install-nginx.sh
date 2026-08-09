#!/usr/bin/env bash
# Install nginx.conf from the repo into sites-available and enable the site.
#
# Copies nginx.conf to /etc/nginx/sites-available/ntier-template, symlinks it
# into sites-enabled, removes the default site, validates, and reloads nginx.
#
# Requires nginx to be installed (see docs/setup.md).
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
site_name="ntier-template"
source_conf="$repo_root/nginx.conf"
target_available="/etc/nginx/sites-available/$site_name"
target_enabled="/etc/nginx/sites-enabled/$site_name"

if [[ ! -f "$source_conf" ]]; then
    echo "nginx.conf not found: $source_conf" >&2
    exit 1
fi

sudo cp "$source_conf" "$target_available"
sudo ln -sf "$target_available" "$target_enabled"
sudo rm -f /etc/nginx/sites-enabled/default
sudo nginx -t
sudo systemctl reload nginx
