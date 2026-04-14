"""Unit tests for the local CI runner script."""

from __future__ import annotations

import subprocess
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPO_ROOT / "scripts" / "ci-local.sh"


class TestLocalCiScript(unittest.TestCase):
    def test_dry_run_lists_all_github_ci_gates(self) -> None:
        result = subprocess.run(
            [str(SCRIPT_PATH), "--dry-run"],
            check=False,
            capture_output=True,
            text=True,
            cwd=REPO_ROOT,
        )

        self.assertEqual(result.returncode, 0, msg=result.stderr)
        stdout = result.stdout

        self.assertIn("Backend (.NET)", stdout)
        self.assertIn(
            "dotnet restore tests/AppTorcedor.Api.Tests/AppTorcedor.Api.Tests.csproj",
            stdout,
        )
        self.assertIn(
            "dotnet test tests/AppTorcedor.Application.Tests/AppTorcedor.Application.Tests.csproj -c Release --no-build --verbosity normal",
            stdout,
        )
        self.assertIn("Frontend (Node)", stdout)
        self.assertIn("npm ci", stdout)
        self.assertIn("npm run build", stdout)
        self.assertIn("Deploy/CD tooling", stdout)
        self.assertIn("bash -n deploy/vps/deploy.sh", stdout)
        self.assertIn(
            "python3 -m unittest discover -s deploy/ci -p 'test_*.py' -v",
            stdout,
        )
        self.assertIn("Docker Compose (config)", stdout)
        self.assertIn(
            "docker compose --env-file .env.compose.example config --quiet",
            stdout,
        )


if __name__ == "__main__":
    unittest.main()