// CD na VPS: GitHub Actions CI dispara este job após sucesso.
// DEPLOY_USE_COMPOSE=true (padrão): docker compose build/up (API + web em containers).
// DEPLOY_USE_COMPOSE=false: fluxo legado git pull + dotnet publish + npm + systemd.
// Segredos são gravados em /etc/apptorcedor/api.env (referência) e .env na raiz do repo para Compose.
//
// Credenciais esperadas (IDs podem ser ajustados no job ou via folder credentials):
// - api-connection-string     (Secret text) ConnectionStrings__DefaultConnection
// - api-jwt-key               (Secret text) Jwt__Key
// - api-admin-password        (Secret text) ADMIN_MASTER_INITIAL_PASSWORD
// - api-webhook-secret        (Secret text) Payments__WebhookSecret / PAYMENTS_WEBHOOK_SECRET (callback legacy; não é whsec do Stripe)
// - stripe-api-key            (Secret text) STRIPE_API_KEY (sk_test_/sk_live_; use Secret vazio se só Mock)
// - stripe-webhook-secret     (Secret text) STRIPE_WEBHOOK_SECRET (whsec_; Secret vazio se só Mock)
// - payments-provider         (Secret text) PAYMENTS_PROVIDER — Mock ou Stripe
// - stripe-success-url        (Secret text) STRIPE_SUCCESS_URL (HTTPS da SPA após Checkout; vazio se não usar)
// - stripe-cancel-url         (Secret text) STRIPE_CANCEL_URL (HTTPS ao cancelar Checkout; vazio se não usar)
// - api-cors-origin           (Secret text) Cors__AllowedOrigins__0
// - api-aspnetcore-urls       (Secret text) ASPNETCORE_URLS (ex.: http://127.0.0.1:5031)
// - vite-public-api-url       (Secret text) URL pública da API para build do Vite (gravada também no arquivo vite na VPS)
// - github-token-deploy-status (Secret text) PAT com escopo repo:status (commit statuses)
//
// Quando Jenkins corre noutra máquina e faz SSH para a VPS (JENKINS_LOCAL_DEPLOY=false):
// - vps-ssh-key               (SSH Username with private key)
// - vps-host                  (Secret text) hostname ou IP (sem https://)
//
// Variáveis de job (opcional): DEPLOY_ROOT, APP_SERVICE_NAME, APP_HEALTHCHECK_URL, VPS_PORT,
// DEPLOY_BRANCH, VPS_REPO_DIR, JENKINS_LOCAL_DEPLOY, NODEJS_HOME, DEPLOY_USE_COMPOSE, COMPOSE_FILE, API_PORT, WEB_PORT

pipeline {
  agent any

  options {
    buildDiscarder(logRotator(numToKeepStr: '20'))
    timestamps()
  }

  environment {
    DEPLOY_BRANCH = "${env.DEPLOY_BRANCH ?: 'single-tenant'}"
    DOTNET_NOLOGO = 'true'
    DOTNET_CLI_TELEMETRY_OPTOUT = 'true'
    DEPLOY_ROOT = "${env.DEPLOY_ROOT ?: '/opt/apptorcedor'}"
    APP_SERVICE_NAME = "${env.APP_SERVICE_NAME ?: 'apptorcedor-api'}"
    APP_HEALTHCHECK_URL = "${env.APP_HEALTHCHECK_URL ?: 'http://127.0.0.1:5031/health/live'}"
    VPS_PORT = "${env.VPS_PORT ?: '22'}"
    VPS_REPO_DIR = "${env.VPS_REPO_DIR ?: '/opt/apptorcedor/repo'}"
    // true = sem scp/ssh; usa VPS_REPO_DIR como clone dedicado de deploy na mesma VPS,
    // evitando alterar permissões do WORKSPACE do Jenkins com comandos via sudo.
    JENKINS_LOCAL_DEPLOY = "${env.JENKINS_LOCAL_DEPLOY ?: 'true'}"
    // Opcional: ex. saída de `dirname $(nvm which node)` ou /usr (Node de sistema). Só fluxo systemd (DEPLOY_USE_COMPOSE=false).
    NODEJS_HOME = "${env.NODEJS_HOME ?: ''}"
    DEPLOY_USE_COMPOSE = "${env.DEPLOY_USE_COMPOSE ?: 'true'}"
    COMPOSE_FILE = "${env.COMPOSE_FILE ?: 'docker-compose.yml'}"
    API_PORT = "${env.API_PORT ?: '5031'}"
    WEB_PORT = "${env.WEB_PORT ?: '5173'}"
  }

  stages {
    stage('Checkout') {
      steps {
        checkout scm
        script {
          env.GIT_COMMIT = sh(returnStdout: true, script: 'git rev-parse HEAD').trim()
          env.GITHUB_REPOSITORY = sh(
            returnStdout: true,
            script: '''
              set -e
              u="$(git remote get-url origin)"
              echo "$u" | sed -E 's|.*github\\.com[:/]([^/]+/[^/.]+)(\\.git)?$|\\1|'
            '''
          ).trim()
        }
      }
    }

    stage('Deploy VPS') {
      when {
        expression {
          def raw = env.BRANCH_NAME ?: env.GIT_BRANCH ?: ''
          def b = raw.startsWith('origin/') ? raw.substring('origin/'.length()) : raw
          return b == env.DEPLOY_BRANCH
        }
      }
      steps {
        script {
          env.RELEASE_ID = sh(
            returnStdout: true,
            script: 'echo "$(date -u +%Y%m%d%H%M%S)-$(git rev-parse --short HEAD)"'
          ).trim()
          def localDeploy = (env.JENKINS_LOCAL_DEPLOY ?: 'true').trim().equalsIgnoreCase('true')

          def secretCreds = [
            string(credentialsId: 'api-connection-string', variable: 'API_CONN'),
            string(credentialsId: 'api-jwt-key', variable: 'API_JWT_KEY'),
            string(credentialsId: 'api-admin-password', variable: 'API_ADMIN_PW'),
            string(credentialsId: 'api-webhook-secret', variable: 'API_WEBHOOK'),
            string(credentialsId: 'api-cors-origin', variable: 'API_CORS'),
            string(credentialsId: 'api-aspnetcore-urls', variable: 'API_ASPNET_URLS'),
            string(credentialsId: 'vite-public-api-url', variable: 'VITE_API_URL'),
            string(credentialsId: 'stripe-api-key', variable: 'STRIPE_API_KEY'),
            string(credentialsId: 'stripe-webhook-secret', variable: 'STRIPE_WEBHOOK_SECRET'),
            string(credentialsId: 'payments-provider', variable: 'PAYMENTS_PROVIDER'),
            string(credentialsId: 'stripe-success-url', variable: 'STRIPE_SUCCESS_URL'),
            string(credentialsId: 'stripe-cancel-url', variable: 'STRIPE_CANCEL_URL')
          ]

          def useCompose = (env.DEPLOY_USE_COMPOSE ?: 'true').trim().equalsIgnoreCase('true')

          if (localDeploy) {
            withCredentials(secretCreds) {
              if (useCompose) {
                sh '''#!/bin/bash
                  set -euo pipefail
                  API_ENV_LOCAL="$(pwd)/api.env.jenkins.${BUILD_NUMBER}"
                  REMOTE_ENV="/tmp/apptorcedor-api.env.${BUILD_NUMBER}"
                  REMOTE_COMPOSE_ENV="/tmp/apptorcedor.compose.env.${BUILD_NUMBER}"
                  REMOTE_SH="/tmp/apptorcedor-build-deploy-compose-${BUILD_NUMBER}.sh"
                  {
                    echo "ASPNETCORE_ENVIRONMENT=Production"
                    printf 'ConnectionStrings__DefaultConnection=%s\n' "${API_CONN}"
                    printf 'Jwt__Key=%s\n' "${API_JWT_KEY}"
                    echo "Jwt__Issuer=AppTorcedor"
                    echo "Jwt__Audience=AppTorcedor"
                    printf 'Payments__WebhookSecret=%s\n' "${API_WEBHOOK}"
                    printf 'ADMIN_MASTER_INITIAL_PASSWORD=%s\n' "${API_ADMIN_PW}"
                    printf 'Cors__AllowedOrigins__0=%s\n' "${API_CORS}"
                    printf 'ASPNETCORE_URLS=%s\n' "${API_ASPNET_URLS}"
                    printf 'Payments__Provider=%s\n' "${PAYMENTS_PROVIDER}"
                    printf 'Payments__Stripe__ApiKey=%s\n' "${STRIPE_API_KEY:-}"
                    printf 'Payments__Stripe__WebhookSecret=%s\n' "${STRIPE_WEBHOOK_SECRET:-}"
                    printf 'Payments__Stripe__SuccessUrl=%s\n' "${STRIPE_SUCCESS_URL:-}"
                    printf 'Payments__Stripe__CancelUrl=%s\n' "${STRIPE_CANCEL_URL:-}"
                    echo 'Google__Auth__ClientId='
                  } > "${API_ENV_LOCAL}"
                  cp "${API_ENV_LOCAL}" "${REMOTE_ENV}"
                  {
                    printf 'DATABASE_CONNECTION_STRING=%s\n' "${API_CONN}"
                    printf 'JWT_KEY=%s\n' "${API_JWT_KEY}"
                    echo 'JWT_ISSUER=AppTorcedor'
                    echo 'JWT_AUDIENCE=AppTorcedor'
                    printf 'ADMIN_MASTER_INITIAL_PASSWORD=%s\n' "${API_ADMIN_PW}"
                    printf 'PAYMENTS_WEBHOOK_SECRET=%s\n' "${API_WEBHOOK}"
                    printf 'PAYMENTS_PROVIDER=%s\n' "${PAYMENTS_PROVIDER}"
                    printf 'STRIPE_API_KEY=%s\n' "${STRIPE_API_KEY:-}"
                    printf 'STRIPE_WEBHOOK_SECRET=%s\n' "${STRIPE_WEBHOOK_SECRET:-}"
                    printf 'STRIPE_SUCCESS_URL=%s\n' "${STRIPE_SUCCESS_URL:-}"
                    printf 'STRIPE_CANCEL_URL=%s\n' "${STRIPE_CANCEL_URL:-}"
                    printf 'CORS_ORIGIN=%s\n' "${API_CORS}"
                    printf 'VITE_API_URL=%s\n' "${VITE_API_URL}"
                    printf 'API_PORT=%s\n' "${API_PORT}"
                    printf 'WEB_PORT=%s\n' "${WEB_PORT}"
                    echo 'COMPOSE_PROJECT_NAME=apptorcedor'
                  } > "${REMOTE_COMPOSE_ENV}"
                  cp "${WORKSPACE}/deploy/vps/build-and-deploy-compose.sh" "${REMOTE_SH}"
                  chmod +x "${REMOTE_SH}"
                  rm -f "${API_ENV_LOCAL}"
                  sudo mkdir -p /etc/apptorcedor
                  sudo install -m 600 -o root -g root "${REMOTE_ENV}" /etc/apptorcedor/api.env
                  sudo bash "${REMOTE_SH}" "${DEPLOY_BRANCH}" "${REMOTE_COMPOSE_ENV}" "${APP_HEALTHCHECK_URL}" "${RELEASE_ID}" "${VPS_REPO_DIR}" "${COMPOSE_FILE}"
                '''
              } else {
                sh '''#!/bin/bash
                  set -euo pipefail
                  API_ENV_LOCAL="$(pwd)/api.env.jenkins.${BUILD_NUMBER}"
                  VITE_LOCAL="$(pwd)/vite-api-url.jenkins.${BUILD_NUMBER}"
                  REMOTE_ENV="/tmp/apptorcedor-api.env.${BUILD_NUMBER}"
                  REMOTE_VITE="/tmp/apptorcedor-vite-url.${BUILD_NUMBER}"
                  REMOTE_SH="/tmp/apptorcedor-build-deploy-${BUILD_NUMBER}.sh"
                  {
                    echo "ASPNETCORE_ENVIRONMENT=Production"
                    printf 'ConnectionStrings__DefaultConnection=%s\n' "${API_CONN}"
                    printf 'Jwt__Key=%s\n' "${API_JWT_KEY}"
                    echo "Jwt__Issuer=AppTorcedor"
                    echo "Jwt__Audience=AppTorcedor"
                    printf 'Payments__WebhookSecret=%s\n' "${API_WEBHOOK}"
                    printf 'ADMIN_MASTER_INITIAL_PASSWORD=%s\n' "${API_ADMIN_PW}"
                    printf 'Cors__AllowedOrigins__0=%s\n' "${API_CORS}"
                    printf 'ASPNETCORE_URLS=%s\n' "${API_ASPNET_URLS}"
                    printf 'Payments__Provider=%s\n' "${PAYMENTS_PROVIDER}"
                    printf 'Payments__Stripe__ApiKey=%s\n' "${STRIPE_API_KEY:-}"
                    printf 'Payments__Stripe__WebhookSecret=%s\n' "${STRIPE_WEBHOOK_SECRET:-}"
                    printf 'Payments__Stripe__SuccessUrl=%s\n' "${STRIPE_SUCCESS_URL:-}"
                    printf 'Payments__Stripe__CancelUrl=%s\n' "${STRIPE_CANCEL_URL:-}"
                    echo 'Google__Auth__ClientId='
                  } > "${API_ENV_LOCAL}"
                  printf '%s' "${VITE_API_URL}" > "${VITE_LOCAL}"
                  cp "${API_ENV_LOCAL}" "${REMOTE_ENV}"
                  cp "${VITE_LOCAL}" "${REMOTE_VITE}"
                  cp "${WORKSPACE}/deploy/vps/build-and-deploy.sh" "${REMOTE_SH}"
                  chmod +x "${REMOTE_SH}"
                  rm -f "${API_ENV_LOCAL}" "${VITE_LOCAL}"
                  if [ -n "${NODEJS_HOME:-}" ]; then
                    export PATH="${NODEJS_HOME}/bin:${PATH}"
                  fi
                  if [ -f "${HOME}/.nvm/nvm.sh" ]; then
                    export NVM_DIR="${HOME}/.nvm"
                    # shellcheck disable=SC1090
                    . "${NVM_DIR}/nvm.sh"
                  fi
                  DEPLOY_PATH="${PATH}"
                  if command -v npm >/dev/null 2>&1; then
                    DEPLOY_PATH="$(dirname "$(command -v npm)"):${DEPLOY_PATH}"
                  fi
                  if command -v dotnet >/dev/null 2>&1; then
                    DEPLOY_PATH="$(dirname "$(command -v dotnet)"):${DEPLOY_PATH}"
                  fi
                  DEPLOY_PATH="/usr/local/bin:/usr/bin:${DEPLOY_PATH}"
                  sudo mkdir -p /etc/apptorcedor
                  sudo install -m 600 -o root -g root "${REMOTE_ENV}" /etc/apptorcedor/api.env
                  sudo env PATH="${DEPLOY_PATH}" HOME="${HOME}" NVM_DIR="${NVM_DIR:-}" NODEJS_HOME="${NODEJS_HOME:-}" \
                    bash "${REMOTE_SH}" "${DEPLOY_BRANCH}" "${REMOTE_VITE}" "${DEPLOY_ROOT}" "${APP_SERVICE_NAME}" "${APP_HEALTHCHECK_URL}" "${RELEASE_ID}" "${VPS_REPO_DIR}"
                '''
              }
            }
          } else {
            withCredentials(secretCreds + [
              sshUserPrivateKey(
                credentialsId: 'vps-ssh-key',
                keyFileVariable: 'SSH_KEY',
                usernameVariable: 'SSH_USER'
              ),
              string(credentialsId: 'vps-host', variable: 'VPS_HOST')
            ]) {
              if (useCompose) {
                sh '''#!/bin/bash
                  set -euo pipefail
                  chmod 600 "${SSH_KEY}"
                  API_ENV_LOCAL="$(pwd)/api.env.jenkins.${BUILD_NUMBER}"
                  REMOTE_ENV="/tmp/apptorcedor-api.env.${BUILD_NUMBER}"
                  REMOTE_COMPOSE_ENV="/tmp/apptorcedor.compose.env.${BUILD_NUMBER}"
                  REMOTE_SH="/tmp/apptorcedor-build-deploy-compose-${BUILD_NUMBER}.sh"
                  {
                    echo "ASPNETCORE_ENVIRONMENT=Production"
                    printf 'ConnectionStrings__DefaultConnection=%s\n' "${API_CONN}"
                    printf 'Jwt__Key=%s\n' "${API_JWT_KEY}"
                    echo "Jwt__Issuer=AppTorcedor"
                    echo "Jwt__Audience=AppTorcedor"
                    printf 'Payments__WebhookSecret=%s\n' "${API_WEBHOOK}"
                    printf 'ADMIN_MASTER_INITIAL_PASSWORD=%s\n' "${API_ADMIN_PW}"
                    printf 'Cors__AllowedOrigins__0=%s\n' "${API_CORS}"
                    printf 'ASPNETCORE_URLS=%s\n' "${API_ASPNET_URLS}"
                    printf 'Payments__Provider=%s\n' "${PAYMENTS_PROVIDER}"
                    printf 'Payments__Stripe__ApiKey=%s\n' "${STRIPE_API_KEY:-}"
                    printf 'Payments__Stripe__WebhookSecret=%s\n' "${STRIPE_WEBHOOK_SECRET:-}"
                    printf 'Payments__Stripe__SuccessUrl=%s\n' "${STRIPE_SUCCESS_URL:-}"
                    printf 'Payments__Stripe__CancelUrl=%s\n' "${STRIPE_CANCEL_URL:-}"
                    echo 'Google__Auth__ClientId='
                  } > "${API_ENV_LOCAL}"
                  {
                    printf 'DATABASE_CONNECTION_STRING=%s\n' "${API_CONN}"
                    printf 'JWT_KEY=%s\n' "${API_JWT_KEY}"
                    echo 'JWT_ISSUER=AppTorcedor'
                    echo 'JWT_AUDIENCE=AppTorcedor'
                    printf 'ADMIN_MASTER_INITIAL_PASSWORD=%s\n' "${API_ADMIN_PW}"
                    printf 'PAYMENTS_WEBHOOK_SECRET=%s\n' "${API_WEBHOOK}"
                    printf 'PAYMENTS_PROVIDER=%s\n' "${PAYMENTS_PROVIDER}"
                    printf 'STRIPE_API_KEY=%s\n' "${STRIPE_API_KEY:-}"
                    printf 'STRIPE_WEBHOOK_SECRET=%s\n' "${STRIPE_WEBHOOK_SECRET:-}"
                    printf 'STRIPE_SUCCESS_URL=%s\n' "${STRIPE_SUCCESS_URL:-}"
                    printf 'STRIPE_CANCEL_URL=%s\n' "${STRIPE_CANCEL_URL:-}"
                    printf 'CORS_ORIGIN=%s\n' "${API_CORS}"
                    printf 'VITE_API_URL=%s\n' "${VITE_API_URL}"
                    printf 'API_PORT=%s\n' "${API_PORT}"
                    printf 'WEB_PORT=%s\n' "${WEB_PORT}"
                    echo 'COMPOSE_PROJECT_NAME=apptorcedor'
                  } > "${REMOTE_COMPOSE_ENV}"
                  scp -i "${SSH_KEY}" -P "${VPS_PORT}" -o StrictHostKeyChecking=accept-new \
                    "${API_ENV_LOCAL}" "${SSH_USER}@${VPS_HOST}:${REMOTE_ENV}"
                  scp -i "${SSH_KEY}" -P "${VPS_PORT}" -o StrictHostKeyChecking=accept-new \
                    "${REMOTE_COMPOSE_ENV}" "${SSH_USER}@${VPS_HOST}:${REMOTE_COMPOSE_ENV}"
                  scp -i "${SSH_KEY}" -P "${VPS_PORT}" -o StrictHostKeyChecking=accept-new \
                    "${WORKSPACE}/deploy/vps/build-and-deploy-compose.sh" "${SSH_USER}@${VPS_HOST}:${REMOTE_SH}"
                  rm -f "${API_ENV_LOCAL}"
                  ssh -i "${SSH_KEY}" -p "${VPS_PORT}" -o StrictHostKeyChecking=accept-new \
                    "${SSH_USER}@${VPS_HOST}" \
                    "set -eu; sudo mkdir -p /etc/apptorcedor; sudo install -m 600 -o root -g root \"${REMOTE_ENV}\" /etc/apptorcedor/api.env; sudo bash \"${REMOTE_SH}\" \"${DEPLOY_BRANCH}\" \"${REMOTE_COMPOSE_ENV}\" \"${APP_HEALTHCHECK_URL}\" \"${RELEASE_ID}\" \"${VPS_REPO_DIR}\" \"${COMPOSE_FILE}\""
                '''
              } else {
                sh '''#!/bin/bash
                  set -euo pipefail
                  chmod 600 "${SSH_KEY}"
                  API_ENV_LOCAL="$(pwd)/api.env.jenkins.${BUILD_NUMBER}"
                  VITE_LOCAL="$(pwd)/vite-api-url.jenkins.${BUILD_NUMBER}"
                  REMOTE_ENV="/tmp/apptorcedor-api.env.${BUILD_NUMBER}"
                  REMOTE_VITE="/tmp/apptorcedor-vite-url.${BUILD_NUMBER}"
                  REMOTE_SH="/tmp/apptorcedor-build-deploy-${BUILD_NUMBER}.sh"
                  {
                    echo "ASPNETCORE_ENVIRONMENT=Production"
                    printf 'ConnectionStrings__DefaultConnection=%s\n' "${API_CONN}"
                    printf 'Jwt__Key=%s\n' "${API_JWT_KEY}"
                    echo "Jwt__Issuer=AppTorcedor"
                    echo "Jwt__Audience=AppTorcedor"
                    printf 'Payments__WebhookSecret=%s\n' "${API_WEBHOOK}"
                    printf 'ADMIN_MASTER_INITIAL_PASSWORD=%s\n' "${API_ADMIN_PW}"
                    printf 'Cors__AllowedOrigins__0=%s\n' "${API_CORS}"
                    printf 'ASPNETCORE_URLS=%s\n' "${API_ASPNET_URLS}"
                    printf 'Payments__Provider=%s\n' "${PAYMENTS_PROVIDER}"
                    printf 'Payments__Stripe__ApiKey=%s\n' "${STRIPE_API_KEY:-}"
                    printf 'Payments__Stripe__WebhookSecret=%s\n' "${STRIPE_WEBHOOK_SECRET:-}"
                    printf 'Payments__Stripe__SuccessUrl=%s\n' "${STRIPE_SUCCESS_URL:-}"
                    printf 'Payments__Stripe__CancelUrl=%s\n' "${STRIPE_CANCEL_URL:-}"
                    echo 'Google__Auth__ClientId='
                  } > "${API_ENV_LOCAL}"
                  printf '%s' "${VITE_API_URL}" > "${VITE_LOCAL}"
                  scp -i "${SSH_KEY}" -P "${VPS_PORT}" -o StrictHostKeyChecking=accept-new \
                    "${API_ENV_LOCAL}" "${SSH_USER}@${VPS_HOST}:${REMOTE_ENV}"
                  scp -i "${SSH_KEY}" -P "${VPS_PORT}" -o StrictHostKeyChecking=accept-new \
                    "${VITE_LOCAL}" "${SSH_USER}@${VPS_HOST}:${REMOTE_VITE}"
                  scp -i "${SSH_KEY}" -P "${VPS_PORT}" -o StrictHostKeyChecking=accept-new \
                    "${WORKSPACE}/deploy/vps/build-and-deploy.sh" "${SSH_USER}@${VPS_HOST}:${REMOTE_SH}"
                  rm -f "${API_ENV_LOCAL}" "${VITE_LOCAL}"
                  ssh -i "${SSH_KEY}" -p "${VPS_PORT}" -o StrictHostKeyChecking=accept-new \
                    "${SSH_USER}@${VPS_HOST}" \
                    "export NODEJS_HOME='${NODEJS_HOME:-}'; set -eu; [ -n \"\${NODEJS_HOME}\" ] && export PATH=\"\${NODEJS_HOME}/bin:\${PATH}\"; [ -f \"\${HOME}/.nvm/nvm.sh\" ] && export NVM_DIR=\"\${HOME}/.nvm\" && . \"\${HOME}/.nvm/nvm.sh\"; DPATH=\"/usr/local/bin:/usr/bin:\${PATH}\"; command -v npm >/dev/null 2>&1 && DPATH=\"\$(dirname \"\$(command -v npm)\"):\${DPATH}\"; command -v dotnet >/dev/null 2>&1 && DPATH=\"\$(dirname \"\$(command -v dotnet)\"):\${DPATH}\"; sudo mkdir -p /etc/apptorcedor; sudo install -m 600 -o root -g root \"${REMOTE_ENV}\" /etc/apptorcedor/api.env; sudo env PATH=\"\${DPATH}\" HOME=\"\${HOME}\" NVM_DIR=\"\${NVM_DIR:-}\" NODEJS_HOME=\"\${NODEJS_HOME}\" bash \"${REMOTE_SH}\" \"${DEPLOY_BRANCH}\" \"${REMOTE_VITE}\" \"${DEPLOY_ROOT}\" \"${APP_SERVICE_NAME}\" \"${APP_HEALTHCHECK_URL}\" \"${RELEASE_ID}\" \"${VPS_REPO_DIR}\""
                '''
              }
            }
          }
        }
      }
    }
  }

  post {
    success {
      script {
        def raw = env.BRANCH_NAME ?: env.GIT_BRANCH ?: ''
        def b = raw.startsWith('origin/') ? raw.substring('origin/'.length()) : raw
        if (b != env.DEPLOY_BRANCH) {
          echo "Branch não elegível para deploy (${b}); não envia status ao GitHub."
        } else {
          withCredentials([string(credentialsId: 'github-token-deploy-status', variable: 'GH_STATUS_TOKEN')]) {
            sh '''#!/bin/bash
              set -euo pipefail
              if [ -z "${GIT_COMMIT:-}" ] || [ -z "${GITHUB_REPOSITORY:-}" ]; then
                echo "GIT_COMMIT ou GITHUB_REPOSITORY ausente; pulando status no GitHub." >&2
                exit 0
              fi
              BODY="$(python3 -c "import json,os; print(json.dumps({'state':'success','context':'jenkins/cd-vps','description':'Deploy VPS OK','target_url':os.environ.get('BUILD_URL','')}))")"
              curl -fsS -X POST \
                -H "Authorization: Bearer ${GH_STATUS_TOKEN}" \
                -H "Accept: application/vnd.github+json" \
                -H "X-GitHub-Api-Version: 2022-11-28" \
                "https://api.github.com/repos/${GITHUB_REPOSITORY}/statuses/${GIT_COMMIT}" \
                -d "${BODY}"
            '''
          }
        }
      }
    }
    failure {
      script {
        def raw = env.BRANCH_NAME ?: env.GIT_BRANCH ?: ''
        def b = raw.startsWith('origin/') ? raw.substring('origin/'.length()) : raw
        if (b != env.DEPLOY_BRANCH) {
          echo "Branch não elegível para deploy (${b}); não envia status de falha ao GitHub."
        } else {
          withCredentials([string(credentialsId: 'github-token-deploy-status', variable: 'GH_STATUS_TOKEN')]) {
            sh '''#!/bin/bash
              set +e
              if [ -z "${GIT_COMMIT:-}" ] || [ -z "${GITHUB_REPOSITORY:-}" ]; then
                echo "GIT_COMMIT ou GITHUB_REPOSITORY ausente; pulando status no GitHub." >&2
                exit 0
              fi
              BODY="$(python3 -c "import json,os; print(json.dumps({'state':'failure','context':'jenkins/cd-vps','description':'Deploy VPS falhou','target_url':os.environ.get('BUILD_URL','')}))")"
              curl -fsS -X POST \
                -H "Authorization: Bearer ${GH_STATUS_TOKEN}" \
                -H "Accept: application/vnd.github+json" \
                -H "X-GitHub-Api-Version: 2022-11-28" \
                "https://api.github.com/repos/${GITHUB_REPOSITORY}/statuses/${GIT_COMMIT}" \
                -d "${BODY}"
            '''
          }
        }
      }
    }
  }
}
