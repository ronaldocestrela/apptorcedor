"""Unit tests for deploy/ci/wait_github_ci.py (CI gate logic)."""

from __future__ import annotations

import unittest
import unittest.mock
from unittest.mock import patch

from wait_github_ci import (
    evaluate_latest_run,
    parse_repo_from_git,
    pick_latest_ci_run,
    wait_for_ci,
)


class TestPickLatestCiRun(unittest.TestCase):
    def test_picks_newest_by_updated_at(self) -> None:
        payload = {
            "workflow_runs": [
                {
                    "name": "CI",
                    "status": "completed",
                    "conclusion": "failure",
                    "updated_at": "2025-01-01T10:00:00Z",
                },
                {
                    "name": "CI",
                    "status": "completed",
                    "conclusion": "success",
                    "updated_at": "2025-01-02T10:00:00Z",
                },
                {
                    "name": "Other",
                    "status": "completed",
                    "conclusion": "success",
                    "updated_at": "2025-01-03T10:00:00Z",
                },
            ]
        }
        latest = pick_latest_ci_run(payload, "CI")
        assert latest is not None
        self.assertEqual(latest["conclusion"], "success")

    def test_empty_runs(self) -> None:
        self.assertIsNone(pick_latest_ci_run({"workflow_runs": []}, "CI"))


class TestEvaluateLatestRun(unittest.TestCase):
    def test_success(self) -> None:
        self.assertEqual(
            evaluate_latest_run(
                {"status": "completed", "conclusion": "success"}
            ),
            ("ok", None),
        )

    def test_failure(self) -> None:
        action, detail = evaluate_latest_run(
            {"status": "completed", "conclusion": "failure"}
        )
        self.assertEqual(action, "fail")
        self.assertEqual(detail, "failure")

    def test_in_progress_waits(self) -> None:
        self.assertEqual(
            evaluate_latest_run({"status": "in_progress"}),
            ("wait", "in_progress"),
        )


class TestParseRepoFromGit(unittest.TestCase):
    @patch("wait_github_ci.subprocess.check_output")
    def test_ssh_remote(self, mock_out: unittest.mock.MagicMock) -> None:
        mock_out.return_value = "git@github.com:acme/appTorcedor.git\n"
        self.assertEqual(parse_repo_from_git(), "acme/appTorcedor")

    @patch("wait_github_ci.subprocess.check_output")
    def test_https_remote(self, mock_out: unittest.mock.MagicMock) -> None:
        mock_out.return_value = "https://github.com/acme/appTorcedor.git\n"
        self.assertEqual(parse_repo_from_git(), "acme/appTorcedor")


class TestWaitForCi(unittest.TestCase):
    def test_exits_ok_on_success_immediately(self) -> None:
        def fetch(_url: str, _token: str) -> dict:
            return {
                "workflow_runs": [
                    {
                        "name": "CI",
                        "status": "completed",
                        "conclusion": "success",
                        "updated_at": "2025-01-02T10:00:00Z",
                    }
                ]
            }

        wait_for_ci(
            repo="o/r",
            sha="abc1234",
            branch="single-tenant",
            token="t",
            workflow_name="CI",
            max_wait_s=5,
            poll_s=1,
            fetch_fn=fetch,
        )

    def test_exits_fail_on_completed_failure(self) -> None:
        def fetch(_url: str, _token: str) -> dict:
            return {
                "workflow_runs": [
                    {
                        "name": "CI",
                        "status": "completed",
                        "conclusion": "failure",
                        "updated_at": "2025-01-02T10:00:00Z",
                    }
                ]
            }

        with self.assertRaises(SystemExit) as ctx:
            wait_for_ci(
                repo="o/r",
                sha="abc1234",
                branch="single-tenant",
                token="t",
                workflow_name="CI",
                max_wait_s=5,
                poll_s=1,
                fetch_fn=fetch,
            )
        self.assertEqual(ctx.exception.code, 1)


if __name__ == "__main__":
    unittest.main()
