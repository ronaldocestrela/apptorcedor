#!/usr/bin/env bash
# Deploy com Docker Compose: git pull, .env para compose, build de imagens e up.
# Uso:
#   build-and-deploy-compose.sh <branch> <compose_env_file> <health_url> <release_id> <repo_dir> [compose_file]
#
# Pré-requisitos na VPS: git, Docker Engine + plugin "docker compose" (ou docker-compose v1),
# utilizador com permissão para docker (grupo docker) ou sudo sem password para docker.
set -euo pipefail

BRANCH="${1:-}"
COMPOSE_ENV_SRC="${2:-}"
HEALTH_URL="${3:-http://127.0.0.1:5031/health/live}"
RELEASE_ID="${4:-}"
REPO_DIR="${5:-}"
COMPOSE_FILE="${6:-docker-compose.yml}"

if [[ -z "$BRANCH" || -z "$COMPOSE_ENV_SRC" || -z "$RELEASE_ID" || -z "$REPO_DIR" ]]; then
  echo "Uso: $0 <branch> <compose_env_file> <health_url> <release_id> <repo_dir> [compose_file]" >&2
  exit 1
fi

if [[ ! -f "$COMPOSE_ENV_SRC" ]]; then
  echo "Arquivo de ambiente não encontrado: $COMPOSE_ENV_SRC" >&2
  exit 1
fi

if [[ ! -d "$REPO_DIR/.git" ]]; then
  echo "Repositório git não encontrado em: $REPO_DIR" >&2
  exit 1
fi

if ! command -v docker >/dev/null 2>&1; then
  echo "Comando 'docker' não encontrado. Instale Docker Engine." >&2
  exit 127
fi

compose_cmd() {
  if docker info >/dev/null 2>&1; then
    docker compose "$@"
  else
    sudo docker compose "$@"
  fi
}

cd "$REPO_DIR"
git fetch origin
git checkout "$BRANCH"
git pull origin "$BRANCH"

COMPOSE_PATH="${REPO_DIR}/${COMPOSE_FILE}"
if [[ ! -f "$COMPOSE_PATH" ]]; then
  echo "Ficheiro Compose não encontrado: $COMPOSE_PATH" >&2
  exit 1
fi

install -m 600 "$COMPOSE_ENV_SRC" "${REPO_DIR}/.env"

echo "Compose build/up (release ${RELEASE_ID})..."
compose_cmd -f "$COMPOSE_PATH" --project-directory "$REPO_DIR" build --pull
compose_cmd -f "$COMPOSE_PATH" --project-directory "$REPO_DIR" up -d --remove-orphans

sleep 3

if ! curl -sf --max-time 30 "${HEALTH_URL}" >/dev/null; then
  echo "Health check falhou: ${HEALTH_URL}" >&2
  compose_cmd -f "$COMPOSE_PATH" --project-directory "$REPO_DIR" logs --tail 80 api || true
  exit 1
fi

echo "Deploy Compose OK: ${RELEASE_ID}"
