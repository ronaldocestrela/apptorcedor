"""Regression tests for Jenkins local deploy workspace handling."""

from __future__ import annotations

import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
JENKINSFILE_PATH = REPO_ROOT / "Jenkinsfile"


class TestJenkinsfileLocalDeploy(unittest.TestCase):
    def test_local_deploy_uses_dedicated_repo_dir_instead_of_workspace(self) -> None:
        content = JENKINSFILE_PATH.read_text(encoding="utf-8")

        self.assertIn(
            'sudo bash "${REMOTE_SH}" "${DEPLOY_BRANCH}" "${REMOTE_COMPOSE_ENV}" "${APP_HEALTHCHECK_URL}" "${RELEASE_ID}" "${VPS_REPO_DIR}" "${COMPOSE_FILE}"',
            content,
        )
        self.assertIn(
            'bash "${REMOTE_SH}" "${DEPLOY_BRANCH}" "${REMOTE_VITE}" "${DEPLOY_ROOT}" "${APP_SERVICE_NAME}" "${APP_HEALTHCHECK_URL}" "${RELEASE_ID}" "${VPS_REPO_DIR}"',
            content,
        )
        self.assertNotIn(
            'sudo bash "${REMOTE_SH}" "${DEPLOY_BRANCH}" "${REMOTE_COMPOSE_ENV}" "${APP_HEALTHCHECK_URL}" "${RELEASE_ID}" "${WORKSPACE}" "${COMPOSE_FILE}"',
            content,
        )
        self.assertNotIn(
            'bash "${REMOTE_SH}" "${DEPLOY_BRANCH}" "${REMOTE_VITE}" "${DEPLOY_ROOT}" "${APP_SERVICE_NAME}" "${APP_HEALTHCHECK_URL}" "${RELEASE_ID}" "${WORKSPACE}"',
            content,
        )


if __name__ == "__main__":
    unittest.main()
