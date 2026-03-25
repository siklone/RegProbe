# Nohuto Configuration Audit

Date: 2026-03-09

This note summarizes the current gap between the app's configuration surface and the upstream nohuto research repos that we use as the main source set.

## Verified Current State

- The app has strong source coverage for shipped tweaks.
- `Docs/tweaks/tweak-provenance.csv` currently shows `267 repo-backed` tweaks and `1 category-fallback`.
- The remaining review-only item is `network.reset-winsock`, which still lacks direct nohuto repo documentation and should stay review-only for now.

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

The app does not currently expose dedicated user-facing areas for these nohuto domains:

- `affinities`
- `nvidia`
- `policies`

Recommended handling:

- `affinities`: expert-only workflow, not a default SAFE one-click toggle.
- `nvidia`: vendor-specific workspace for driver, NVCPL, and advanced NVIDIA research.
- `policies`: read-only policy catalog first, then curated actions where SAFE wrappers exist.

### 2. Category mapping is too app-centric

Current app groupings such as `Explorer`, `Notifications`, and `Performance` are understandable, but they do not map cleanly to the upstream research taxonomy.

Recommended handling:

- keep friendly labels for end users
- attach a hidden upstream domain mapping behind every option
- preserve strict nohuto-style state detection per option

### 3. Cleanup is mixed with configuration

The upstream `cleanup` domain is mostly operational maintenance, not persistent Windows configuration.

Examples:

- cache clearing
- log clearing
- `Windows.old` removal
- `shadow copy` deletion

Recommended handling:

- keep cleanup in the product, but not under the same mental model as persistent configuration
- label these as one-time maintenance actions
- never present them as normal reversible configuration toggles

### 4. Misc includes external tools and companion workflows

The upstream `misc` domain contains items such as `RegKit`, `NVFetch`, and `Explorer Blur`, which are not plain Windows configuration values.

Recommended handling:

- split `Misc` into:
  - `Windows settings`
  - `Companion tools`
  - `External installs`
- avoid presenting tool installation as if it were a native Windows registry or policy setting

### 5. Advanced nohuto research is deeper than the current UI surface

The current app already covers many power, system, network, privacy, and visibility values. The bigger gap is not raw count, it is the quality of presentation:

- upstream options often have suboptions and strict multi-value matching
- many advanced values have ranges, caveats, and fallback behavior
- current UI still compresses many options into a simpler toggle model

Recommended handling:

- show exact current-state evidence
- show suboptions where upstream docs define them
- show partial-match status when only some expected values match

## Domain By Domain Product Decision

### Strong and worth expanding

- `network`: keep and expand. This is one of the clearest high-value domains in both `win-config` and `win-registry`.
- `power`: keep and expand. Upstream research is deep and maps well to user-visible behavior.
- `privacy`: keep, but continue to explain side effects clearly because the category is broad and user trust matters.
- `system`: keep, but split broad buckets into clearer user language.
- `peripheral`: keep and deepen. USB, HID, and device power behavior are useful and already align with the hardware-first direction.
- `visibility`: keep. This domain productizes cleanly for end users.

### Present, but should be reframed

- `cleanup`: keep as maintenance, not as configuration.
- `misc`: keep only the actual Windows-facing settings in the main configuration surface. Move tool installs and companion utilities elsewhere.
- `security`: keep in a conservative explain-first mode. Do not let the upstream research pressure SAFE defaults into unsafe territory.

### Missing and should be added carefully

- `policies`: add as a read-only catalog first, then selectively promote actions after SAFE wrappers exist.
- `nvidia`: add as a vendor-specific advanced area only when NVIDIA hardware is present.
- `affinities`: add only as an expert workflow with validation and rollback guidance, never as a casual one-click toggle.

## Concrete Add, Remove, and Reclassify Actions

### Add next

- policy-backed configuration browser sourced from nohuto `policies`
- vendor-aware NVIDIA section for supported machines
- exact match and partial-match state reporting per configuration
- per-setting evidence text that explains what was detected locally
- better multi-option configuration cards where upstream docs define more than a simple on/off

### Reclassify

- move `Cleanup` out of the main persistent configuration mental model
- split `Misc` into `Windows settings`, `Companion tools`, and `Optional installs`
- treat runtime installers, helper tools, and vendor utilities as recommendations, not toggles

### Do not ship as default SAFE actions

- raw interrupt affinity edits
- raw NVIDIA driver-class bitmask edits
- undocumented or repo-indirect registry experiments
- any security-reducing setting that conflicts with SAFE rules

## What Looks Correct Today

- the shipped configuration surface is already strongly backed by nohuto sources
- the source pipeline is doing its job
- the biggest quality gap is product modeling, not lack of upstream research
- one remaining review-only operation (`network.reset-winsock`) is correctly being held back

## What Looks Incorrect Or Too Loose Today

- some user-facing groups are still organized around app convenience rather than upstream behavior
- operational maintenance and persistent settings are still too close together conceptually
- several advanced upstream options would not fit the current simple toggle model without losing important caveats
- current UI language is sometimes clearer than the raw repo taxonomy, but it still needs a hidden source-to-option mapping per item

## Recommended Immediate Work

1. Add hidden upstream-domain metadata to every configuration item.
2. Separate persistent configuration, maintenance actions, and optional installs in the UI.
3. Introduce exact local evidence text in each configuration detail panel.
4. Add read-only `Policies` ingestion before any new action surface.
5. Add vendor-aware `NVIDIA` recommendations and advanced configuration scaffolding.
6. Keep `affinities` internal until validation and rollback UX are mature enough.

## Recommended Productization Order

1. Add hidden upstream mapping metadata for every user-facing configuration.
2. Create a read-only `Policies` browser from nohuto `policies`.
3. Build an expert-only `NVIDIA` area for supported NVIDIA systems.
4. Build an expert-only `Interrupt Affinity` area with validation guidance, not blind one-click actions.
5. Reclassify cleanup actions outside the main configuration workflow.
6. Split companion tools out of `Misc`.

## Keep or Remove

Keep:

- repo-backed privacy, power, network, system, peripheral, and visibility settings
- current source pipeline in the background
- conservative SAFE gating

Do not promote yet:

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

