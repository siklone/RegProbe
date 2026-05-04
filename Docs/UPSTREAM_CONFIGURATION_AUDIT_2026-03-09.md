# Upstream Configuration Audit

Date: 2026-03-09

This note summarizes the current gap between the app's configuration surface and the upstream research repos, including the nohuto source set that still acts as a major discovery input.

## Verified Current State

- The app has strong source coverage for shipped tweaks.
- At audit time, `Docs/tweaks/tweak-provenance.csv` showed `267 repo-backed` tweaks and `1 category-fallback`.
- At audit time, the remaining review-only item was `network.reset-winsock`, which still lacked direct nohuto repo documentation and therefore remained review-only in that pass.

## Verified Upstream Domains

The current upstream `win-config` mirror exposes these top-level domains:

- `affinities`
- `cleanup`
- `misc`
- `network`
- `nvidia`
- `peripheral`
- `policies`
- `power`
- `privacy`
- `security`
- `system`
- `visibility`

The current in-app tweak catalog groups user-facing items into:

- `Audio`
- `Cleanup`
- `Explorer`
- `Misc`
- `Network`
- `Notifications`
- `Other`
- `Performance`
- `Peripheral`
- `Power`
- `Privacy`
- `Security`
- `System`
- `Visibility`

## Main Gaps

### 1. Missing upstream domains

At audit time, the app did not expose dedicated user-facing areas for these upstream domains:

- `affinities`
- `nvidia`
- `policies`

Audit-time handling notes:

- `affinities`: expert-only workflow, not a default SAFE one-click toggle.
- `nvidia`: vendor-specific workspace for driver, NVCPL, and advanced NVIDIA research.
- `policies`: read-only policy catalog first, then curated actions where SAFE wrappers exist.

### 2. Category mapping is too app-centric

Current app groupings such as `Explorer`, `Notifications`, and `Performance` are understandable, but they do not map cleanly to the upstream research taxonomy.

Audit-time handling notes:

- keep friendly labels for end users
- attach a hidden upstream domain mapping behind every option
- preserve strict upstream-style state detection per option

### 3. Cleanup is mixed with configuration

The upstream `cleanup` domain is mostly operational maintenance, not persistent Windows configuration.

Examples:

- cache clearing
- log clearing
- `Windows.old` removal
- `shadow copy` deletion

Audit-time handling notes:

- keep cleanup in the product, but not under the same mental model as persistent configuration
- label these as one-time maintenance actions
- never present them as normal reversible configuration toggles

### 4. Misc includes external tools and companion workflows

The upstream `misc` domain contains items such as `RegKit`, `NVFetch`, and `Explorer Blur`, which are not plain Windows configuration values.

Audit-time handling notes:

- split `Misc` into:
  - `Windows settings`
  - `Companion tools`
  - `External installs`
- avoid presenting tool installation as if it were a native Windows registry or policy setting

### 5. Advanced upstream research is deeper than the current UI surface

The checked-in app already covers many power, system, network, privacy, and visibility values. The bigger gap is not raw count, it is the quality of presentation:

- upstream options often have suboptions and strict multi-value matching
- many advanced values have ranges, caveats, and fallback behavior
- current UI still compresses many options into a simpler toggle model

Audit-time handling notes:

- show exact current-state evidence
- show suboptions where upstream docs define them
- show partial-match status when only some expected values match

## Domain By Domain Product Decision

### Domains With Strong Audit-Time Fit

- `network`: strong upstream depth and clean overlap with the app surface.
- `power`: deep upstream research with clear user-visible behavior mapping.
- `privacy`: broad category with strong coverage, but side effects still need clear explanation.
- `system`: broad coverage that benefits from clearer user-facing subdivision.
- `peripheral`: useful USB, HID, and device-power coverage that already fits the hardware-first direction.
- `visibility`: user-facing domain with relatively clean product mapping.

### Domains Needing Retained Reframing

- `cleanup`: modeled as maintenance rather than as persistent configuration.
- `misc`: mixes Windows-facing settings with tool installs and companion utilities.
- `security`: needs conservative explain-first treatment so upstream breadth does not get mistaken for SAFE defaults.

### Missing Domains Held As Careful-Add Candidates

- `policies`: initially treated as a read-only catalog, with action promotion only after SAFE wrappers exist.
- `nvidia`: treated as a vendor-specific advanced area only when NVIDIA hardware is present.
- `affinities`: treated as an expert workflow with validation and rollback guidance, not as a casual one-click toggle.

## Concrete Audit-Time Follow-Ups

### Candidate Next Additions

- policy-backed configuration browser sourced from upstream `policies`
- vendor-aware NVIDIA section for supported machines
- exact match and partial-match state reporting per configuration
- per-setting evidence text that explains what was detected locally
- multi-option configuration cards where upstream docs define more than a simple on/off

### Candidate Reclassifications

- move `Cleanup` out of the main persistent configuration mental model
- split `Misc` into `Windows settings`, `Companion tools`, and `Optional installs`
- treat runtime installers, helper tools, and vendor utilities as companion actions, not toggles

### Not Suitable As Default SAFE Actions

- raw interrupt affinity edits
- raw NVIDIA driver-class bitmask edits
- undocumented or repo-indirect registry experiments
- any security-reducing setting that conflicts with SAFE rules

## What Looks Correct Today

- the shipped configuration surface is already strongly backed by upstream sources
- the source pipeline is doing its job
- the biggest quality gap is product modeling, not lack of upstream research
- one remaining review-only operation (`network.reset-winsock`) is correctly being held back

## What Looks Incorrect Or Too Loose Today

- some user-facing groups are still organized around app convenience rather than upstream behavior
- operational maintenance and persistent settings are still too close together conceptually
- several advanced upstream options would not fit the current simple toggle model without losing important caveats
- current UI language is sometimes clearer than the raw repo taxonomy, but it still needs a hidden source-to-option mapping per item

## Immediate Work Candidates

1. Add hidden upstream-domain metadata to every configuration item.
2. Separate persistent configuration, maintenance actions, and optional installs in the UI.
3. Introduce exact local evidence text in each configuration detail panel.
4. Add read-only `Policies` ingestion before any new action surface.
5. Add vendor-aware `NVIDIA` recommendations and advanced configuration scaffolding.
6. Keep `affinities` internal until validation and rollback UX are mature enough.

## Audit-Time Productization Order

1. Add hidden upstream mapping metadata for every user-facing configuration.
2. Create a read-only `Policies` browser from upstream `policies`.
3. Build an expert-only `NVIDIA` area for supported NVIDIA systems.
4. Build an expert-only `Interrupt Affinity` area with validation guidance, not blind one-click actions.
5. Reclassify cleanup actions outside the main configuration workflow.
6. Split companion tools out of `Misc`.

## Keep or Hold

Retain:

- repo-backed privacy, power, network, system, peripheral, and visibility settings
- current source pipeline in the background
- conservative SAFE gating

Hold from promotion:

- `network.reset-winsock`
- raw `affinities` actions
- raw NVIDIA bitmask or driver-class registry editing
- external companion tools as default SAFE toggles

## Source Notes

Primary upstream references used for this audit:

- `research/_source-mirrors/win-config/home.md`
- `research/_source-mirrors/win-config/nvidia/desc.md`
- `research/_source-mirrors/win-config/policies/desc.md`
- `research/_source-mirrors/win-config/affinities/desc.md`
- `research/_source-mirrors/win-registry/README.md`
- `Docs/tweaks/tweak-provenance.csv`
- `Docs/tweaks/tweak-provenance-missing.csv`
