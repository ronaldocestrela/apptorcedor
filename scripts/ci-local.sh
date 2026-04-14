#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DRY_RUN=false

if [[ "${1:-}" == "--dry-run" ]]; then
  DRY_RUN=true
  shift
fi

if [[ $# -gt 0 ]]; then
  echo "Usage: scripts/ci-local.sh [--dry-run]" >&2
  exit 1
fi

run_step() {
  local title="$1"
  shift

  printf '\n==> %s\n' "$title"

  while [[ $# -gt 0 ]]; do
    local cmd="$1"
    shift

    if [[ "$DRY_RUN" == true ]]; then
      printf '[dry-run] %s\n' "$cmd"
    else
      printf '+ %s\n' "$cmd"
      bash -lc "cd \"$ROOT_DIR\" && $cmd"
    fi
  done
}

run_step "Backend (.NET)" \
  "cd backend && dotnet test tests/AppTorcedor.Api.Tests/AppTorcedor.Api.Tests.csproj -c Release --verbosity normal" \
  "cd backend && dotnet test tests/AppTorcedor.Identity.Tests/AppTorcedor.Identity.Tests.csproj -c Release --verbosity normal" \
  "cd backend && dotnet test tests/AppTorcedor.Application.Tests/AppTorcedor.Application.Tests.csproj -c Release --verbosity normal"

run_step "Frontend (Node)" \
  "cd frontend && npm test"

printf '\nLocal CI finished successfully.\n'