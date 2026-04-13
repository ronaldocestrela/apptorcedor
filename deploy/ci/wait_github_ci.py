#!/usr/bin/env python3
"""Wait until GitHub Actions workflow run named CI completes successfully for a commit."""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import time
import urllib.error
import urllib.request
from typing import Any, Callable


def parse_repo_from_git() -> str | None:
    try:
        out = subprocess.check_output(
            ["git", "remote", "get-url", "origin"],
            stderr=subprocess.DEVNULL,
            text=True,
        ).strip()
    except (subprocess.CalledProcessError, FileNotFoundError, OSError):
        return None

    # git@github.com:owner/repo.git
    if out.startswith("git@"):
        _, _, rest = out.partition(":")
        if not rest:
            return None
        path = rest.rstrip("/").removesuffix(".git")
        return path if "/" in path else None

    # https://github.com/owner/repo.git
    if "github.com" in out:
        idx = out.find("github.com")
        tail = out[idx + len("github.com") :].lstrip("/:").rstrip("/")
        path = tail.removesuffix(".git")
        return path if "/" in path else None

    return None


FetchFn = Callable[[str, str], dict[str, Any]]


def default_fetch(url: str, token: str) -> dict[str, Any]:
    req = urllib.request.Request(
        url,
        headers={
            "Authorization": f"Bearer {token}",
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
            "User-Agent": "appTorcedor-jenkins-ci-gate",
        },
        method="GET",
    )
    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            body = resp.read().decode("utf-8")
            return json.loads(body)
    except urllib.error.HTTPError as e:
        err_body = e.read().decode("utf-8", errors="replace")
        raise RuntimeError(f"GitHub API HTTP {e.code}: {err_body}") from e


def pick_latest_ci_run(
    payload: dict[str, Any], workflow_name: str
) -> dict[str, Any] | None:
    runs = [
        r
        for r in payload.get("workflow_runs", [])
        if isinstance(r, dict) and r.get("name") == workflow_name
    ]
    if not runs:
        return None
    runs.sort(key=lambda r: str(r.get("updated_at", "")), reverse=True)
    return runs[0]


def evaluate_latest_run(
    latest: dict[str, Any],
) -> tuple[str, str | None]:
    """Returns (action, detail) where action is wait|ok|fail."""
    status = str(latest.get("status", "")).lower()
    if status in ("queued", "in_progress", "waiting", "requested", "pending"):
        return "wait", status
    if status == "completed":
        conclusion = latest.get("conclusion")
        if conclusion == "success":
            return "ok", None
        return "fail", str(conclusion or "unknown")
    return "wait", status or "unknown"


def wait_for_ci(
    *,
    repo: str,
    sha: str,
    branch: str,
    token: str,
    workflow_name: str,
    max_wait_s: int,
    poll_s: int,
    fetch_fn: FetchFn | None = None,
) -> None:
    fetch = fetch_fn or default_fetch
    deadline = time.monotonic() + max_wait_s
    url = (
        f"https://api.github.com/repos/{repo}/actions/runs"
        f"?head_sha={sha}&branch={branch}&per_page=20"
    )

    while time.monotonic() < deadline:
        payload = fetch(url, token)
        latest = pick_latest_ci_run(payload, workflow_name)
        if latest is None:
            print("Nenhuma execução do workflow CI encontrada ainda; aguardando...", flush=True)
            time.sleep(poll_s)
            continue

        action, detail = evaluate_latest_run(latest)
        if action == "ok":
            print("GitHub Actions CI concluiu com sucesso para este commit.", flush=True)
            return
        if action == "fail":
            print(
                f"GitHub Actions CI falhou (conclusão: {detail}). Deploy bloqueado.",
                file=sys.stderr,
                flush=True,
            )
            sys.exit(1)

        print(f"CI em andamento (detalhe: {detail}); aguardando...", flush=True)
        time.sleep(poll_s)

    print(
        f"Tempo esgotado ({max_wait_s}s) aguardando CI bem-sucedido para {sha}.",
        file=sys.stderr,
        flush=True,
    )
    sys.exit(1)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--repo",
        default=os.environ.get("GITHUB_REPOSITORY", "").strip() or None,
        help="owner/repo (ou env GITHUB_REPOSITORY)",
    )
    parser.add_argument(
        "--sha",
        default=os.environ.get("GIT_COMMIT", "").strip() or None,
        help="commit SHA completo (ou env GIT_COMMIT)",
    )
    parser.add_argument(
        "--branch",
        default=os.environ.get("CI_BRANCH", "single-tenant"),
        help="branch filtro da API (padrão single-tenant ou env CI_BRANCH)",
    )
    parser.add_argument(
        "--workflow-name",
        default=os.environ.get("CI_WORKFLOW_NAME", "CI"),
        help='nome do workflow em .github/workflows (campo "name:", padrão CI)',
    )
    parser.add_argument(
        "--max-wait",
        type=int,
        default=int(os.environ.get("CI_MAX_WAIT_SECONDS", "1200")),
    )
    parser.add_argument(
        "--poll",
        type=int,
        default=int(os.environ.get("CI_POLL_SECONDS", "15")),
    )
    args = parser.parse_args()

    token = os.environ.get("GITHUB_TOKEN", "").strip()
    if not token:
        print("GITHUB_TOKEN é obrigatório.", file=sys.stderr)
        sys.exit(1)

    repo = args.repo or parse_repo_from_git()
    if not repo:
        print(
            "Não foi possível determinar owner/repo; defina --repo ou GITHUB_REPOSITORY.",
            file=sys.stderr,
        )
        sys.exit(1)

    sha = args.sha
    if not sha:
        try:
            sha = subprocess.check_output(
                ["git", "rev-parse", "HEAD"], text=True
            ).strip()
        except (subprocess.CalledProcessError, FileNotFoundError, OSError):
            sha = ""

    if not sha or len(sha) < 7:
        print("Commit SHA inválido; defina --sha ou GIT_COMMIT.", file=sys.stderr)
        sys.exit(1)

    wait_for_ci(
        repo=repo,
        sha=sha,
        branch=args.branch,
        token=token,
        workflow_name=args.workflow_name,
        max_wait_s=args.max_wait,
        poll_s=args.poll,
    )


if __name__ == "__main__":
    main()
