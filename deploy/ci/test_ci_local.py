"""Unit tests for the local CI runner script."""

from __future__ import annotations

import subprocess
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
SCRIPT_PATH = REPO_ROOT / "scripts" / "ci-local.sh"


class TestLocalCiScript(unittest.TestCase):
    def test_dry_run_lists_only_backend_and_frontend_tests(self) -> None:
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
            "dotnet test tests/AppTorcedor.Api.Tests/AppTorcedor.Api.Tests.csproj -c Release --verbosity normal",
            stdout,
        )
        self.assertIn(
            "dotnet test tests/AppTorcedor.Identity.Tests/AppTorcedor.Identity.Tests.csproj -c Release --verbosity normal",
            stdout,
        )
        self.assertIn(
            "dotnet test tests/AppTorcedor.Application.Tests/AppTorcedor.Application.Tests.csproj -c Release --verbosity normal",
            stdout,
        )
        self.assertIn("Frontend (Node)", stdout)
        self.assertIn("cd frontend && npm test", stdout)

        self.assertNotIn("dotnet restore", stdout)
        self.assertNotIn("dotnet build", stdout)
        self.assertNotIn("npm ci", stdout)
        self.assertNotIn("npm run lint", stdout)
        self.assertNotIn("npm run build", stdout)
        self.assertNotIn("Deploy/CD tooling", stdout)
        self.assertNotIn("bash -n", stdout)
        self.assertNotIn("python3 -m unittest", stdout)
        self.assertNotIn("Docker Compose (config)", stdout)
        self.assertNotIn("docker compose", stdout)


if __name__ == "__main__":
    unittest.main()