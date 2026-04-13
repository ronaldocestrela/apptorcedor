#!/usr/bin/env bash
# Build e deploy na VPS: git pull na cópia local do repositório, dotnet publish,
# npm build (VITE_API_URL), copia SPA para wwwroot, move release, symlink current,
# systemctl restart, health check e rollback simples.
#
# Uso (na VPS, em geral via sudo):
#   build-and-deploy.sh <branch> <vite_url_file> [deploy_root] [service_name] [health_url] [release_id] [repo_dir]
#
# O arquivo vite_url_file deve conter uma única linha com a URL pública da API (VITE_API_URL).
#
# Pré-requisitos na VPS: git clone em repo_dir, .NET SDK 10, Node.js 22+, npm, curl, sudo para systemctl.
set -euo pipefail

BRANCH="${1:-}"
VITE_URL_FILE="${2:-}"
DEPLOY_ROOT="${3:-/opt/apptorcedor}"
SERVICE_NAME="${4:-apptorcedor-api}"
HEALTH_URL="${5:-http://127.0.0.1:5031/health/live}"
RELEASE_ID="${6:-}"
REPO_DIR="${7:-/opt/apptorcedor/repo}"

if [[ -z "$BRANCH" || -z "$VITE_URL_FILE" || -z "$RELEASE_ID" ]]; then
  echo "Uso: $0 <branch> <vite_url_file> [deploy_root] [service_name] [health_url] [release_id] [repo_dir]" >&2
  exit 1
fi

if [[ ! -f "$VITE_URL_FILE" ]]; then
  echo "Arquivo VITE não encontrado: $VITE_URL_FILE" >&2
  exit 1
fi

if [[ ! -d "$REPO_DIR/.git" ]]; then
  echo "Repositório git não encontrado em: $REPO_DIR" >&2
  exit 1
fi

export VITE_API_URL
VITE_API_URL="$(tr -d '\r\n' <"$VITE_URL_FILE")"
if [[ -z "$VITE_API_URL" ]]; then
  echo "VITE_API_URL vazio em $VITE_URL_FILE" >&2
  exit 1
fi

cd "$REPO_DIR"
git fetch origin
git checkout "$BRANCH"
git pull origin "$BRANCH"

STAGE="${DEPLOY_ROOT}/.build-staging-${RELEASE_ID}"
rm -rf "$STAGE"
mkdir -p "$STAGE/api/wwwroot"

dotnet publish "${REPO_DIR}/backend/src/AppTorcedor.Api/AppTorcedor.Api.csproj" -c Release -o "$STAGE/api"

pushd "${REPO_DIR}/frontend" >/dev/null
npm ci
npm run build
popd >/dev/null

cp -a "${REPO_DIR}/frontend/dist/." "$STAGE/api/wwwroot/"

mkdir -p "${DEPLOY_ROOT}/releases"
TARGET="${DEPLOY_ROOT}/releases/${RELEASE_ID}"

if [[ -d "$TARGET" ]]; then
  echo "Release ${RELEASE_ID} já existe; sobrescrevendo conteúdo..." >&2
  rm -rf "${TARGET}"
fi
mkdir -p "$TARGET"
cp -a "$STAGE/api/." "$TARGET/"
rm -rf "$STAGE"

PREVIOUS=""
if [[ -L "${DEPLOY_ROOT}/current" ]]; then
  PREVIOUS="$(readlink -f "${DEPLOY_ROOT}/current" || true)"
fi

rollback() {
  trap - ERR
  echo "Deploy falhou; tentando rollback..." >&2
  if [[ -n "${PREVIOUS}" && -d "${PREVIOUS}" ]]; then
    ln -sfn "${PREVIOUS}" "${DEPLOY_ROOT}/current"
    if [[ -n "${SERVICE_NAME}" ]]; then
      systemctl restart "${SERVICE_NAME}" || true
    fi
    echo "Rollback concluído para: ${PREVIOUS}" >&2
  else
    echo "Sem release anterior para rollback." >&2
  fi
  exit 1
}

trap 'rollback' ERR

ln -sfn "${TARGET}" "${DEPLOY_ROOT}/current"

if [[ -n "${SERVICE_NAME}" ]]; then
  systemctl restart "${SERVICE_NAME}"
  sleep 2
fi

if ! curl -sf --max-time 30 "${HEALTH_URL}" >/dev/null; then
  echo "Health check falhou: ${HEALTH_URL}" >&2
  rollback
fi

trap - ERR
echo "Build e deploy OK: ${RELEASE_ID} -> ${TARGET}"
