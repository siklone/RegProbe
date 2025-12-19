#!/usr/bin/env bash
set -euo pipefail

BASE_BRANCH="${BASE_BRANCH:-main}"
COMMENT_BODY="${CODEX_COMMENT:-@codex review}"

cd "$(git rev-parse --show-toplevel)"

branch="$(git rev-parse --abbrev-ref HEAD)"
if [[ "$branch" == "main" || "$branch" == "master" ]]; then
  echo "Refusing to open a PR from $branch. Create a feature branch first." >&2
  exit 1
fi

if [[ -n "$(git status --porcelain)" ]]; then
  echo "Working tree is dirty. Commit or stash before creating a PR." >&2
  exit 1
fi

if ! command -v gh >/dev/null 2>&1; then
  echo "GitHub CLI (gh) is not installed." >&2
  echo "Install: https://github.com/cli/cli#installation" >&2
  exit 1
fi

if ! gh auth status >/dev/null 2>&1; then
  echo "gh is not authenticated. Run: gh auth login" >&2
  exit 1
fi

title="${1:-$(git log -1 --pretty=%s)}"
body="${2:-Automated PR via scripts/codex_pr.sh}"

git push -u origin "$branch"

pr_url="$(gh pr view --json url -q .url 2>/dev/null || true)"
if [[ -z "$pr_url" ]]; then
  pr_url="$(gh pr create --title "$title" --body "$body" --base "$BASE_BRANCH" --head "$branch")"
fi

gh pr comment "$pr_url" --body "$COMMENT_BODY"

echo "PR: $pr_url"
