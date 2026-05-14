# CLI Reference

RegProbe ships a command-line surface for scripted, automation-friendly workflows. The CLI uses the same research gating and SAFE execution model as the desktop app, so a tweak can still be visible yet blocked from mutation if the promotion state says it is not ready.

Host note: the .NET CLI targets the Windows desktop app stack. On Linux hosts that do not have `Microsoft.WindowsDesktop.App`, use the Python mirrors for `research inspect` and `research qa-plan`, or run the .NET CLI inside the Windows VM.

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
python3 registry-research-framework/scripts/check_single_tweak.py SystemResponsiveness

# Check whether specific values are tracked for that setting
python3 registry-research-framework/scripts/check_single_tweak.py SystemResponsiveness --expected-value 10 --expected-value 30000

# Build the manual app-QA plan for one shipped card
python3 registry-research-framework/scripts/check_single_tweak_app_qa.py SystemResponsiveness

# Emit the same app-QA plan as JSON
python3 registry-research-framework/scripts/check_single_tweak_app_qa.py SystemResponsiveness --expected-value 10 --expected-value 30000 --json

# Plan a promoted multi-card batch
python3 registry-research-framework/scripts/check_promoted_tweak_app_qa_batch.py --category Power --category Explorer --total-limit 4

# Run a live KVM promoted batch
python3 registry-research-framework/scripts/check_promoted_tweak_app_qa_batch.py --id power.disable-fast-startup --id power.disable-windows-search --id explorer.hide-empty-drives --id privacy.disable-find-my-device --run-kvm --json

# Run the full app-retest readiness check
python3 registry-research-framework/scripts/check_app_retest_readiness.py

# Emit the readiness report as JSON
python3 registry-research-framework/scripts/check_app_retest_readiness.py --json
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

Host-safe equivalent:

```bash
python3 registry-research-framework/scripts/check_single_tweak.py SystemResponsiveness --expected-value 10 --expected-value 30000 --json
```

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

Host-safe equivalent:

```bash
python3 registry-research-framework/scripts/check_single_tweak_app_qa.py SystemResponsiveness --expected-value 10 --expected-value 30000 --json
```

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

## Promoted App QA Batch

`research qa-batch` takes the same app-QA truth model and scales it to several shipped cards at once. The host-safe Python equivalent is `registry-research-framework/scripts/check_promoted_tweak_app_qa_batch.py`.

Use it when you want to:

- spot-check several promoted/apply-allowed cards before a manual app retest
- collect one host-driven KVM batch result instead of launching cards one by one
- keep one audit snapshot of which cards were selected and how the live run behaved

Useful flags:

- `--id <tweak-id>`
  pin the batch to explicit shipped tweak ids
- `--category <name>`
  auto-select from one or more categories
- `--limit-per-category <n>`
  cap auto-selection per category
- `--total-limit <n>`
  cap the overall batch size
- `--run-kvm`
  execute the selected batch through `scripts/vm-kvm/run-guest-app-tweak-qa-batch.py`
- `--wait-timeout <seconds>`
  override the live KVM wait timeout
- `--json`
  emit the batch report as JSON

The report includes:

- selected tweak ids, categories, and card names
- documentation files and rollback expectations
- per-card direct app and guest QA commands
- optional live KVM results
- the generated audit files:
  `registry-research-framework/audit/promoted-app-qa-batch-latest.json`
  and
  `registry-research-framework/audit/promoted-app-qa-batch-latest.md`

If you are running several live batches over time, use the cumulative artifacts alongside the latest snapshot:

- `registry-research-framework/audit/promoted-app-qa-batch-history.jsonl`
- `registry-research-framework/audit/promoted-app-qa-coverage-latest.json`
- `registry-research-framework/audit/promoted-app-qa-coverage-latest.md`

Current audit snapshot: as of 2026-05-08, promoted app-QA coverage is `258/258` (`100.0%`) with no uncovered promoted app-QA candidates remaining. Treat that number as a living release gate: refresh it after app-provider, card-mapping, promotion-gate, evidence, or rollback changes.

An `already-applied` live result is still a valid retest pass when the report verifies the desired value, keeps the card snapshot contract green, and skips standalone rollback only because no mutation was performed.

## Visual App Retest

The desktop app accepts startup navigation flags for manual/visual retest passes:

```powershell
& 'C:\Tools\AppSmoke\RegProbe.App.exe' --tweaks --open-tweak 'power.disable-network-power-saving.policy' --expand-plan
```

Use this after the JSON QA pass when you want to screenshot or manually inspect the visible card, proof lanes, claim-boundary copy, and plan/rollback drawer for the same tweak that was exercised by `research qa-plan`.

QA aliases are also accepted for scripts:

- `--qa-open-tweak <id>`
- `--qa-expand-plan`

`--qa-run-tweak` launches with an isolated single-instance key. That keeps a visual/manual retest window from stealing QA arguments when the live apply/verify/rollback harness starts a separate scheduled-task instance.

## Retest Readiness

`research readiness` is the fast preflight check to run before a manual desktop-app retest. The host-safe Python equivalent is `registry-research-framework/scripts/check_app_retest_readiness.py`.

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
