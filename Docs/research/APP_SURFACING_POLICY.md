# App Surfacing Policy

Every proven registry key-value combination that has a concrete, app-renderable state should surface inside RegProbe as a research card as soon as it is validated.

## Current rule

- If a research record is `validated`, marked stable for the current build family, and resolves to one registry target with one concrete value state, it belongs in the app immediately.
- The in-app surface must preserve the research `record_id` as the tweak id so evidence classes, promotion gates, and card presentation bind to the same record.
- New values discovered later should extend the same surfaced record instead of creating a parallel card when the underlying key-value lane is the same.

## First-wave scope

The current research-card ingest path intentionally covers the records that the app can represent faithfully today:

- One registry target
- One concrete value state
- Standard registry types such as `REG_DWORD`, `REG_QWORD`, `REG_SZ`, `REG_MULTI_SZ`, or `REG_BINARY`

These shapes are held for a later expansion wave until the app has dedicated value models:

- Missing-only states
- Registry pairs
- Key subtrees
- Multi-value surfaces that need explicit preset selection

## Source of truth

- Research records remain the canonical source of truth in `research/records/`.
- The app-consumable projection lives in `Docs/research/app-surface/validated-registry-values.json`.
- `tests/python/test_research_app_surface_manifest.py` is the guardrail that fails when an eligible proven record is missing from that projection.

## Operational expectation

- Research updates that produce a new eligible concrete value must update the app-surface projection in the same wave.
- UI presence does not imply mutability. Evidence-class and promotion-gate metadata still decide whether a surfaced card is blocked, hold-only, or actionable.
