# Research Blockers

This file tracks current blocker lanes only. The 2026-04-23 through
2026-04-27 QGA, ETW upload, tracerpt, and masked `ensure-admin-shell`
incidents were superseded by the 2026-05-07 QGA-first recovery sweep unless
they are listed again under Open Blockers.

## Current Snapshot

- last refreshed: 2026-05-07T15:07:47Z from `research/evidence-audit.json`
- active records: 301
- promotion states: 250 promoted, 51 blocked
- blocked layer counts: 33 validation-proof, 18 intentional-hold
- imported candidate backlog: 0
- VM transport backlog: 0 active ETW timeout, artifact-upload, or tracerpt
  blockers after the QGA recovery sweep
- re-audit queue: 23 records still require research review, mostly intentional
  holds and early-boot/system lanes

## VM Health Decision Tree

1. Run the non-mutating QGA health checker before ETW or guest Ghidra work:
   `python3 scripts/vm-kvm/vm-health-check.py --domain regprobe-win11-25h2-session --connect qemu:///session --json`
2. If the health checker returns `error_kind=qga-preflight-failed`, stop the
   evidence run. Repair QGA outside repo automation, then rerun the health
   checker.
3. If QGA is healthy, use QGA-first ETW or Ghidra launch paths. Do not let an
   implicit send-key fallback hide a QGA failure.
4. Use `--launch-transport send-key` only when intentionally testing the legacy
   path. Summaries must clearly report `launch_transport=send-key`.

## Open Blockers

### 2026-05-07T15:15:00Z - Validation-proof records are blocked from promotion

- status: open
- scope: 33 records in `research/evidence-audit.json` have
  `next_missing_layer=validation-proof`.
- records: `cleanup.component-store`, `cleanup.directx-shader-cache`,
  `cleanup.eventlog-system`, `cleanup.font-cache`, `cleanup.memory-dumps`,
  `cleanup.prefetch-files`, `cleanup.product-key`, `cleanup.recycle-bin`,
  `cleanup.shadow-copies`, `cleanup.temp-files`, `cleanup.thumbnail-cache`,
  `cleanup.wer-files`, `cleanup.windows-old`,
  `cleanup.windows-update-cache`, `misc.disable-edge-features`,
  `misc.disable-office-telemetry`, `misc.disable-onedrive`,
  `misc.disable-visual-studio-telemetry`,
  `misc.disable-vscode-telemetry`, `misc.optimize-7zip-settings`,
  `network.flush-dns-cache`, `network.reset-winsock`,
  `peripheral.audio-disable-ducking`,
  `peripheral.audio-disable-enhancements`,
  `peripheral.keyboard-disable-language-hotkey`,
  `peripheral.keyboard-optimize-repeat`,
  `peripheral.mouse-disable-acceleration`,
  `peripheral.mouse-disable-throttle`, `power.disable-cpu-parking`,
  `power.disable-hibernation`, `power.disable-superfetch`,
  `power.disable-usb-selective-suspend`, `security.disable-uac`.
- blocker: these are not QGA or ETW transport failures. Most are first-party
  cleanup, maintenance, peripheral, or utility cards that still need a stronger
  validation proof, app-surface promotion decision, or rollback story before
  they can be treated as promoted research cards.
- next action: handle them as product/research promotion work. Do not retry
  them as VM transport failures unless a specific record later receives a fresh
  evidence-run error.

### 2026-05-07T15:15:00Z - Intentional holds need dedicated research lanes

- status: open
- scope: 18 records in `research/evidence-audit.json` have
  `next_missing_layer=intentional-hold`.
- records: `policy.system.enable-virtualization`,
  `power.control.allow-audio-to-enable-execution-required-power-requests`,
  `power.control.allow-system-required-power-requests`,
  `power.control.hiber-file-size-percent`,
  `power.control.hibernate-enabled-default`,
  `power.control.power-request-override-subtree`,
  `power.control.power-watchdog-timeout-cluster`,
  `power.control.timer-rebase-threshold-on-drips-exit`,
  `power.control.ttm-enabled`,
  `power.control.win32k-callout-watchdog-timeout-seconds`,
  `power.session-watchdog-timeouts`,
  `power.session-win32-callout-watchdog-bugcheck-enabled`,
  `system.kernel-dpc-watchdog-control-cluster`,
  `system.kernel-dpc-watchdog-profile-cluster`,
  `system.kernel-long-dpc-threshold-cluster`,
  `system.kernel.force-bugcheck-for-dpc-watchdog`,
  `system.kernel.global-timer-resolution-requests`,
  `system.kernel.timer-check-flags`.
- blocker: these lanes need safer trigger conditions, boot/debug coverage,
  current-build pivots, or semantics proof. QGA is no longer the blocker.
- next action: schedule dedicated boot/power/kernel lanes, especially for DPC,
  timer, TTM, Win32 callout watchdog, and power watchdog clusters.

### 2026-04-23T04:49:51Z - Phase 3 WinDbg boot registry trace on KVM

- status: open
- scope: boot-time kernel/debug evidence.
- blocker: the repo still does not have a KVM WinDbg boot-trace wrapper that can
  capture the target boot registry reads safely and reproducibly.
- next action: keep this as a separate boot-debug tooling project. It should not
  block ordinary QGA-first ETW retries.

## Resolved Or Superseded Blockers

### [RESOLVED 2026-05-07] QGA guest-exec failures and masked send-key timeouts

- status: resolved
- affected historical batches: `batch-20260427-154500`,
  `batch-20260427-210500`, `batch-20260427-134500`,
  `batch-20260427-115400`, and follow-up single-record retries.
- resolution: repo tooling now has a non-mutating QGA health checker and
  QGA-first preflight contract. If QGA is unhealthy, runners fail fast with
  `summary_source=qga-preflight`,
  `error_kind=qga-preflight-failed`,
  `transport_blocker=qga-agent-command`, and
  `recovery_action=repair-qga-or-run-vm-health-check` instead of falling through
  to a misleading `ensure-admin-shell timeout`.
- evidence: QGA recovery receipts and refreshed scanners show no active ETW
  transport backlog.

### [RESOLVED 2026-05-07] ETW ingest, upload, and tracerpt timeout blockers

- status: resolved
- affected historical batches: `batch-20260424-192723`,
  `batch-20260424-194855`, `batch-20260424-191015`,
  `batch-20260424-182120`, `batch-20260424-171615`,
  `batch-20260424-154913`, and 2026-04-24 single-record ETW retries.
- resolution: QGA-first reruns with widened ETW timeouts produced ingested
  receipts. The active blocker scanner now reports zero remaining timeout,
  artifact-upload, or tracerpt statuses.
- caveat: old guest Ghidra launch failures are historical evidence-depth gaps,
  not active QGA transport blockers. Rerun Ghidra only when a record still needs
  static xref proof.

### [RESOLVED 2026-05-07] Windows Search service-control blocker

- status: resolved
- records: `power.disable-windows-search-service`,
  `system.services-disable-windows-search-service`.
- resolution: non-mutating QGA service inspection receipts replaced the old ETW
  artifact-upload timeout.
- receipts: `evidence/captures/power-disable-windows-search-service-qga-20260507.json`,
  `evidence/captures/system-services-disable-windows-search-service-qga-20260507.json`.

### [RESOLVED 2026-05-07] HKLM and policy ETW retry backlog

- status: resolved
- scope: HKLM and policy records previously blocked by 2026-04-24 or
  2026-04-27 ETW timeout receipts.
- resolution: QGA-unblocked ETW probe receipts were ingested and the affected
  records no longer depend on the stale timeout artifacts.
- receipts: `evidence/captures/*etw-qga-unblock-20260507.json`.

### [RESOLVED WITH CAVEAT 2026-05-07] HKCU tooling probe backlog

- status: resolved-with-caveat
- scope: HKCU records retried through QGA.
- resolution: QGA-unblocked tooling receipts establish machine/SYSTEM-scope
  observations and remove the stale timeout blocker.
- caveat: these receipts are not exact current-user HKCU reads. If a record
  needs per-user semantics, run a current-user lane instead of treating the
  SYSTEM-scope receipt as final user-scope proof.

### [RESOLVED 51aefbe8] 2026-04-23T06:53:00Z - Host build validation tool missing on default PATH

- status: resolved
- resolved_by: `51aefbe8`
- resolution: `scripts/test-local-build.sh` now discovers
  `dotnet-install.sh` output and common user-local SDK locations before failing.
