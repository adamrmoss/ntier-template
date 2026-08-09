#!/usr/bin/env bash
# Install systemd unit files from NTierTemplate.Api and NTierTemplate.Queue.
#
# Copies each *.service file from its project directory to /etc/systemd/system/,
# reloads systemd, and enables the units (does not start them).
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

units=(
    "$repo_root/NTierTemplate.Api/ntier-template-api.service"
    "$repo_root/NTierTemplate.Queue/ntier-template-queue.service"
)

for source_unit in "${units[@]}"
do
    if [[ ! -f "$source_unit" ]]; then
        echo "Unit file not found: $source_unit" >&2
        exit 1
    fi

    service_name="$(basename "$source_unit" .service)"
    target_unit="/etc/systemd/system/${service_name}.service"

    sudo cp "$source_unit" "$target_unit"
done

sudo systemctl daemon-reload

for source_unit in "${units[@]}"
do
    service_name="$(basename "$source_unit" .service)"
    sudo systemctl enable "$service_name"
done
