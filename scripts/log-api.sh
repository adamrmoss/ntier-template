#!/usr/bin/env bash
# Follow NTierTemplate.Api systemd journal logs.
set -euo pipefail

journalctl -u ntier-template-api -f
