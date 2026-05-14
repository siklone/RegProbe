# Contributing to RegProbe

RegProbe is both a desktop tweak app and a registry research workspace. The most direct contributions fall into one of these buckets:

- finding and validating Windows keys and values
- strengthening evidence for an existing tweak
- adding or updating a shipped tweak/provider
- improving the v3.2 research pipeline, audit flow, or VM tooling

Audience boundary: end users should use the WPF app, not contributor tooling.
Contributors and agentic AI should treat Python scripts plus JSON artifacts as
the canonical research API. The .NET research CLI is a compatibility layer; keep
`tweak list/apply/revert` for advanced Windows/headless usage, but prefer the
Python mirrors for research inspection, app QA planning, readiness checks, and
VM campaign automation.

Windows contributors can also use the WPF Contributor Lab from a repo/dev build.
It is gated by an explicit acknowledgement and is intentionally conservative:
readiness checks, command packs, and research observations are visible there,
but direct mutation still requires certified VM/snapshot readiness and the
canonical Python/VM scripts.

## Core Rules

- runtime validation happens in the `Win25H2Clean` VM, not on the host
- host usage is for source edits, docs, generation scripts, and offline prep
- SAFE tweaks stay reversible: `Detect -> Apply -> Verify -> Rollback`
- do not casually rewrite historical evidence under `evidence/`, `research/`, or `Docs/`
- this repo uses a `main`-only remote workflow
- do not commit plaintext VM credentials
- do not treat `staged` manifests as proof
- do not touch [research/vm-incidents.json](research/vm-incidents.json) unless the task explicitly targets incident logging

## Evidence Contract

Wave 1 quality hardening is now the repo baseline.

- `full-evidence.json.artifact_refs` must be structured objects
- every physical artifact must carry:
  - `path`
  - `sha256`
  - `size`
  - `collected_utc`
- manifests may stay `staged`, but `staged` does not count as captured evidence
- if a runtime lane claims capture and the referenced ETL/PML/JSON artifact does not exist, the lane is treated as `missing-capture`
- kernel, boot, and driver records require a live mapped runtime lane with physical artifacts before they can finish green

When in doubt, prefer honest `missing-capture`, `staged-without-capture`, or `missing-required-runner` statuses over optimistic prose.

## Rejected Closure Decisions

Do not leave a rejected setting looking like an active evidence blocker. Pick the right closure lane and make it explicit:

- protected or ACL-owned surfaces should cite ACL/API evidence and use a protected-lane closure
- platform-limited VM behavior should cite the firmware or platform declaration and use an environment-limited closure
- one-shot cleanup, repair, or destructive actions should use a non-reversible closure unless a tested rollback lane exists
- deprecated records should stay rejected as `deprecated-record` and remain searchable for history

After changing any rejected decision, run:

```bash
python3 scripts/refresh_research_publish_surfaces.py
python3 registry-research-framework/scripts/generate_rejected_closure_ledger.py
```

Then inspect [rejected-closure-ledger.md](registry-research-framework/audit/rejected-closure-ledger.md). `promotion_blockers` should show the compact closure label, while `rejection_closure.superseded_blockers` preserves the old blocker context for auditors.

## Promotion-Eligible Final Decisions

When `promotion_state=promotion-eligible`, the evidence lane is no longer the blocker. Use the final decision pack to decide whether the record should become promoted, promoted-with-warnings, conditional, hold-closed, or evidence-backed rejected:

```bash
python3 registry-research-framework/scripts/generate_promotion_eligible_review_pack.py
```

Then inspect [promotion-eligible-review-pack.md](registry-research-framework/audit/promotion-eligible-review-pack.md). The pack is decision prep only; update the owning research record separately when you are ready to apply a verdict.

## Wave 2 Metadata

New research output should preserve and reuse these surfaces instead of inventing local ad-hoc notes:

- build freshness and revalidation metadata
- interaction graph and tweak dependency data
- anti-cheat / DRM advisory risk tags
- reproducibility baseline manifest
- negative-evidence publishing for archived and no-hit records

Relevant files:

- [interaction-graph.json](registry-research-framework/config/interaction-graph.json)
- [tweak-dependencies.json](registry-research-framework/config/tweak-dependencies.json)
- [anticheat-risk-overrides.json](registry-research-framework/config/anticheat-risk-overrides.json)
- [reproducibility-manifest.json](registry-research-framework/config/reproducibility-manifest.json)
- [regression-history.json](research/regression-history.json)
- [evidence-not-found](research/evidence-not-found)

## Pull Request Expectations

- if you change the SAFE flow, update the integration coverage for `Detect -> Apply -> Verify -> Rollback`
- if you change shipped UI language or layout, update the public media lane in [Docs/product/media.md](Docs/product/media.md)
- if you change CLI behavior, update [Docs/product/cli.md](Docs/product/cli.md)
- if you change release packaging or download guidance, update [Docs/product/support-matrix.md](Docs/product/support-matrix.md) and the root [README.md](README.md)
- if you change trust language, keep README, user guide, and app labels aligned

## Contribution Standards

- SAFE flow work is not done until the integration test story is still true
- UI renames or layout changes are not done until the media lane and README preview stay truthful
- CLI changes are not done until the command guide stays current
- release contract changes are not done until package names, checksums, and support docs stay aligned

Use conventional commit style when practical:

- `feat: add rollback point naming`
- `fix: correct GameDVR registry path`
- `docs: refresh CLI reference`
- `test: add SAFE flow smoke coverage`
- `refactor: extract application service layer`

## Collection Modes and Rollback

Research runners now accept `-CollectionMode evidence|operational`.

- `evidence`
  default for research, audits, and new runtime captures
- `operational`
  reserved for flows that intentionally keep automatic rollback

In `evidence` mode:

- automatic rollback should not fire
- pre-change and post-change exports are expected
- manifests must mark `rollback_pending = true` until an explicit rollback record exists

If you later roll back an evidence run, preserve the adli zincir:

- export before rollback
- export after rollback
- keep a diff record

Do not silently revert and pretend nothing changed.

## VM Credentials

All repo-tracked VM scripts should resolve credentials through the shared helper under [scripts/vm/_vmrun-common.ps1](scripts/vm/_vmrun-common.ps1).

Resolution order:

1. explicit credential input
2. environment variables
3. DPAPI-protected CLIXML credential file outside the repo

Do not reintroduce hard-coded guest passwords in PowerShell scripts, notes, or example commands.

## VM Setup

RegProbe research scripts support multiple VM backends.
Set these environment variables before running any script:

| Variable | Default | Description |
|---|---|---|
| `REGPROBE_VM_BACKEND` | `kvm` | `kvm` / `vmware` / `hyperv` / `virtualbox` |
| `REGPROBE_VM_DOMAIN` | `regprobe-win11` | VM name in your hypervisor |
| `REGPROBE_VM_USER` | `Administrator` | Guest Windows username |
| `REGPROBE_VM_SNAPSHOT` | `RegProbe-Baseline` | Snapshot to restore from |
| `REGPROBE_VM_UPLOAD_DIR` | `/tmp/regprobe-bridge` | Host-side file exchange dir |
| `REGPROBE_BRIDGE_URL` | `http://10.0.2.2:8766` | Guest-to-host bridge URL |

### KVM/QEMU (Linux)

```bash
export REGPROBE_VM_BACKEND=kvm
export REGPROBE_VM_DOMAIN=your-vm-name
```

Before running ETW, WPR, or Ghidra KVM lanes, verify QGA without mutating the guest:

```bash
python3 scripts/vm-kvm/vm-health-check.py --domain regprobe-win11-25h2-session --connect qemu:///session --json
```

Recovery decision tree:

- `domstate` is not `running`: start, restore, or replace the VM before collecting evidence.
- `guest_ping`, `guest_info`, or `guest_exec` fails: repair QGA in the guest and rerun the health check; do not continue from a masked `ensure-admin-shell` timeout.
- Health is `ok`: use the default QGA-first runner path, `--launch-transport auto --preflight require`.
- You intentionally need keyboard injection: pass `--launch-transport send-key`; this bypasses QGA preflight and the summary must record `launch_transport=send-key`.

Blocked evidence recovery loop:

1. Confirm QGA health with `vm-health-check.py`.
2. Check guest disk space before ETW; stale `C:\RegProbe-Diag\etw-stackwalk\*202604*` diagnostics can consume tens of GB.
3. Prefer a narrow QGA-first retry over committing stale giant XML: use `run-guest-etw-stackwalk-capture.py --stackwalk-event RegQueryValue --buffer-size-kb 256 --min-buffers 16 --max-buffers 64 --ingest-to-repo`.
4. Treat `launch_transport=qga`, `etl_exists=true`, `xml_exists=true`, and normalized bundle `status=ok` as the unblock contract.
5. Record what the bundle actually proves: exact target `RegQueryValue` is stronger than a helper command line; absent-key/open evidence can support transport and baseline state but should not be overstated.
6. Add an `evidence/captures/...json` receipt, update the owning record, and run `python3 scripts/refresh_research_publish_surfaces.py`.

### VMware Workstation

```bash
export REGPROBE_VM_BACKEND=vmware
export REGPROBE_VM_PATH="/path/to/your.vmx"
```

### Hyper-V (Windows)

```powershell
$env:REGPROBE_VM_BACKEND="hyperv"
$env:REGPROBE_VM_NAME="Your-VM-Name"
```

### Oracle VirtualBox

```bash
export REGPROBE_VM_BACKEND=virtualbox
export REGPROBE_VM_DOMAIN="Your-VM-Name"
```

## Start Here

Read these first:

- [README.md](README.md)
- [research/README.md](research/README.md)
- [research/evidence-atlas.md](research/evidence-atlas.md)
- [research/evidence-audit.json](research/evidence-audit.json)
- [Docs/research/tooling-map.md](Docs/research/tooling-map.md)
- [Docs/research/run-tiers.md](Docs/research/run-tiers.md)
- [Docs/research/vm-workflow.md](Docs/research/vm-workflow.md)
- [Docs/research/runtime-escalation.md](Docs/research/runtime-escalation.md)
- [Docs/research/script-catalog.md](Docs/research/script-catalog.md)
- [Docs/TWEAK_SOURCES.md](Docs/TWEAK_SOURCES.md)
- [Docs/SERVICES_DOCUMENTATION.md](Docs/SERVICES_DOCUMENTATION.md)

## If You Are Starting Cold

If you do not know the repo yet, use this order:

1. Build and test the repo once so you know the baseline is green.
2. Read the [research tooling map](Docs/research/tooling-map.md) and
   [run tiers](Docs/research/run-tiers.md) so you know which scripts are
   canonical and which run artifacts are reference-eligible.
3. If you are on Windows, open Contributor Lab from the app to check local
   readiness and copy the safe command pack for the task.
4. Run a single-setting inspection before editing anything. On Linux hosts
   without `Microsoft.WindowsDesktop.App`, use the Python mirror; use the .NET
   CLI in the Windows VM or on a desktop-runtime host only when you explicitly
   need CLI compatibility.

```bash
python3 registry-research-framework/scripts/check_single_tweak.py SystemResponsiveness
python3 registry-research-framework/scripts/check_single_tweak.py SystemResponsiveness --expected-value 10 --expected-value 30000
```

4. Run the app-retest readiness check if you are about to verify cards, evidence, rollback, or KVM smoke:

```bash
python3 registry-research-framework/scripts/check_app_retest_readiness.py
python3 registry-research-framework/scripts/check_app_retest_readiness.py --json
```

5. Generate the single-card app QA plan before touching the desktop app:

```bash
python3 registry-research-framework/scripts/check_single_tweak_app_qa.py SystemResponsiveness
python3 registry-research-framework/scripts/check_single_tweak_app_qa.py SystemResponsiveness --expected-value 10 --expected-value 30000 --json
```

The equivalent .NET CLI commands are:

```powershell
dotnet run --project cli/cli.csproj -- research inspect SystemResponsiveness --expected-value 10 --expected-value 30000
dotnet run --project cli/cli.csproj -- research qa-plan SystemResponsiveness --expected-value 10 --expected-value 30000 --json
```

6. If you are about to retest several shipped cards, plan or run a promoted batch:

```bash
python3 registry-research-framework/scripts/check_promoted_tweak_app_qa_batch.py --category Power --category Explorer --total-limit 4
python3 registry-research-framework/scripts/check_promoted_tweak_app_qa_batch.py --id power.disable-fast-startup --id power.disable-windows-search --id explorer.hide-empty-drives --id privacy.disable-find-my-device --run-kvm --json
```

6. Open the reported research record, app card doc, and source file together.
7. Only then change provider code, research records, evidence, or docs.

That flow is the fastest way to answer beginner questions such as:

- does this key already exist in the repo?
- which tweak id owns it?
- which values does the app write?
- does the record allow apply or keep the card blocked?
- is the value only in docs, or does it also exist in code and evidence?

If you are validating the desktop app itself, `research qa-plan` is the next hop after `research inspect` and `research readiness`. It prints the direct app startup-QA command, the guest VM helper command, the expected JSON report contract, and the rollback/card/evidence checks to confirm while the app is open. If you need wider coverage before a release-style retest, `research qa-batch` turns the same truth model into a promoted multi-card batch and can drive the KVM guest lane directly.

When you run several live promoted batches, treat `registry-research-framework/audit/promoted-app-qa-batch-latest.json` as the newest snapshot only. The cumulative view lives in `registry-research-framework/audit/promoted-app-qa-batch-history.jsonl` plus the paired coverage files `promoted-app-qa-coverage-latest.json` and `promoted-app-qa-coverage-latest.md`.

Current audit snapshot: as of 2026-05-08, app retest readiness is passing with `265` app-surface entries, `0` app-only backlog items, `261` apply-allowed records, and `0` missing rollback stories. Promoted app-QA coverage is `258/258` (`100.0%`) with no uncovered promoted app-QA candidates remaining. If you change shipped providers, card mapping, promotion gates, evidence links, or rollback behavior, refresh this readiness and the promoted app-QA batch coverage before handing the repo back.

For a single-card retest, keep a JSON artifact when the result will be needed later:

```bash
python3 registry-research-framework/scripts/check_single_tweak_app_qa.py SystemResponsiveness --expected-value 10 --expected-value 30000 --json > registry-research-framework/audit/single-tweak-check-systemresponsiveness-latest.json
```

An `already-applied` live result is acceptable only when the report still verifies the desired value, preserves the card/evidence drawer contract, and skips standalone rollback because no mutation happened.

For raw registry batches, first turn the paste into a one-value experiment plan:

```bash
python3 scripts/registry/parse_reg_add_batch.py \
  --input pasted-reg-adds.txt \
  --json-output registry-research-framework/audit/registry-value-experiments/operator-batch.json \
  --markdown-output registry-research-framework/audit/registry-value-experiments/operator-batch.md
```

Do not apply the whole batch at once. Missing or opaque values need ETW, Procmon, or static-string follow-up before being closed. Present boot-sensitive values need snapshot/overlay-gated apply, basic Windows smoke, reboot health, rollback, reboot, and final smoke. Use `scripts/vm-kvm/run-guest-registry-value-experiment.py --require-domain-snapshot` for libvirt-domain runs; the `pilot-perf-calculate-actual-utilization-0` artifact is the current cautionary example.

## Evidence Card Quality Bar

Do not treat a card as complete just because it names a key and a value. A reviewer should be able to answer three questions without reading your mind:

1. What do we know? Name the exact key, value, app write, promotion state, rollback story, and evidence layer that backs the card.
2. What do we not claim? Say when the record does not prove runtime behavior, benchmark impact, ETW/WPR activity, or full undocumented semantics.
3. What happened to ambiguous values? Split or archive them instead of hiding them inside a broad app-ready bundle.

Use `power.disable-network-power-saving.policy` as the current pattern. The shipped app card keeps `DisableTaskOffload = 0` and `SystemResponsiveness = 10`, but the older `power.disable-network-power-saving` parent is intentionally not mapped because its `NetworkThrottlingIndex` write is still opaque. That parent remains in research as audit history, not as a normal apply-ready tweak.

## Where To Find Keys and Values

There is no single source of truth. Use several surfaces together.

### 1. Shipped tweak definitions

Check the live app mapping first:

- `app/Services/TweakProviders/`
- `engine/Tweaks/`
- `engine/Tweaks/Commands/`

This tells you whether a key is already shipped, what category it belongs to, and which tweak abstraction is already in use.

### 2. Existing research records

Before researching from scratch, check:

- `research/records/`
- `research/notes/`
- `research/evidence-atlas.md`
- `research/evidence-manifest.md`
- `research/evidence-audit.json`

If a record already exists, extend it instead of creating a duplicate tweak id.

### 3. Existing evidence bundles

If you need to see how something was previously proven, use:

- `evidence/records/<tweak-id>/`
- `evidence/raw/procmon/`
- `evidence/raw/ghidra/`
- `evidence/files/vm-tooling-staging/`

These usually show the trigger path, target binary, runtime summary, and static export that moved the tweak to its current class.

### 4. Official source surfaces

Use official Microsoft surfaces whenever possible:

- Learn articles
- ADMX/ADML policy definitions
- Policy CSP pages
- KB articles
- service/driver registry documentation

If official documentation gives the exact path and value semantics, that can be enough for `Class A` when the app mapping and restore path are exact.

### 5. Lineage and imported sources

Community or imported research can help you discover a key, but it is not final proof by itself. Cross-check lineage against runtime and static evidence before you trust it.

## Tool Selection

RegProbe's v3.2 pipeline is cross-layer. For undocumented keys, one tool is not enough.

### Runtime tools

- `ETW`
  primary runtime trace lane
- `safe mega-trigger runtime v2`
  family-specific trigger escalation after plain ETW no-hit
- `Procmon`
  supporting runtime lane, especially for visible user-mode reads
- `WPR/WPA`
  behavior and boot/system tracing
- `typeperf`
  lightweight before/after behavior measurement
- `xperf`
  deeper stack or boot analysis when needed
- `DTrace`
  optional strengthening lane where supported
- `WinDbg`
  last-resort boot and dead-flag arbiter for keys ETW still misses

For `kernel`, `boot`, or `driver` suspected layers, at least one real runtime runner must complete with physical capture artifacts. A resolver-only or staged manifest is not enough.

### Static tools

- strings/bin-grep style scanning
- `FLOSS`
  for decoded or stack-built strings
- `capa`
  for registry-read capability and semantic clues
- `Ghidra + PDB`
  for string xrefs, branch logic, and decompilation
- source-enrichment scan
  `ReactOS`, `WRK`, `System Informer`, `Sandboxie`, `Wine`, `ADMX`, and `WDK` cross-reference

`IDA` is optional today. Do not block a record on IDA automation unless a working headless-capable build is actually available.

### Frida Kernel Guard

Apply Frida guard rules every time:

- do not use Frida for kernel keys
- do not use Frida for boot-phase keys
- do not use Frida for driver parameter keys
- do not use Frida for `HKLM\\SYSTEM` or `SYSTEM`-only paths

Use:

- `registry-research-framework/routing/frida-kernel-guard.ps1`
- `registry-research-framework/routing/key-type-router.ps1`
- `registry-research-framework/routing/tool-selector.ps1`

## Scripts You Will Actually Use

### Full pipeline entry

- `registry-research-framework/pipeline/v31_pipeline.py`

Use this when you want the orchestrated v3.2 flow instead of running each phase by hand.

### Individual v3.2 phases

- `registry-research-framework/pipeline/faz0-enrichment.ps1`
- `registry-research-framework/pipeline/faz1-runtime-trace.ps1`
- `registry-research-framework/pipeline/faz2-static-analysis.ps1`
- `registry-research-framework/pipeline/faz3-behavior-measure.ps1`
- `registry-research-framework/pipeline/faz4-dead-flag-check.ps1`
- `registry-research-framework/pipeline/faz5-classify.ps1`
- `registry-research-framework/pipeline/faz6-output.ps1`
- `registry-research-framework/pipeline/faz-retroactive-audit.ps1`

### Audit helpers

- `registry-research-framework/audit/re-audit-scanner.ps1`
- `registry-research-framework/audit/re-audit-queue.csv`
- `registry-research-framework/audit/re-audit-report.md`

### Runtime and static wrappers

- `registry-research-framework/tools/etw-registry-trace.ps1`
- `registry-research-framework/tools/run-power-control-batch-mega-trigger-runtime.ps1`
- `registry-research-framework/tools/run-windbg-boot-registry-trace.ps1`
- `registry-research-framework/tools/procmon-registry-trace.ps1`
- `registry-research-framework/tools/ghidra-headless-analyze.ps1`
- `registry-research-framework/tools/pdb-download.ps1`
- `registry-research-framework/tools/run-source-enrichment-scan.ps1`
- `registry-research-framework/tools/capa-scan.ps1`
- `registry-research-framework/tools/floss-scan.ps1`
- `registry-research-framework/tools/bingrep-scan.ps1`
- `registry-research-framework/tools/typeperf-baseline.ps1`
- `registry-research-framework/tools/wpr-boot-trace.ps1`
- `registry-research-framework/tools/registry-sideeffect-diff.ps1`

### VM orchestration

- `scripts/vm/ensure-shell-stable-snapshot.ps1`
- `scripts/vm/get-vm-shell-health.ps1`
- `scripts/vm/log-vm-incident.ps1`
- `scripts/vm/configure-kernel-debug-baseline.ps1`
- `scripts/vm/new-windbg-registry-watch-script.ps1`
- `scripts/vm/app-launch-smoke.ps1`
- `scripts/vm/host-validation-controller.ps1`
- `scripts/vm/run-validation-with-restart-watch.ps1`

### Tweak-specific VM probes

Look in `scripts/vm/` for targeted runners such as:

- `run-cpu-idle-states-runtime-probe.ps1`
- `run-cpu-idle-states-benchmark.ps1`
- `run-explorer-shell-registry-runtime-probe.ps1`
- `run-explorer-compact-mode-runtime-probe.ps1`
- `run-jpeg-import-quality-runtime-probe.ps1`
- `run-reliability-timestamp-probe.ps1`
- `run-defender-threat-file-hash-probe.ps1`
- `run-ghidra-string-xref-probe.ps1`

If a matching runner already exists, use it before inventing a new one.

### Published output generators

After updating records or evidence, regenerate the published layer with:

- `scripts/generate_evidence_classes.py`
- `scripts/generate_evidence_index.py`
- `scripts/generate_evidence_manifest.py`
- `scripts/generate_evidence_atlas.py`
- `scripts/generate_evidence_audit.py`

## Standard Workflow For a New or Updated Key

### 1. Find the candidate

Start from one of these:

- official documentation
- an existing backlog item
- a VM/runtime observation
- an imported lineage source that still needs proof

### 2. Check whether it already exists

Search:

- `research/records/`
- `app/Services/TweakProviders/`
- `engine/Tweaks/`
- `research/evidence-atlas.md`

If it already exists, extend the current record instead of creating a duplicate.

### 3. Classify the key type

Decide whether it is:

- user-mode
- kernel
- boot-phase
- driver/service parameter

This decides whether Frida is allowed and whether boot ETW or WPR is required.

### 4. Collect runtime proof in the VM

Use `Win25H2Clean` for all live probing:

- ETW first
- if ETW stays weak or idle-only, move to the family-safe mega-trigger lane
- Procmon as supporting evidence
- WPR/WPA and `typeperf` when behavior matters
- WinDbg only after the no-hit queue survives the cheaper runtime lanes
- snapshot before risky runs
- log incidents if shell, input, desktop, or graphics break

### 5. Collect static proof

Use a layered approach:

- strings/bin-grep/FLOSS to narrow the candidate binary
- `capa` to confirm registry-read capability
- source-enrichment scan to find external references and raise or lower runtime priority
- Ghidra + PDB to map strings to functions and branch behavior

If Ghidra logs a MATCH address or `<no function>`, follow it through the fallback lane. A log hit is the start of analysis, not the end.

Nohuto guardrails are mandatory:

- no committed `FUN_` / `DAT_` artifacts
- no long decompile walls
- bounded branch output only
- if the branch meaning is not established, write `unclear`
- external source references help, but they do not replace branch-backed Windows evidence

### 6. Write the result back into the repo

Update the right places:

- `evidence/records/<tweak-id>/`
- `research/records/<tweak-id>*.json`
- `research/notes/`
- `evidence/files/...` when a normalized artifact belongs in git

Keep absolute local paths out of published outputs.

### 7. Regenerate published outputs

Run the evidence generators so atlas, manifest, audit, and class summaries stay in sync.

### 8. Update or add the live tweak mapping

If the key should ship in the app, add or update it in the appropriate provider under `app/Services/TweakProviders/` and use existing engine abstractions where possible.

## Implementation Guidance

### Tweak ids

- format: `category.descriptive-name`
- examples:
  - `privacy.disable-telemetry`
  - `network.optimize-smb`
  - `system.disable-jpeg-reduction`

### Risk levels

- `Safe`
  reversible and broadly suitable
- `Advanced`
  meaningful side effects or environmental assumptions
- `Risky`
  high-blast-radius or specialist-only behavior

### Registry implementation rules

- choose the correct `RegistryHive`
- choose the correct `RegistryValueKind`
- keep `CurrentUser` tweaks non-elevated where possible
- support detect/apply/verify/rollback
- prefer existing tweak types before creating a new abstraction

## Validation Checklist

For code changes:

```powershell
dotnet build RegProbe.sln -c Release
dotnet test tests/tests.csproj -c Release --no-build -v minimal
```

If the host can build but cannot execute `net8.0-windows` tests because the WindowsDesktop runtime is missing, use the KVM guest test lane:

```bash
dotnet build tests/tests.csproj -c Release -p:EnableWindowsTargeting=true
python3 scripts/vm-kvm/run-guest-dotnet-tests.py --wait-timeout 1800
```

The guest runner preserves the repo layout expected by C# tests by staging `Docs`, `research/records`, `research/promotion-gates.json`, and `tests/bin/Release/net8.0-windows` under `C:\RegProbe`.

For research changes:

- VM lane executed in `Win25H2Clean`
- evidence bundles updated
- published outputs regenerated
- no local drive paths leaked into atlas, manifest, or audit

For risky runtime work:

- shell snapshot taken first
- incident logged if the VM breaks
- app launch smoke rerun after recovery

## Commit Style

Use short conventional subjects:

- `research: add reliability runtime lane`
- `fix: correct tweak rollback semantics`
- `docs: refresh contributing workflow`
- `refactor: simplify provider wiring`
- `test: add coverage for conditional tweak routing`

## Final Notes

- prefer extending existing records over creating parallel ones
- prefer normalized repo-tracked artifacts over random temp dumps
- do not treat lineage as final proof
- do not run live validation on the host
- when in doubt, make the evidence stronger before making the tweak easier
