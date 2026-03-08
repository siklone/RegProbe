# Nohuto Configuration Sources

This document defines how the app should use the four nohuto repositories that now form the configuration intelligence layer.

## Source Roles

- `win-config`: primary option catalog. Use it to discover user-facing configuration candidates, group them by category, and inherit its strict "all required values must match" detection mindset.
- `win-registry`: provenance and evidence layer. Use it to understand which keys are read on boot, which values exist by default, and how Windows components actually consume them.
- `decompiled-pseudocode`: internals verification layer. Use it to confirm semantics and bounds, not as a direct end-user tweak source.
- `regkit`: inspection and troubleshooting companion. Use it for trace/default workflows, registry exploration, and deep-linking advanced users into supporting context.

## Safe Productization Rules

- `win-config` can seed options, but every shipped action must still be wrapped in `Detect -> Apply -> Verify -> Rollback`.
- `win-registry` and `decompiled-pseudocode` are research inputs. They should strengthen confidence, explain behavior, and surface defaults, but they do not automatically qualify a tweak as SAFE.
- `regkit` should remain an advanced inspection surface. We can deep-link into it or export data compatible with it, but we should not mirror its elevated editing behavior as one-click SAFE actions.
- Security-reducing actions stay out of SAFE defaults unless they are explicitly marked advanced/unsafe elsewhere in the product.

## How Each Repo Maps To The App

### win-config

Best source for:

- configuration categories (`network`, `power`, `privacy`, `security`, `system`, `visibility`, `peripheral`, `nvidia`)
- documented options and suboptions
- strict state detection expectations
- external helper script references that may later become optional tools

Product work that should come from it:

- configuration catalog
- per-option "current state" badges
- "what this changes" summaries
- curated one-click actions after SAFE wrappers are implemented

### win-registry

Best source for:

- boot-read traces and observed registry activity
- default values and value ranges
- Windows subsystem research (`dxgkrnl`, `dwm`, `mmcss`, `stornvme`, `intel-nic`, `usb`, `pnp`)
- proving whether a key is actually read by the OS or a driver

Product work that should come from it:

- detection probes
- defaults/reference panels
- provenance/evidence links beside options
- "why this knob exists" explanations

### decompiled-pseudocode

Best source for:

- subsystem semantics inside Windows binaries
- bounds, coercion rules, and fallback behavior
- non-obvious interactions across graphics, storage, USB, MMCSS, ACPI, and kernel code

Product work that should come from it:

- internal validation notes
- range checks for advanced settings
- better warnings before risky values
- justification text in expert/detail views

### regkit

Best source for:

- power-user registry inspection workflows
- trace/default preset concepts
- advanced views of live registry state
- cases where standard RegEdit hides important structure

Product work that should come from it:

- "Open in RegKit" actions for advanced users
- trace/default export compatibility
- troubleshooting workflows and support playbooks

## Recommended Expansion Order

### Source Feed and Documentation

- Track upstream changes for all four repos.
- Save a local machine-readable report and a readable markdown briefing.
- Show tracked repos, last check time, and top impacted domains on the dashboard.

### Configuration Catalog Ingestion

- Build a curated local option catalog from `win-config`.
- Keep repo metadata attached to each option: source repo, source path, category, and evidence links.
- Add read-only "current state" detection before enabling any action buttons.

### Evidence-Backed State Model

- Use `win-registry` to display defaults, observed-read paths, and trace notes.
- Use `decompiled-pseudocode` to annotate advanced values with ranges, coercion behavior, and caveats.
- Expose these in compact detail drawers rather than crowding the main dashboard.

### SAFE One-Click Actions

- Promote only curated options into one-click actions.
- Every action must stay reversible and logged.
- When a tweak is partially matched, show exactly which expected values differ.

### Advanced Inspection Bridge

- Add optional deep links or exported context for `regkit`.
- Support troubleshooting bundles that include current state, defaults, and evidence references.

## Immediate High-Value Domains

- `Network`: NIC, DNS, NDIS, SMB, offload, RSS, QoS, and latency-related values.
- `Power`: idle policies, Modern Standby interactions, USB/device power, platform-specific power gates.
- `Graphics`: DXG, DWM, MPO, scheduler, TDR, and vendor-adjacent display behavior.
- `Storage`: StorNVMe, StorPort, queueing, sector-size quirks, power transitions.
- `Peripheral`: USBHUB/XHCI, touch, pen, mouse, keyboard, device enumeration behavior.
- `Security`: only detection and explanation first; SAFE actions must stay conservative.

## What We Should Not Do

- auto-import every repo script and expose it as a SAFE action
- treat reverse-engineered notes as sufficient validation on their own
- collapse research, defaults, and actions into a single unreviewed toggle
- hide provenance from the user once we start shipping repo-derived options
