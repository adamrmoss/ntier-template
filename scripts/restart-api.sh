#!/usr/bin/env bash
# Restart the NTierTemplate.Api systemd service.
set -euo pipefail

sudo systemctl restart ntier-template-api
