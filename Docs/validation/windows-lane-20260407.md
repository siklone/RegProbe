# Windows validation lane - 2026-04-07

## Snapshot

- Canonical repo: `main` at `d2bf0016415ca8754b28c3a135744c8ee8d66396`
- Freeze tag: `validation-green-windows-1`
- Host baseline before validation:
  - `git status --short` clean
  - `git diff --check` clean
- Windows guest baseline:
  - OS: `Microsoft Windows [Version 10.0.26200.8037]`
  - PowerShell: `5.1.26100.1882`
  - .NET SDK: `8.0.416`
  - Git: not installed in guest `PATH`

## Validation approach

The Linux host intentionally stayed without `dotnet`. Missing .NET validation was executed inside the authorized Windows guest.

The guest did not have a native `git` checkout or `git.exe`, so validation used a clean archive exported from host `HEAD` and extracted inside the guest at `C:\r\rg`. This preserved the exact repo content of `d2bf0016` while avoiding guest-side drift.

## Commands and results

### Restore

Command:

```powershell
C:\Tools\DotNetSDK\8.0.416\dotnet.exe restore C:\r\rg\RegProbe.sln -v minimal
```

Result:

- exit code `0`
- restored `app`, `elevated-host`, `core`, `cli`, `plugins-devtools`, `infrastructure`, `engine`, `tests`

Evidence:

- `/tmp/regprobe-bridge/restore.code`
- `/tmp/regprobe-bridge/restore.txt`

### Targeted security tests

Command:

```powershell
C:\Tools\DotNetSDK\8.0.416\dotnet.exe test C:\r\rg\tests\tests.csproj --no-restore --filter FullyQualifiedName~ElevatedHostSessionSecurityTests -v minimal
```

Result:

- exit code `0`
- `Passed: 7, Failed: 0, Skipped: 0`

Evidence:

- `/tmp/regprobe-bridge/ehsec.code`
- `/tmp/regprobe-bridge/ehsec.txt`

Command:

```powershell
C:\Tools\DotNetSDK\8.0.416\dotnet.exe test C:\r\rg\tests\tests.csproj --no-restore --filter FullyQualifiedName~CommandAllowlistSecurityTests -v minimal
```

Result:

- exit code `0`
- `Passed: 4, Failed: 0, Skipped: 0`

Evidence:

- `/tmp/regprobe-bridge/cmdallow.code`
- `/tmp/regprobe-bridge/cmdallow.txt`

### Broader Windows test surface

Command:

```powershell
C:\Tools\DotNetSDK\8.0.416\dotnet.exe test C:\r\rg\tests\tests.csproj --no-restore -v minimal
```

Result:

- exit code `0`
- `Passed: 218, Failed: 0, Skipped: 0`

Evidence:

- `/tmp/regprobe-bridge/fulltest.code`
- `/tmp/regprobe-bridge/fulltest.txt`

Additional targeted proofs:

- `JsonTweakLoaderTests`: `Passed: 4`
- `ProcmonCsvRegistryNormalizerTests`: `Passed: 3`

Evidence:

- `/tmp/regprobe-bridge/jsonloader2.txt`
- `/tmp/regprobe-bridge/procmonnorm.txt`

## Security hardening proof

The masking hardening is now proven by both implementation review and executed Windows tests.

Implementation:

- `infrastructure/Elevation/ElevatedHostSessionSecurity.cs`

Executed coverage:

- quoted `--session-token`
- unquoted `--session-token`
- `sessionToken=`
- `token=`
- repeated token appearances
- `--pipe`
- `pipeName=`
- false-positive guards for token-like and pipe-like substrings

Windows result:

- `ElevatedHostSessionSecurityTests`: `7/7`
- `CommandAllowlistSecurityTests`: `4/4`

## CLI / loader proof

Guest-side synthetic invalid-definition batch was used to validate the new JSON tweak report path.

Command:

```powershell
dotnet run --project C:\r\rg\cli\cli.csproj -- research validate-json-tweaks --input-dir C:\r\jt --output C:\r\jt-report.json
```

Result:

- exit code `2`
- report status `invalid-definitions-present`
- `validation_issue_count = 1`
- issue code `documentation-required`

Evidence:

- `/tmp/regprobe-bridge/jtcli.code`
- `/tmp/regprobe-bridge/jtcli-report2.json`

## Imported evidence promotion gate proof

A synthetic `osquery` import was executed against the external-evidence importer to verify the machine-readable promotion gate fields.

Confirmed generated fields:

- `promotion_state = "blocked"`
- `promotion_blockers = ["documentation-first-review", "repo-native-proof"]`
- `record_promotion_allowed = false`
- `tweak_ingest_allowed = false`
- backlog `counts_by_promotion_state = {"blocked": 1}`

Evidence:

- `/tmp/regprobe-import-validate/imported-candidate-backlog.json`
- `/tmp/regprobe-import-validate/imported/external-osquery-win-validation/record-seeds/hklm-software-policies-microsoft-windows-explorer-hiderecommendedsection.json`
- `/tmp/regprobe-import-validate/evidence/external-osquery-win-validation/normalized-registry-bundle.json`

## Repo hygiene after validation

- canonical repo remained clean after the Windows run
- `git status --short` clean
- `git diff --check` clean

## Decision

This snapshot is a known-good Windows validation point for:

- elevated-host log and token masking hardening
- command allowlist hardening
- JSON tweak invalid-definition reporting
- normalized registry evidence plumbing already proven on host Python tests
- imported evidence promotion gate fields and backlog aggregation

Further research should branch from this checkpoint, not from the pre-validation state.
