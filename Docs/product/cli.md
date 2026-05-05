# CLI Reference

RegProbe ships a command-line surface for scripted, automation-friendly workflows. The CLI uses the same research gating and SAFE execution model as the desktop app, so a tweak can still be visible yet blocked from mutation if the promotion state says it is not ready.

## Common Commands

```powershell
# List available tweaks
dotnet run --project cli/cli.csproj -- tweak list --risk safe

# Preview a tweak without applying it
dotnet run --project cli/cli.csproj -- tweak apply system.disable-game-recording-broadcasting

# Apply a tweak and keep verify + rollback-on-failure enabled
dotnet run --project cli/cli.csproj -- tweak apply system.disable-game-recording-broadcasting --apply

# Roll back a tweak
dotnet run --project cli/cli.csproj -- tweak revert system.disable-game-recording-broadcasting --apply

# Export the detected state bundle
dotnet run --project cli/cli.csproj -- config export --file regprobe-config.json

# Validate JSON tweak definitions
dotnet run --project cli/cli.csproj -- research validate-json-tweaks --input-dir app/Config/Tweaks

# Inspect one tweak or raw registry value name
dotnet run --project cli/cli.csproj -- research inspect SystemResponsiveness

# Check whether specific values are tracked for that setting
dotnet run --project cli/cli.csproj -- research inspect SystemResponsiveness --expected-value 10 --expected-value 30000

# Build the manual app-QA plan for one shipped card
dotnet run --project cli/cli.csproj -- research qa-plan SystemResponsiveness

# Emit the same app-QA plan as JSON
dotnet run --project cli/cli.csproj -- research qa-plan SystemResponsiveness --expected-value 10 --expected-value 30000 --json

# Run the full app-retest readiness check
dotnet run --project cli/cli.csproj -- research readiness

# Emit the readiness report as JSON
dotnet run --project cli/cli.csproj -- research readiness --json
```

## Main Command Groups

- `tweak`
  list, preview/apply, and rollback shipped tweaks
- `preset`
  list, preview/apply, and revert preset bundles
- `dns`
  inspect and set DNS provider profiles
- `config`
  export and import RegProbe configuration state
- `info`
  print lightweight machine and runtime context
- `research`
  inspect promotion gates, blocked worklists, single-setting truth, regression pack generation, and JSON tweak validation

## Single Setting Inspection

`research inspect <query>` is the fastest way to answer "what does RegProbe think this setting is?"

Use it with:

- a tweak id such as `power.optimize-cpu-boost`
- a record id such as `power.disable-network-power-saving.policy`
- a raw registry value name such as `SystemResponsiveness`
- a registry path fragment such as `Multimedia\\SystemProfile`

Optional flags:

- `--expected-value <value>`
  check whether a value appears in tracked targets, app writes, default/profile states, or proof text
- `--exact`
  require exact token matches instead of substring matches
- `--json`
  emit machine-readable output for scripts
- `--limit <n>`
  cap the number of matching records shown

The report ties together:

- research record id and tweak id
- promotion state
- rollback support
- app card presence
- tracked paths and value names
- app-written values
- evidence links
- nearby source hits

## Single Tweak App QA Plan

`research qa-plan <query>` turns one tweak, value name, or registry path query into a concrete desktop-app QA plan.

Use it when you want to confirm:

- the app card really exists
- the card points at the right research record
- rollback support and promotion state are what the repo says they are
- the desktop app can apply and optionally roll back that card through the hidden startup QA lane

The plan includes:

- the matched tweak id and card title
- the direct desktop-app command using `--qa-run-tweak`, `--qa-output`, `--qa-shutdown`, and the optional `--qa-skip-rollback` variant
- the guest VM helper command using `scripts/vm/guest-app-tweak-qa.ps1`
- the host-side KVM batch command using `scripts/vm-kvm/run-guest-app-tweak-qa-batch.py`
- the expected JSON report fields (`Success`, `Status`, `RollbackRequested`, and required stage names)
- the linked research doc, evidence locations, and expected-value summary for the chosen card

Useful overrides:

- `--app-exe <path>`
  replace the default `C:\Tools\AppSmoke\RegProbe.App.exe` path in the printed direct-launch command
- `--guest-output-dir <path>`
  change where the QA JSON report should be written inside Windows
- `--guest-user <name>`
  update the documented guest user in the plan output when your VM or lab user is not `rai`

Typical flow:

1. `research inspect <query>`
2. `research readiness`
3. `research qa-plan <query>`
4. Run the printed app command on Windows
5. Check the generated QA JSON before trusting the result

## Retest Readiness

`research readiness` is the fast preflight check to run before a manual desktop-app retest.

It ties together:

- public docs truth and contributor-doc drift
- tweak catalog wording truth
- app-surface card coverage and linked record docs
- evidence corpus, evidence-audit, and evidence-atlas count consistency
- rollback story coverage for apply-allowed records
- latest KVM app publish/deploy smoke and lane-health status

Use `--json` if you want the same result in a scriptable form.

## SAFE Notes

- `tweak apply` defaults to dry-run unless `--apply` is passed.
- verify stays on by default unless `--no-verify` is used.
- rollback-on-failure stays on by default unless `--no-rollback` is used.
- contributor/debug override flags only affect research gating; they do not bypass elevation or execution requirements.

## Release Use

If you only want the CLI from a release artifact, prefer the `RegProbe-Cli-<version>-win-x64.zip` package and verify it against the matching `RegProbe-<version>-win-x64-sha256.txt` file.
