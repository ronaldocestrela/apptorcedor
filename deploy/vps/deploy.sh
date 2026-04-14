#!/usr/bin/env bash
# Deploy idempotente na VPS: extrai release publicada, atualiza symlink "current",
# reinicia serviço systemd e valida health; em falha faz rollback para o release anterior.
#
# Uso (na VPS, em geral via sudo):
#   deploy.sh <archive.tgz> <release_id> [deploy_root] [service_name] [health_url]
#
# Exemplo:
#   sudo deploy.sh /tmp/release.tgz 20250413120000-abc1234 /opt/apptorcedor apptorcedor-api http://127.0.0.1:5031/health/live
#
# Variáveis de ambiente da API (ConnectionStrings, Jwt, etc.) devem estar configuradas
# no host (ex.: /etc/apptorcedor/api.env + systemd EnvironmentFile=), não neste pacote.
set -euo pipefail

ARCHIVE="${1:-}"
RELEASE_ID="${2:-}"
DEPLOY_ROOT="${3:-/opt/apptorcedor}"
SERVICE_NAME="${4:-apptorcedor-api}"
HEALTH_URL="${5:-http://127.0.0.1:5031/health/live}"

if [[ -z "$ARCHIVE" || -z "$RELEASE_ID" ]]; then
  echo "Uso: $0 <archive.tgz> <release_id> [deploy_root] [service_name] [health_url]" >&2
  exit 1
fi

if [[ ! -f "$ARCHIVE" ]]; then
  echo "Arquivo não encontrado: $ARCHIVE" >&2
  exit 1
fi

mkdir -p "${DEPLOY_ROOT}/releases"
TARGET="${DEPLOY_ROOT}/releases/${RELEASE_ID}"

if [[ -d "$TARGET" ]]; then
  echo "Release ${RELEASE_ID} já existe; sobrescrevendo conteúdo..." >&2
  rm -rf "${TARGET}"
fi
mkdir -p "$TARGET"

tar xzf "$ARCHIVE" -C "$TARGET"

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
echo "Deploy OK: ${RELEASE_ID} -> ${TARGET}"
