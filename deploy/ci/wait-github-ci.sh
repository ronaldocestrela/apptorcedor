#!/usr/bin/env bash
# Aguarda o workflow GitHub Actions "CI" concluir com sucesso para o commit atual.
# Requer: GITHUB_TOKEN (Jenkins Secret text), opcionalmente GITHUB_REPOSITORY, GIT_COMMIT, CI_BRANCH.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec python3 "${ROOT}/wait_github_ci.py" "$@"
