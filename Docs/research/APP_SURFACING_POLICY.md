# App Surfacing Policy

Every proven app-renderable control state should surface inside RegProbe as a research card as soon as the checked-in build lane is stable enough to represent it honestly.

## Current rule

- If a research record is `validated` and can be rendered honestly by the checked-in research-card loader, it belongs in the app immediately.
- Intentionally held `draft` records still require an explicit stable build-family marker before they enter the app surface.
- `deprecated` audit-trail records are still allowed on the app surface when they are preserving an already-shipped app behavior and the manifest-backed card can represent that legacy parity honestly.
- `deprecated` child audit-trail records that only document one slice of a broader live composite do not need their own live catalog card; they still need checked-in research metadata that points back to the real shipped parent or child surface.
- The in-app surface must preserve the research `record_id` as the tweak id so evidence classes, promotion gates, and card presentation bind to the same record.
- New values discovered later should extend the same surfaced record instead of creating a parallel card when the underlying key-value lane is the same.

## Currently supported shapes

The checked-in research-card ingest path now covers the control shapes that the app can represent faithfully:

- One registry-backed target, including direct registry and group-policy-backed registry writes
- One concrete value state
- One registry target with multiple concrete values written as a single batch
- Multi-target registry or group-policy-backed registry writes that the app applies as one coordinated batch across all surfaced targets
- One registry target with multiple app-visible preset choices
- One registry target expressed as a read-only registry subtree observation card
- One registry target whose baseline is missing but whose research lane has a concrete, evidence-backed write value
- One single-target research lane whose checked-in app implementation writes multiple concrete registry values and is therefore rendered as one coordinated batch card
- Multi-target research records where exactly one registry target is the checked-in app-backed surfaced write and the remaining targets are retained only as historical or audit-trail context
- Standard registry types such as `REG_DWORD`, `REG_QWORD`, `REG_SZ`, `REG_MULTI_SZ`, or `REG_BINARY`
- One service start-mode target rendered through the checked-in app-backed disable/start-mode action
- One scheduled-task target or scheduled-task batch rendered through the checked-in app-backed disabled-task action
- One command-backed control rendered through a dedicated research-provider bridge when the command surface matches the faithful supported app model

These shapes are still held for a later expansion wave until the app has dedicated value models:

- Pure missing-only states with no concrete surfaced write state
- Records whose only faithful app representation would require a custom non-registry interaction model

## Source of truth

- Research records remain the canonical source of truth in `research/records/`.
- The app-consumable projection lives in `Docs/research/app-surface/validated-registry-values.json`.
  The filename is legacy, but the projection now includes non-registry service and scheduled-task cards too.
- `Docs/research/app-surface/intentional-not-mapped-records.json` is the checked-in ledger for proven records that are intentionally not surfaced as live cards in the checked-in app projection.
- `Docs/research/app-surface/app-only-catalog-tweaks.json` is the checked-in ledger for live app cards that intentionally remain outside the research-record corpus in the checked-in app projection.
- `scripts/research/generate_app_surface_manifest.py` rebuilds that projection from the eligible research records.
- `tests/python/test_research_app_surface_manifest.py` is the guardrail that fails when an eligible proven record is missing from the app surface.
- `tests/python/test_research_app_surface_manifest.py` also fails when the checked-in app-only ledger drifts from the live first-party provider source set.
- `tests/python/test_research_app_surface_manifest.py` also checks that each app-only ledger entry points at the exact live provider file that still surfaces that tweak id.
- `tests/ResearchAppSurfaceCompletenessTests.cs` is the runtime guardrail that fails when a `matches-research` record claims to ship through `ResearchAppSurfaceTweakProvider` but does not resolve to a real in-app card.
- `tests/ResearchAppSurfaceCompletenessTests.cs` also fails when the live in-app catalog contains any tweak id that is neither backed by a checked-in research record nor listed in `Docs/research/app-surface/app-only-catalog-tweaks.json`.
- Validated records and stable-draft records should enter the app through `app/Services/TweakProviders/ResearchAppSurfaceTweakProvider.cs` whenever the checked-in research-card loader can represent them honestly.
- Existing first-party provider parity is a temporary bridge, not the target end state; when the research-card loader can express the record faithfully, migrate it into the manifest-backed surface.

## Operational expectation

- Research updates that produce a new eligible concrete value must update the app-surface projection in the same wave.
- Records already shipped through the research-card projection must update `app_current_implementation.status` to `matches-research` in the same wave.
- Records already shipped through another provider should migrate into the manifest-backed research surface as soon as the loader can express them without losing fidelity.
- Deprecated audit-trail child records that exist only to preserve the details of a broader composite or split bundle remain documented as `deprecated` instead of pretending they still deserve separate live cards.
- Stable draft records are allowed on the in-app surface when the card can present them faithfully as blocked, hold-only, or research-first items through promotion-gate and evidence-class metadata.
- Proven records that stay `not-mapped` must appear in `Docs/research/app-surface/intentional-not-mapped-records.json` with the exact checked-in provider-source and notes rationale so accidental card loss cannot hide inside the backlog.
- Live app cards that intentionally remain outside the research-record corpus must appear in `Docs/research/app-surface/app-only-catalog-tweaks.json` with their checked-in provider-source and rationale so app-only legacy surfaces cannot drift silently.
- UI presence does not imply mutability. Evidence-class and promotion-gate metadata still decide whether a surfaced card is blocked, hold-only, or actionable.
