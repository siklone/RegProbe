# RegProbe Legacy Monitor Report Reconciliation - 2026-04-19

## Why This Note Exists

A pasted "RegProbe - Detaylı Analiz Raporu" described a much smaller registry-monitoring proof of concept:

- one small C# solution
- a `RegistryMonitor.cs` loop around `RegNotifyChangeKeyValue`
- no tests
- no CI
- no meaningful configuration or logging

That description does not match the current repository.

This note freezes the current-repo reality so future work does not inherit the wrong mental model.

## Bottom Line

The pasted report is mostly about an older or different `RegProbe` variant, not this repo.

The current repo is:

- a multi-project .NET 8 solution
- a WPF desktop app plus CLI plus elevated host
- an evidence-first research pipeline with VM/runtime/static lanes
- a repo with C# tests, Python tests, integration tests, CI, rollback state, and documented research contracts

The report still contains a few ideas that remain directionally useful, but its primary architecture and scorecard are not current-repo accurate.

## Current Repo Reality

### Solution shape

The repo is not a single `RegProbe/Program.cs` console prototype.

Current solution evidence:

- `RegProbe.sln`
- `app/app.csproj`
- `application/application.csproj`
- `core/core.csproj`
- `engine/engine.csproj`
- `infrastructure/infrastructure.csproj`
- `elevated-host/elevated-host.csproj`
- `cli/cli.csproj`
- `tests/tests.csproj`
- `tests.integration/tests.integration.csproj`

That is a ten-project solution, not a one-folder PoC.

### Product shape

The repo contract in `README.md` is explicit:

- "Evidence-first Windows registry research and safer configuration tooling."

The product surface is not "just monitor the registry". It is:

- detect
- preview
- apply
- verify
- rollback

with research/audit lanes behind the UI.

### Runtime lane shape

The pasted report centers on `RegNotifyChangeKeyValue`, `RegistryMonitor.cs`, and `RegistryChange.cs`.

Current repo evidence says otherwise:

- no `RegNotifyChangeKeyValue` hit exists in the repo
- no `RegistryMonitor.cs` file exists
- no `RegistryChange.cs` file exists

The repo does carry ETW/TraceEvent runtime research infrastructure instead:

- `infrastructure/RegistryResearch/TraceEventEtlRegistryNormalizer.cs`

That does not mean the repo is "an ETW-only monitor". It means the runtime proof model has moved far beyond the old Win32 notify-loop framing.

## Claims From The Pasted Report That Are Now Wrong

### 1. "Single small project"

Wrong for the current repo.

The repo now includes:

- WPF app
- CLI
- elevated host
- multiple core/engine/infrastructure layers
- dedicated test projects

### 2. "Unit test yok"

Wrong for the current repo.

Current local counts on 2026-04-19:

- `42` C# `*Tests.cs` files under `tests/`
- `33` Python `test_*.py` modules under `tests/python/`

There is also a separate integration-test project:

- `tests.integration/tests.integration.csproj`

### 3. "CI/CD yok"

Wrong for the current repo.

The repo has a GitHub Actions pipeline at:

- `.github/workflows/dotnet.yml`

That pipeline covers:

- build
- unit tests
- integration tests
- code quality
- coverage gates
- Python setup and hygiene-related validation

### 4. "Konfigürasyon desteği yok"

Wrong in substance for the current repo.

The repo does not use the exact `appsettings.json` shape proposed in the pasted report, but it does have persisted configuration infrastructure:

- `infrastructure/ISettingsStore.cs`
- `infrastructure/SettingsStore.cs`
- `infrastructure/AppPaths.cs`

So the correct statement is not "configuration is missing". The correct statement is that configuration is implemented through the repo's own persisted settings model.

### 5. "Logging mekanizması yok"

Wrong in substance for the current repo.

The repo has lightweight file-backed logging and tweak log persistence:

- `infrastructure/IAppLogger.cs`
- `infrastructure/FileAppLogger.cs`
- `infrastructure/FileTweakLogStore.cs`

This is not a Serilog/NLog stack, but it is definitely not "no logging".

### 6. "README minimal / lisans belirsiz"

Wrong for the current repo.

Current evidence:

- `README.md` is detailed and product-facing
- `Docs/product/cli.md` exists
- `Docs/product/media.md` exists
- `LICENSE` exists

### 7. "Monolithic monitoring PoC"

Wrong for the current repo.

There is clear layering across:

- app
- application
- core
- engine
- infrastructure
- elevated-host

That does not mean the architecture is finished or perfect, but it is no longer the single-class PoC described in the pasted report.

## Claims That Stay Partially Useful

### 1. Runtime proof matters more than folklore

This remains strongly aligned with the current repo.

The present repo already encodes that belief through:

- the evidence model in `README.md`
- VM workflow guidance
- runtime escalation guidance
- research audit outputs

### 2. Diff / before-after reasoning matters

This remains useful, but not in the exact "snapshot monitor class" shape proposed by the pasted report.

Current repo equivalents live closer to:

- tweak execution and rollback flow
- research evidence before/after artifacts
- explicit SAFE flow validation

### 3. ETW is often a better runtime lane than naive registry polling

Directionally true for the current repo and already reflected in practice:

- the runtime workflow prefers ETW first
- the repo carries TraceEvent ETL normalization

### 4. Structured logging could still be improved

This remains a fair residual observation.

The repo has logging, but it is lighter-weight and less structured than a full sink-based observability stack.

## Claims That Need Reframing, Not Repeating

### "Windows Service mode yok"

Technically true in the narrow sense that the repo does not expose a Worker Service / `UseWindowsService` host.

But that observation is easy to misuse.

The current product shape is not "missing the obvious next step of becoming a service". The repo already chose a different operational split:

- desktop app
- CLI
- separate elevated host

So "convert it into a service" is not an automatic next move.

### "Need to know exactly what value changed"

That is a valid requirement for a generic registry monitor.

But the current repo is not a generic registry event monitor. Its primary contract is evidence-backed setting research and safer application/rollback, not generic change surveillance.

## Real Residual Gaps That Still Matter In The Current Repo

The pasted report is mostly outdated, but a few higher-level concerns still translate into current work:

### 1. Some records remain intentionally blocked or revalidation-pending

Current audit evidence shows the repo is not "done"; it still carries blocked and stale evidence work:

- `research/evidence-audit.json`
- `registry-research-framework/audit/blocked-worklist.md`

### 2. Some exact runtime bindings remain unresolved

The current PowerRequestOverride lane is a concrete example:

- `research/records/power.control.power-request-override-subtree.json`

That record now has strong runtime/storage proof, but the exact current-build live reader binding is still unresolved.

### 3. Logging is present but not deeply structured

The repo has logging, but the observability story is still intentionally lightweight compared with a full structured logging stack.

### 4. Configuration is present but fragmented by product needs

The repo has persisted settings, but not a single uniform `appsettings.json` product story. That is a real design choice with tradeoffs, not an absence.

## Repo-Truth Checks Captured For This Reconciliation

Local checks run on 2026-04-19:

- `find tests -maxdepth 1 -name '*Tests.cs' | wc -l` -> `42`
- `find tests/python -maxdepth 1 -name 'test_*.py' | wc -l` -> `33`
- `rg -n 'RegNotifyChangeKeyValue' .` -> no hit
- `rg -n 'RegistryMonitor\\.cs|class RegistryMonitor\\b' .` -> no hit
- `rg -n 'RegistryChange\\.cs|class RegistryChange\\b' .` -> no hit

Those checks support the core conclusion that the pasted report is not describing the current codebase.

## Recommendation

Do not use the pasted scorecard as the current repo scorecard.

Use it only as:

- a reminder of what an older registry-monitor PoC looked like
- a source of a few still-useful heuristics like "runtime proof beats folklore"
- a prompt to distinguish real current gaps from legacy assumptions

For current-repo planning, the better sources are:

- `README.md`
- `CONTRIBUTING.md`
- `research/evidence-audit.json`
- `registry-research-framework/audit/blocked-worklist.md`
- the relevant record under `research/records/`

## Conclusion

The current repo is not a tiny registry notify loop with no tests and no CI.

It is an evidence-first Windows registry research and safer configuration toolchain with a desktop product surface, a CLI, an elevated host, a runtime/static VM workflow, and a real validation/audit system.

That means future analysis should critique the repo that exists now, not the PoC that the pasted report appears to describe.

## Retained audit artifact

- [regprobe-legacy-monitor-report-reconciliation-20260419.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/regprobe-legacy-monitor-report-reconciliation-20260419.json)
