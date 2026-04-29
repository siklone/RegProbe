# Script Catalog

This file is the maintained inventory for every tracked `.ps1` and `.py` file under `scripts/`.
Entries are grouped by directory so new helpers are harder to lose and removed files stop lingering as stale catalog rows.

## Root Scripts

Repo-level publishing, evidence, metrics, and maintenance utilities.

- `scripts/artifact_metadata_lib.py`
  Shared helper library for artifact path, size, sha256, and collected_utc metadata.
- `scripts/audit_execution_required_runtime_retries.py`
  Audits execution required runtime retries.
- `scripts/audit_registry_sideeffect_regressions.py`
  Audits registry sideeffect regressions.
- `scripts/audit_static_evidence_v32.py`
  Audits static evidence v32.
- `scripts/audit_tweak_sources.py`
  Audits tweak sources.
- `scripts/backfill_deprecated_validation_proof.py`
  Backfills deprecated validation proof.
- `scripts/behavior_stats_lib.py`
  Shared helper library for behavior and evidence scoring statistics.
- `scripts/build_brand_assets.ps1`
  Rebuilds the checked-in logo PNG and ICO assets from the brand sources.
- `scripts/check_release_assets.py`
  Verifies packaged release assets before publication.
- `scripts/clean_build_outputs.ps1`
  Cleans build outputs.
- `scripts/compact_ghidra_branch_output.py`
  Compacts raw Ghidra branch-analysis output into smaller review artifacts.
- `scripts/compare_static_cross_verification.py`
  Compares static verification results across evidence sources.
- `scripts/evidence_class_lib.py`
  Shared helper library for evidence-class scoring and normalization.
- `scripts/external_evidence_import_lib.py`
  Shared helper library for importing externally collected evidence into repo format.
- `scripts/find_dynamic_resolution_patterns.py`
  Searches evidence and notes for dynamic-resolution patterns worth reclassification.
- `scripts/generate_docs_first_recovery_batches.py`
  Generates recovery batches for docs-first records that still need runtime follow-up.
- `scripts/generate_evidence_atlas.py`
  Builds the evidence atlas index for published research surfaces.
- `scripts/generate_evidence_audit.py`
  Generates cross-record evidence audit summaries.
- `scripts/generate_evidence_classes.py`
  Rebuilds normalized evidence-class outputs from research records.
- `scripts/generate_evidence_index.py`
  Rebuilds the evidence index used by published research surfaces.
- `scripts/generate_evidence_manifest.py`
  Rebuilds the evidence manifest for tracked artifacts and records.
- `scripts/generate_imported_candidate_backlog.py`
  Generates a backlog of imported candidate records that still need promotion work.
- `scripts/generate_negative_evidence.py`
  Generates the negative-evidence index from no-hit research findings.
- `scripts/generate_promotion_gates.py`
  Rebuilds `research/promotion-gates.json` from current validated records and audit state.
- `scripts/generate_product_preview_assets.py`
  Builds product preview images and derived launch assets.
- `scripts/research/generate_app_surface_manifest.py`
  Rebuilds the research app-surface manifest from validated, surfaceable research records.
- `scripts/generate_regression_history.py`
  Rebuilds regression-history summaries from validation metadata.
- `scripts/generate_review_required_backlog.py`
  Generates a backlog of records that still need manual review.
- `scripts/generate_tweak_catalog.py`
  Rebuilds the tweak catalog markdown, CSV, and HTML publish surfaces.
- `scripts/generate_tweak_details.py`
  Rebuilds the per-tweak detail publish surfaces.
- `scripts/generate_tweak_provenance.py`
  Rebuilds tweak provenance outputs from app metadata and research records.
- `scripts/imported_candidate_backlog_lib.py`
  Shared helper library for imported-candidate backlog generation.
- `scripts/metrics_publish_v36_lib.py`
  Shared helper library for v3.6 metrics publishing.
- `scripts/normalize_evidence_layout.py`
  Normalizes evidence file layout into the repo publish structure.
- `scripts/normalize_json_evidence_refs.py`
  Normalizes JSON evidence references to repo-relative paths and metadata.
- `scripts/normalize_published_evidence_roots.py`
  Normalizes published evidence roots after path or bundle moves.
- `scripts/package_release_assets.ps1`
  Packages the release asset bundle used for tagged publishes.
- `scripts/package_windows.ps1`
  Packages the Windows desktop build for release review or distribution.
- `scripts/publish_release.ps1`
  Builds the deterministic publish folder used for release smoke checks.
- `scripts/refresh_research_publish_surfaces.py`
  Refreshes the generated research publish surfaces in one pass.
- `scripts/report_tweak_docs.py`
  Reports tweak-to-doc coverage and documentation drift.
- `scripts/research_path_lib.py`
  Shared helper library for canonical research, evidence, and publish paths.
- `scripts/research_v36_lib.py`
  Shared helper library for the v3.6 research pipeline.
- `scripts/runtime_evidence_v36_lib.py`
  Shared helper library for v3.6 runtime evidence processing.
- `scripts/sanitize_public_artifacts.ps1`
  Scrubs machine-specific paths and usernames from tracked public artifacts.
- `scripts/source_enrichment_scan.py`
  Scans research records for ReactOS, WRK, and source-enrichment gaps.
- `scripts/update_readme_progress.py`
  Updates README progress counters from the current research state.
- `scripts/validate-in-vm.ps1`
  Clones the configured repo/branch inside the VM and runs the packaged validation flow with env-driven repo, branch, workdir, and dotnet command overrides.
- `scripts/wave2_research_lib.py`
  Shared helper library for wave-2 research automation.

## Card Pipelines

Generated card surfaces and card-build helpers that sit next to, but outside,
the main `scripts/` tree.

- `cards/generate_cards.py`
  Calls the Anthropic Messages API to convert `research/records/*.json` into
  `cards/v25H2/*.card.json` outputs and skips unchanged records via a local
  hash-state file.

## Hyper-V VM Scripts

Hyper-V-specific planning and feasibility helpers.

- `scripts/vm-hyperv/new-hyperv-debug-baseline-plan.ps1`
  Emits the planned Hyper-V debugger baseline contract and provisioning steps.
- `scripts/vm-hyperv/test-hyperv-debug-feasibility.ps1`
  Checks whether the current host can support the Hyper-V debug environment.

## KVM VM Scripts

KVM/QGA runners, bridge helpers, and host-side orchestration for the research lane.

- `scripts/vm-kvm/bootstrap-research-lane.ps1`
  Bootstraps the KVM research lane prerequisites and working folders.
- `scripts/vm-kvm/command_json_lib.py`
  Shared helper library for guest-bridge command payload JSON.
- `scripts/vm-kvm/ensure-guest-admin-shell.py`
  Checks that the KVM guest exposes a usable elevated shell session.
- `scripts/vm-kvm/guest_bridge.py`
  Shared KVM guest-bridge helper for upload, download, and execution flow.
- `scripts/vm-kvm/qga-exec.py`
  Executes an arbitrary process through QGA and returns structured output.
- `scripts/vm-kvm/qga-get-file.py`
  Downloads a guest file through QGA.
- `scripts/vm-kvm/qga-put-file.py`
  Uploads a host file into the guest through QGA.
- `scripts/vm-kvm/qga-run-powershell.py`
  Runs a PowerShell payload through QGA with structured response handling.
- `scripts/vm-kvm/qga_response_lib.py`
  Shared parser for QGA response envelopes.
- `scripts/vm-kvm/run-guest-app-deploy-smoke.py`
  Deploys the app into the KVM guest, launches the nested smoke check, and bubbles nested runner failures back to the host summary.
- `scripts/vm-kvm/run-guest-app-launch-smoke.py`
  Runs a KVM guest app launch smoke check against an existing deploy and distinguishes guest launch failures from transport or summary-contract failures.
- `scripts/vm-kvm/run-guest-app-publish-deploy-smoke.py`
  Builds, packages, deploys, and smoke-tests the app through the KVM lane while preserving nested deploy-smoke error details.
- `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py`
  Runs the ETW stackwalk capture lane through the KVM guest bridge with stage-aware bridge-artifact timeout and guest-stall reporting.
- `scripts/vm-kvm/run-guest-ghidra-string-xref-probe.py`
  Runs the Ghidra string-xref probe lane through the KVM guest bridge with stale-stage cleanup and launcher-stall fail-fast handling.
- `scripts/vm-kvm/run-guest-ghidra-symbolized-probe.py`
  Runs the Ghidra symbolized-branch probe lane through the KVM guest bridge with stale-stage cleanup and launcher-stall fail-fast handling.
- `scripts/vm-kvm/run-guest-local-kd-smoke.py`
  Runs the local-KD smoke lane through the KVM guest bridge.
- `scripts/vm-kvm/run-guest-procmon-bootlog.py`
  Runs the Procmon bootlog capture lane through the KVM guest bridge with stage-aware first-artifact timeout and guest-stall reporting.
- `scripts/vm-kvm/run-guest-reboot-observation.py`
  Runs a reboot observation lane through the KVM guest bridge with stage-aware post-reboot artifact timeout and guest-stall reporting.
- `scripts/vm-kvm/run-guest-registry-policy-probe.py`
  Runs the registry-policy probe lane through the KVM guest bridge.
- `scripts/vm-kvm/run-guest-wpr-boot-registry.py`
  Runs the boot-time WPR registry trace lane through the KVM guest bridge.
- `scripts/vm-kvm/run-power-kernel-symbol-hunt-pipeline.py`
  Runs the staged power/kernel symbol-hunt pipeline.
- `scripts/vm-kvm/run-power-kernel-symbol-hunt.py`
  Runs the power/kernel symbol-hunt lane for a single target set.
- `scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py`
  Runs the PowerRequestOverride reader-binding pipeline and result-ledger prefill. Supports `--dry-run` for planned commands and `--verify-only` to emit `ready_for_execute`, `blockers`, and an operator checklist without touching the VM.
- `scripts/vm-kvm/run-power-request-override-reader-binding-reacquire.py`
  Reacquires the PowerRequestOverride reader-binding artifacts through the KVM lane before handoff promotion.
- `registry-research-framework/scripts/generate_power_request_override_result_ledger.py`
  Generates the PowerRequestOverride result-ledger JSON and markdown review draft from the reacquired artifacts.
- `registry-research-framework/scripts/promote_power_request_override_result_ledger.py`
  Promotes the reviewed PowerRequestOverride result ledger into the dated audit targets.
- `registry-research-framework/scripts/verify_power_request_override_handoff_bundle.py`
  Verifies the handoff bundle and reports `ready_for_execute`, `blockers`, preview targets, and the operator checklist.
- `scripts/vm-kvm/serve-guest-bridge.py`
  Serves the temporary guest bridge used by KVM upload chains.
- `scripts/vm-kvm/summary_contract_lib.py`
  Shared helper library for JSON summary contracts emitted by KVM runners.
- `scripts/vm-kvm/type-to-guest.py`
  Types text into the active KVM guest session.
- `scripts/vm-kvm/validate-research-lane.py`
  Validates KVM research-lane prerequisites and expected tools.
- `scripts/vm-kvm/vm_env.py`
  Shared environment-variable resolver for KVM VM, snapshot, path, and bridge defaults.

## VMware / Host VM Scripts

VMware-focused orchestration, diagnostics, provisioning, and runtime probes.

- `scripts/vm/_resolve-vm-baseline.ps1`
  Shared VM baseline resolver for snapshot, guest profile, and staging defaults.
- `scripts/vm/_vmrun-common.ps1`
  Shared vmrun helper functions for guest credentials and host/guest file flow.
- `scripts/vm/app-deploy.ps1`
  Deploys the app payload into the configured guest VM.
- `scripts/vm/app-launch-smoke.ps1`
  Runs the guest-side app launch smoke helper.
- `scripts/vm/apply-defender-tooling-exclusions.ps1`
  Applies the bounded Defender exclusions used by trusted tooling lanes.
- `scripts/vm/apply-vmtools-hardening.ps1`
  Applies the VMware Tools hardening fixes used by fragile runtime lanes.
- `scripts/vm/benchmark-diskspd-wpr.ps1`
  Runs the diskspd wpr benchmark.
- `scripts/vm/benchmark-winsat-cpu-wpr.ps1`
  Runs the winsat cpu wpr benchmark.
- `scripts/vm/benchmark-winsat-mem-wpr.ps1`
  Runs the winsat mem wpr benchmark.
- `scripts/vm/build-kvm-bootstrap-iso.py`
  Builds the bootstrap ISO used for KVM guest provisioning.
- `scripts/vm/cleanup-guest-validation-artifacts.ps1`
  Removes guest-side validation leftovers after a smoke or benchmark run.
- `scripts/vm/cleanup-host-validation-artifacts.ps1`
  Removes host-side validation leftovers after a smoke or benchmark run.
- `scripts/vm/configure-kernel-debug-baseline.ps1`
  Configures kernel debug baseline.
- `scripts/vm/defender-enhanced-notifications-probe.ps1`
  Runs the Defender enhanced notifications probe.
- `scripts/vm/defender-hide-exclusions-visibility.ps1`
  Runs the Defender hide exclusions visibility probe.
- `scripts/vm/defender-policy-probe.ps1`
  Runs the Defender policy probe.
- `scripts/vm/defender-threat-file-hash-activity.ps1`
  Runs the Defender threat file hash activity probe.
- `scripts/vm/diagnose-qga-runtime-handshake.ps1`
  Diagnoses qga runtime handshake.
- `scripts/vm/diagnose-qga-vioserial-path.ps1`
  Diagnoses qga vioserial path.
- `scripts/vm/ensure-kvm-qga-channel.py`
  Checks that the KVM guest exposes a working QGA channel.
- `scripts/vm/ensure-shell-stable-snapshot.ps1`
  Ensures shell stable snapshot.
- `scripts/vm/export-high-risk-dumps.ps1`
  Exports high risk dumps.
- `scripts/vm/export-registry-key.ps1`
  Exports registry key.
- `scripts/vm/fix-guest-logon.ps1`
  Fixes guest logon.
- `scripts/vm/gamemode-procmon-probe.ps1`
  Utility script for gamemode procmon probe.
- `scripts/vm/get-vm-shell-health.ps1`
  Gets vm shell health.
- `scripts/vm/guest-app-artifact-audit.ps1`
  Audits guest-side app smoke artifacts and crash outputs.
- `scripts/vm/guest-validation-agent.ps1`
  Guest-resident validation agent entrypoint used by host orchestration.
- `scripts/vm/host-validation-controller.ps1`
  Host-side controller for the VM validation-agent workflow.
- `scripts/vm/install-dotnet-desktop-runtime.ps1`
  Installs dotnet desktop runtime.
- `scripts/vm/install-guest-validation-agent-local.ps1`
  Installs guest validation agent local.
- `scripts/vm/install-guest-validation-agent.ps1`
  Installs guest validation agent.
- `scripts/vm/invoke-guest-agent-diagnostic.ps1`
  Invokes guest agent diagnostic.
- `scripts/vm/invoke-mega-trigger-minimal.ps1`
  Invokes mega trigger minimal.
- `scripts/vm/log-vm-incident.ps1`
  Logs vm incident.
- `scripts/vm/new-regprobe-defender-excluded-baseline.ps1`
  Creates regprobe defender excluded baseline.
- `scripts/vm/new-regprobe-parallel-vm.ps1`
  Creates regprobe parallel vm.
- `scripts/vm/new-regprobe-tools-hardened-baseline.ps1`
  Creates regprobe tools hardened baseline.
- `scripts/vm/new-vmware-debug-only-baseline-plan.ps1`
  Creates vmware debug only baseline plan.
- `scripts/vm/new-vmware-debug-only-vm.ps1`
  Creates vmware debug only vm.
- `scripts/vm/new-windbg-registry-watch-script.ps1`
  Creates a WinDbg registry watch script for a target key or value.
- `scripts/vm/open-regprobe-in-vm.ps1`
  Opens RegProbe interactively inside the configured guest VM.
- `scripts/vm/provision-ida-headless.ps1`
  Installs and wires the headless IDA toolchain inside the guest environment.
- `scripts/vm/provision-symbol-tools.ps1`
  Installs the symbol and debugger tools used by static and runtime probes.
- `scripts/vm/query-logon-settings.ps1`
  Queries guest logon and autologon settings for troubleshooting.
- `scripts/vm/re-audit-ghidra-branch-template-queue.ps1`
  Re-audits queued Ghidra branch templates against the current artifact set.
- `scripts/vm/registry-policy-probe.ps1`
  Utility script for registry policy probe.
- `scripts/vm/repair-defender-runtime.ps1`
  Repairs defender runtime.
- `scripts/vm/request-guest-restart.ps1`
  Requests guest restart.
- `scripts/vm/run-app-launch-smoke-host.ps1`
  Runs app launch smoke host.
- `scripts/vm/run-appcompat-policy-probe.ps1`
  Runs appcompat policy probe.
- `scripts/vm/run-audio-devicecpl-runtime-probe.ps1`
  Runs audio devicecpl runtime probe.
- `scripts/vm/run-cpu-idle-states-benchmark.ps1`
  Runs cpu idle states benchmark.
- `scripts/vm/run-cpu-idle-states-minimal-regwrite-diagnostic.ps1`
  Runs cpu idle states minimal regwrite diagnostic.
- `scripts/vm/run-cpu-idle-states-orchestration-step-a.ps1`
  Runs cpu idle states orchestration step a.
- `scripts/vm/run-cpu-idle-states-orchestration-step-b.ps1`
  Runs cpu idle states orchestration step b.
- `scripts/vm/run-cpu-idle-states-orchestration-step-c.ps1`
  Runs cpu idle states orchestration step c.
- `scripts/vm/run-cpu-idle-states-orchestration-step-c1.ps1`
  Runs cpu idle states orchestration step c1.
- `scripts/vm/run-cpu-idle-states-orchestration-step-c2.ps1`
  Runs cpu idle states orchestration step c2.
- `scripts/vm/run-cpu-idle-states-orchestration-step-c3.ps1`
  Runs cpu idle states orchestration step c3.
- `scripts/vm/run-cpu-idle-states-orchestration-step-c4.ps1`
  Runs cpu idle states orchestration step c4.
- `scripts/vm/run-cpu-idle-states-orchestration-step-d.ps1`
  Runs cpu idle states orchestration step d.
- `scripts/vm/run-cpu-idle-states-orchestration-step.ps1`
  Runs cpu idle states orchestration step.
- `scripts/vm/run-cpu-idle-states-runtime-probe.ps1`
  Runs cpu idle states runtime probe.
- `scripts/vm/run-cpu-idle-states-write-diagnostics.ps1`
  Runs cpu idle states write diagnostics.
- `scripts/vm/run-defender-enhanced-notifications-probe.ps1`
  Runs defender enhanced notifications probe.
- `scripts/vm/run-defender-hide-exclusions-probe.ps1`
  Runs defender hide exclusions probe.
- `scripts/vm/run-defender-runtime-repair.ps1`
  Runs defender runtime repair.
- `scripts/vm/run-defender-threat-file-hash-probe.ps1`
  Runs defender threat file hash probe.
- `scripts/vm/run-dpc-timer-etw-trace-guest.ps1`
  Runs dpc timer etw trace guest.
- `scripts/vm/run-dpc-timer-etw-trace-launcher-guest.ps1`
  Runs dpc timer etw trace launcher guest.
- `scripts/vm/run-dpc-watchdog-control-wpr-filter-guest.ps1`
  Runs dpc watchdog control wpr filter guest.
- `scripts/vm/run-dpc-watchdog-profile-reboot-observation-guest.ps1`
  Runs dpc watchdog profile reboot observation guest.
- `scripts/vm/run-dpc-watchdog-profile-wpr-filter-guest.ps1`
  Runs dpc watchdog profile wpr filter guest.
- `scripts/vm/run-executionrequired-string-xref-guest.ps1`
  Runs executionrequired string xref guest.
- `scripts/vm/run-executive-worker-threads-etw-keyword-review.ps1`
  Runs executive worker threads etw keyword review.
- `scripts/vm/run-executive-worker-threads-procmon-bootlog.ps1`
  Runs executive worker threads procmon bootlog.
- `scripts/vm/run-executive-worker-threads-stress-trigger-probe.ps1`
  Runs executive worker threads stress trigger probe.
- `scripts/vm/run-explorer-compact-mode-runtime-probe.ps1`
  Runs explorer compact mode runtime probe.
- `scripts/vm/run-explorer-shell-registry-runtime-probe.ps1`
  Runs explorer shell registry runtime probe.
- `scripts/vm/run-fullscreen-optimizations-probe.ps1`
  Runs fullscreen optimizations probe.
- `scripts/vm/run-ghidra-string-xref-probe.ps1`
  Runs ghidra string xref probe.
- `scripts/vm/run-ghidra-symbolized-branch-probe.ps1`
  Runs ghidra symbolized branch probe.
- `scripts/vm/run-guest-app-artifact-audit.ps1`
  Runs guest app artifact audit.
- `scripts/vm/run-ida-string-xref-probe.ps1`
  Runs ida string xref probe.
- `scripts/vm/run-jpeg-import-quality-runtime-probe.ps1`
  Runs jpeg import quality runtime probe.
- `scripts/vm/run-kernel-timing-seeded-registry-boot-trace-guest.ps1`
  Runs kernel timing seeded registry boot trace guest.
- `scripts/vm/run-kernel-timing-wpr-boot-registry-guest.ps1`
  Runs kernel timing wpr boot registry guest.
- `scripts/vm/run-manual-value-benchmark.ps1`
  Runs manual value benchmark.
- `scripts/vm/run-power-control-batch-mega-trigger-runtime.guest.ps1`
  Runs power control batch mega trigger runtime.guest.
- `scripts/vm/run-qga-on-open-vioserial-path.ps1`
  Runs qga on open vioserial path.
- `scripts/vm/run-registry-batch-existence-probe.ps1`
  Runs registry batch existence probe.
- `scripts/vm/run-registry-batch-string-probe.ps1`
  Runs registry batch string probe.
- `scripts/vm/run-reliability-timestamp-probe.ps1`
  Runs reliability timestamp probe.
- `scripts/vm/run-service-shutdown-timeout-probe.ps1`
  Runs service shutdown timeout probe.
- `scripts/vm/run-session-watchdog-timeouts-boot-trace.ps1`
  Runs session watchdog timeouts boot trace.
- `scripts/vm/run-session-watchdog-timeouts-power-trigger-probe.ps1`
  Runs session watchdog timeouts power trigger probe.
- `scripts/vm/run-session-watchdog-timeouts-procmon-bootlog.ps1`
  Runs session watchdog timeouts procmon bootlog.
- `scripts/vm/run-session-watchdog-timeouts-s1-procmon-probe.ps1`
  Runs session watchdog timeouts s1 procmon probe.
- `scripts/vm/run-session-watchdog-timeouts-s1-scheduled-procmon-probe.ps1`
  Runs session watchdog timeouts s1 scheduled procmon probe.
- `scripts/vm/run-session-watchdog-timeouts-sleep-capability-probe.ps1`
  Runs session watchdog timeouts sleep capability probe.
- `scripts/vm/run-startup-delay-wpr-trace.ps1`
  Runs startup delay wpr trace.
- `scripts/vm/run-targeted-string-batch-probe.ps1`
  Runs targeted string batch probe.
- `scripts/vm/run-targeted-string-probe.ps1`
  Runs targeted string probe.
- `scripts/vm/run-validation-with-restart-watch.ps1`
  Runs validation with restart watch.
- `scripts/vm/run-vm-tooling-minimal-diagnostic.ps1`
  Runs vm tooling minimal diagnostic.
- `scripts/vm/run-win32-callout-bugcheck-neutral-perf-bench-guest.ps1`
  Runs win32 callout bugcheck neutral perf bench guest.
- `scripts/vm/run-win32k-callout-watchdog-etw-guest.ps1`
  Runs win32k callout watchdog etw guest.
- `scripts/vm/search-guest-binary-string.ps1`
  Searches guest binary string.
- `scripts/vm/send-kvm-text.py`
  Types text into the KVM guest console through the configured input bridge.
- `scripts/vm/test-vm-storage-health.ps1`
  Tests vm storage health.
- `scripts/vm/tool-health-smoke.ps1`
  Utility script for tool health smoke.
- `scripts/vm/try-enable-full-acpi-vmx.ps1`
  Tries enable full acpi vmx.

## Guest Tool Scripts

Guest-resident helpers invoked by host wrappers.

- `scripts/vm/guest-tools/procmon-safe.ps1`
  Utility script for procmon safe.
- `scripts/vm/guest-tools/run-etw-registry-stackwalk-capture.ps1`
  Runs etw registry stackwalk capture.
- `scripts/vm/guest-tools/run-ghidra-string-xref-probe.ps1`
  Runs ghidra string xref probe.
- `scripts/vm/guest-tools/run-ghidra-symbolized-probe.ps1`
  Runs ghidra symbolized probe.
- `scripts/vm/guest-tools/run-local-kd-smoke.ps1`
  Runs local kd smoke.
- `scripts/vm/guest-tools/run-procmon-bootlog-probe.ps1`
  Runs procmon bootlog probe.
- `scripts/vm/guest-tools/run-reboot-observation.ps1`
  Runs reboot observation.
- `scripts/vm/guest-tools/run-registry-policy-probe.ps1`
  Runs registry policy probe.
- `scripts/vm/guest-tools/run-wpr-boot-registry-probe.ps1`
  Runs wpr boot registry probe.

## IDA Helper Scripts

IDA-specific export helpers.

- `scripts/vm/ida/export_branch_analysis.py`
  Exports IDA branch-analysis data into repo-friendly artifacts.
