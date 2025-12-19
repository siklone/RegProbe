# WPF-Windows-optimizer-with-safe-reversible-tweaks

## Automation
- `scripts/codex_pr.sh` opens or updates a PR from the current branch and leaves `@codex review`.
- Requires GitHub CLI (`gh`) with `gh auth login`.
- Usage: `scripts/codex_pr.sh "Title" "Body"`.
- Optional env vars: `BASE_BRANCH`, `CODEX_COMMENT`, `GH_BIN`.
