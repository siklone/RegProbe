# App Surfacing Policy

Every proven registry key-value combination that has a concrete, app-renderable state should surface inside RegProbe as a research card as soon as the current build lane is stable enough to represent it honestly.

## Current rule

- If a research record is `validated` or an intentionally held `draft`, marked stable for the current build family, and can be rendered honestly by the current research-card loader, it belongs in the app immediately.
- The in-app surface must preserve the research `record_id` as the tweak id so evidence classes, promotion gates, and card presentation bind to the same record.
- New values discovered later should extend the same surfaced record instead of creating a parallel card when the underlying key-value lane is the same.

## Currently supported shapes

The current research-card ingest path now covers the registry shapes that the app can represent faithfully today:

- One registry target
- One concrete value state
- One registry target with multiple concrete values written as a single batch
- One registry target with multiple app-visible preset choices
- One registry target whose baseline is missing but whose research lane has a concrete, evidence-backed write value
- Standard registry types such as `REG_DWORD`, `REG_QWORD`, `REG_SZ`, `REG_MULTI_SZ`, or `REG_BINARY`

These shapes are still held for a later expansion wave until the app has dedicated value models:

- Key subtrees
- Multi-target records that need coordinated writes across distinct registry targets
- Pure missing-only states with no concrete surfaced write state
- Records whose only honest app representation would require a custom non-registry interaction model

## Source of truth

- Research records remain the canonical source of truth in `research/records/`.
- The app-consumable projection lives in `Docs/research/app-surface/validated-registry-values.json`.
- `scripts/research/generate_app_surface_manifest.py` rebuilds that projection from the eligible research records.
- `tests/python/test_research_app_surface_manifest.py` is the guardrail that fails when an eligible proven record is missing from the app surface.
- Most validated records enter the app through `app/Services/TweakProviders/ResearchAppSurfaceTweakProvider.cs`.
- A validated record may also satisfy the policy through an existing first-party provider when `app_current_implementation.status = matches-research` and the record metadata points at the shipping provider.

## Operational expectation

- Research updates that produce a new eligible concrete value must update the app-surface projection in the same wave.
- Records already shipped through the research-card projection must update `app_current_implementation.status` to `matches-research` in the same wave.
- Records already shipped through another provider must still update `app_current_implementation.status` to `matches-research` and document the provider file that now matches the research lane.
- Stable draft records are allowed on the in-app surface when the card can honestly present them as blocked, hold-only, or research-first items through promotion-gate and evidence-class metadata.
- UI presence does not imply mutability. Evidence-class and promotion-gate metadata still decide whether a surfaced card is blocked, hold-only, or actionable.
