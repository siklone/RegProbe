# Supply Chain Audit - 2026-05-22

Scope: quick post-incident check after the May 2026 GitHub internal repository
incident and the ongoing npm ecosystem malware risk.

## Current Finding

- App dependency surface is .NET/NuGet only. No `package.json`, npm lockfile, or
  `.npmrc` is present in the repository.
- `dotnet list RegProbe.sln package --vulnerable --include-transitive` reports
  no vulnerable packages for app, CLI, elevated host, infrastructure, tests, or
  plugin projects.
- Dependabot alerts API returned no open alerts for this repository.
- High-signal local token pattern scan found no GitHub PAT, npm token, OpenAI
  key, AWS access key, Slack token, or private-key block in tracked source files.
- Main branch CI was green before this audit (`CI/CD` and dependency submission).

## Hardening Applied

- Added `NuGet.Config` with a single explicit package source: `nuget.org`.
- Added NuGet package source mapping so all package IDs resolve only from
  `nuget.org`.
- Enabled committed NuGet lockfiles through `RestorePackagesWithLockFile`.
- Generated `packages.lock.json` for each project.
- Changed CI restore commands to `dotnet restore RegProbe.sln -r win-x64
  --locked-mode`.
- Changed CI publish steps to use `--no-restore` after the locked restore.

## Residual Notes

- Test projects still use `xunit` 2.9.3, which NuGet marks as `Legacy` with
  `xunit.v3` as the long-term alternative. This is not a shipped runtime app
  dependency, but it is a cleanup candidate.
- GitHub Actions are still referenced by major tags such as `actions/checkout@v6`.
  The workflow already uses least-privilege default permissions and
  `persist-credentials: false`; pinning actions to immutable commit SHAs is a
  reasonable next hardening step if we want a stricter supply-chain policy.
- Contributor VM bootstrap scripts intentionally download external research
  tooling such as Sysinternals, .NET installer scripts, DiskSpd, Java, Ghidra,
  and Windows SDK components. Those scripts are contributor/research lanes, not
  normal end-user app startup or CI package restore.

## Verification Commands

```bash
./dotnetw list RegProbe.sln package --vulnerable --include-transitive
./dotnetw list RegProbe.sln package --deprecated
./dotnetw restore RegProbe.sln -r win-x64 --locked-mode
./dotnetw build RegProbe.sln -c Release --no-restore -p:EnableWindowsTargeting=true
python3 -m unittest discover -s tests/python -q
git diff --check
```

## References

- GitHub Blog, "Investigating unauthorized access to GitHub's internal repositories", 2026-05-20.
- GitHub Changelog, "Dependabot now detects malware in npm dependencies", 2026-03-17.
- GitHub Changelog, "npm bulk trusted publishing config and script security now generally available", 2026-02-18.
