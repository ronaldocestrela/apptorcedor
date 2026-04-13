// CD na VPS: GitHub Actions CI dispara este job após sucesso; build ocorre na VPS (git pull + publish + Vite).
// Segredos de runtime da API são gravados em /etc/apptorcedor/api.env a partir do Jenkins Credentials.
//
// Credenciais esperadas (IDs podem ser ajustados no job ou via folder credentials):
// - api-connection-string     (Secret text) ConnectionStrings__DefaultConnection
// - api-jwt-key               (Secret text) Jwt__Key
// - api-admin-password        (Secret text) ADMIN_MASTER_INITIAL_PASSWORD
// - api-webhook-secret        (Secret text) Payments__WebhookSecret
// - api-cors-origin           (Secret text) Cors__AllowedOrigins__0
// - api-aspnetcore-urls       (Secret text) ASPNETCORE_URLS (ex.: http://127.0.0.1:5031)
// - vite-public-api-url       (Secret text) URL pública da API para build do Vite (gravada também no arquivo vite na VPS)
// - github-token-deploy-status (Secret text) PAT com escopo repo:status (commit statuses)
// - vps-ssh-key               (SSH Username with private key)
// - vps-host                  (Secret text) hostname ou IP
//
// Variáveis de job (opcional, não secret): DEPLOY_ROOT, APP_SERVICE_NAME, APP_HEALTHCHECK_URL, VPS_PORT,
// DEPLOY_BRANCH, VPS_REPO_DIR

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
        withCredentials([
          string(credentialsId: 'api-connection-string', variable: 'API_CONN'),
          string(credentialsId: 'api-jwt-key', variable: 'API_JWT_KEY'),
          string(credentialsId: 'api-admin-password', variable: 'API_ADMIN_PW'),
          string(credentialsId: 'api-webhook-secret', variable: 'API_WEBHOOK'),
          string(credentialsId: 'api-cors-origin', variable: 'API_CORS'),
          string(credentialsId: 'api-aspnetcore-urls', variable: 'API_ASPNET_URLS'),
          string(credentialsId: 'vite-public-api-url', variable: 'VITE_API_URL'),
          sshUserPrivateKey(
            credentialsId: 'vps-ssh-key',
            keyFileVariable: 'SSH_KEY',
            usernameVariable: 'SSH_USER'
          ),
          string(credentialsId: 'vps-host', variable: 'VPS_HOST')
        ]) {
          script {
            env.RELEASE_ID = sh(
              returnStdout: true,
              script: 'echo "$(date -u +%Y%m%d%H%M%S)-$(git rev-parse --short HEAD)"'
            ).trim()
          }
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
              echo 'Google__Auth__ClientId='
            } > "${API_ENV_LOCAL}"
            printf '%s' "${VITE_API_URL}" > "${VITE_LOCAL}"
            scp -i "${SSH_KEY}" -P "${VPS_PORT}" -o StrictHostKeyChecking=accept-new \
              "${API_ENV_LOCAL}" "${SSH_USER}@${VPS_HOST}:${REMOTE_ENV}"
            scp -i "${SSH_KEY}" -P "${VPS_PORT}" -o StrictHostKeyChecking=accept-new \
              "${VITE_LOCAL}" "${SSH_USER}@${VPS_HOST}:${REMOTE_VITE}"
            scp -i "${SSH_KEY}" -P "${VPS_PORT}" -o StrictHostKeyChecking=accept-new \
              deploy/vps/build-and-deploy.sh "${SSH_USER}@${VPS_HOST}:${REMOTE_SH}"
            rm -f "${API_ENV_LOCAL}" "${VITE_LOCAL}"
            ssh -i "${SSH_KEY}" -p "${VPS_PORT}" -o StrictHostKeyChecking=accept-new \
              "${SSH_USER}@${VPS_HOST}" \
              "set -eu; sudo mkdir -p /etc/apptorcedor; sudo install -m 600 -o root -g root \"${REMOTE_ENV}\" /etc/apptorcedor/api.env; sudo bash \"${REMOTE_SH}\" \"${DEPLOY_BRANCH}\" \"${REMOTE_VITE}\" \"${DEPLOY_ROOT}\" \"${APP_SERVICE_NAME}\" \"${APP_HEALTHCHECK_URL}\" \"${RELEASE_ID}\" \"${VPS_REPO_DIR}\""
          '''
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
