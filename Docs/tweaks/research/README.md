# Setting Research System

All research artifacts in this folder are English-only on purpose. The goal is to build a trustworthy setting knowledge base before we expand the set of tweaks that the app is allowed to apply.

## What A Research Record Must Answer

Every record should make the following questions easy to answer:

- What is the setting?
- Where is it stored?
- Which values are possible?
- What does each value do in plain language?
- What is the Windows default or default policy state?
- Which profiles make sense for real user scenarios?
- What breaks when the setting changes?
- Where did we get the information?
- How confident are we?
- Is the app allowed to expose Apply for this setting?

## Core Principles

- Documentation first. A setting can be useful even when it is not yet safe to apply.
- Sources are required. We do not ship anonymous values.
- Nohuto may be used for naming lineage and upstream context, but not as the source of value semantics. If a record cites nohuto, say exactly which upstream file or path it came from and make it clear that the value meanings still come from Microsoft docs, VM/runtime proof, or other record evidence.
- English only. Names, notes, evidence summaries, and labels stay consistent.
- Reverse engineering is evidence, not a final recommendation by itself.
- AI inference alone never unlocks Apply.
- "Previous value" and "Windows default" are different things and must stay separate.

## Key Terms

### Windows Default

The state Windows uses when the setting is untouched or not configured. This may be a concrete value, a missing registry value, a per-interface default, or a version-dependent behavior.

### Observed App Value

The value currently written by the app implementation. This is descriptive only. It does not automatically mean the value is correct.

### Recommended Profile Value

A value we are willing to recommend for a specific scenario such as `privacy-focused`, `secure-modern-network`, or `legacy-compatibility`.

### Previous Value

A runtime snapshot captured before Apply. This belongs to state storage, logs, and rollback history. It should not be confused with a Windows default and it should not live only in human-written documentation.

## Evidence And Confidence

Use explicit evidence items for every record. Preferred evidence order:

1. Official Microsoft documentation
2. Official policy or CSP mapping
3. UI toggle diff or command-line diff on a clean VM
4. Registry observation and live testing
5. Procmon, ETW, or WPR traces
6. Binary analysis or reverse engineering
7. Community reports
8. Inference

Recommended confidence meaning:

- `high`: Official docs and direct validation agree.
- `medium`: Official docs or strong reverse engineering exist, but validation is still limited.
- `low`: Partial evidence only, missing validation, or open contradictions.
- `unknown`: We found references but cannot responsibly recommend behavior yet.

### What ADMX Proof Guarantees

ADMX proof is strong control-surface evidence, but it is not the same thing as runtime execution proof.

What ADMX proof does guarantee:

- Microsoft defines the registry path and value name as a supported policy management surface.
- Microsoft defines what `enabled` and `disabled` states write to that surface.
- The setting is legitimate enough to be managed through Group Policy or an equivalent policy layer.

What ADMX proof does not guarantee by itself:

- That every Windows version still honors the policy in exactly the same way.
- That a user-side write and a machine-side policy write have identical runtime effects.
- That the setting was observed being read and applied on a live system during the current research pass.

Practical interpretation:

- Treat ADMX as `high` confidence proof for the control surface.
- Treat Procmon, VM tests, or official behavior documentation as stronger proof for runtime behavior.
- For critical or behavior-sensitive records, it is reasonable to keep `decision.needs_vm_validation = true` even when ADMX proof is sufficient to move the record to `validated`.

### Enum And Range Proof Order

When a setting is not a simple binary on/off switch, use the following evidence order before escalating to reverse engineering:

1. ADMX enum or `valueList` mapping
2. Microsoft Learn or ADML behavior text
3. WPR or ETW behavioral diff
4. Reverse engineering such as Ghidra

Practical interpretation:

- Binary on/off with explicit ADMX `enabledValue` and `disabledValue`: ADMX is usually sufficient for value meaning. Procmon is still useful for runtime-write confirmation, but not for basic value semantics.
- Enum or multi-state setting with an ADMX `valueList` or enum block: treat ADMX as the primary source for the meaning of each documented value.
- Enum or multi-state setting without a usable ADMX enum and without a Microsoft Learn mapping: use WPR, ETW, or other behavioral diffs before escalating to disassembly.
- Fully undocumented settings with no usable primary source: Ghidra or similar reverse engineering is a last-resort tool, not the first step.

Do not escalate to Ghidra just because a value is non-binary. If the ADMX already defines the enum list, that is the authoritative meaning source for the documented values.

### Single-Value Validation Workflow

Use this workflow when a registry key exposes only one value name or when the record gives you a single obvious value to validate.

1. Confirm the exact path, value name, and baseline state from the strongest source available.
2. Identify the closest safe alternate state to test. For binary values, use the opposite state. For raw or multi-bit values, choose the nearest documented alternative or the current observed baseline.
3. Validate in the Win25H2Clean VM only.
4. Prefer the lightest proof that can answer the question:
   - Procmon or a UI/settings diff for user-facing toggle surfaces
   - WPR or WPA for boot, logon, CPU, GPU, disk, or scheduler behavior
   - Ghidra or decompiled pseudocode for undocumented or internal values
5. Record the full reversible cycle:
   - baseline
   - apply
   - verify
   - restore
6. Treat a key as validated only when the value meaning and the live read/write path are both captured well enough to explain the behavior without guesswork.
7. Keep nohuto references limited to lineage and naming unless a record explicitly says the value semantics also came from a mirror or dump.

For a reusable capture template, see [notes/single-value-validation.md](./notes/single-value-validation.md).

### Procmon Requirement Tiers

Use the following three-way split when deciding whether Procmon is needed:

#### Procmon not required

Use this when:

- an official ADMX or Policy CSP source defines the exact path and value name
- the app writes that same path and value name
- the policy surface is the intended control surface

Interpretation:

- the control surface is already strong enough for validation
- Procmon can still be useful later, but it is not required to move the record forward

Examples:

- `EnableMulticast`
- `DisableSmartNameResolution`

#### Procmon helpful

Use this when:

- an official policy surface exists
- the app writes a different path, usually a user-side preference or observed runtime key
- the feature area matches, but the exact implementation surface does not

Interpretation:

- do not validate from feature similarity alone
- Procmon or a settings-diff is the fastest way to confirm whether the observed user-side key is what Windows Settings actually writes

Examples:

- `TaskbarMn`
- `TaskbarDa`

#### Procmon required

Use this when:

- no official Microsoft control surface has been found
- the setting is supported only by repo notes, traces, reverse engineering, or "it seems to work" observations
- the registry key is undocumented or behavior-sensitive

Interpretation:

- keep the record in `docs-first` or `review-required`
- do not promote it based on inference alone
- Procmon, VM testing, or stronger runtime evidence is required before validation can move forward

Examples:

- `PerfBoostAtGuaranteed`
- other undocumented raw performance or kernel keys

Rule of thumb:

- exact official surface match: Procmon usually not required
- official feature but different surface: Procmon is usually helpful
- no official surface: Procmon is usually required

### Undocumented Key Range Work

Documentation-first still comes before runtime inference.

If a key has no complete Microsoft documentation and we do not yet know the full value range, finish the normal documentation pass first and treat runtime range exploration as a later follow-up task.

Use runtime testing for undocumented keys to answer questions such as:

- which values are actually written or read on a tested build
- whether `missing`, `0`, `1`, or other tested values produce visibly different behavior
- which process or service consumes the key

Do not treat runtime testing alone as exhaustive proof of the full allowed range.

For the repeatable workflow and recording template, see [notes/undocumented-runtime-validation.md](./notes/undocumented-runtime-validation.md).

### Accessibility And Safety Caveat

Some Windows accessibility features expose observable registry values, but Microsoft may still recommend using Win32 APIs such as `SystemParametersInfo` instead of writing those values directly.

Practical interpretation:

- A documented accessibility registry value is not automatically equivalent to an ADMX-backed policy surface.
- Accessibility records should usually start at `medium` source strength unless a stronger primary runtime contract is available.
- For accessibility settings, it is reasonable to keep `decision.needs_vm_validation = true` even when the registry mapping is documented.
- Be conservative with `decision.apply_allowed` because disabling accessibility helpers can seriously impact users who depend on them.

### Multi-State And Runtime-Surface Caveat

Do not collapse true multi-value policy surfaces or runtime UX surfaces into fake booleans.

Examples:

- `DODownloadMode` is a multi-state Delivery Optimization policy, not a simple on/off switch.
- `ActiveHoursStart` and `ActiveHoursEnd` are UX/runtime surfaces, not the same class of evidence as a policy-backed Windows Update control.
- Printing prompt-suppression settings and driver isolation settings require exact surface matching because nearby print runtime keys can be misleading.

### Policy Surface Vs Runtime Consent Surface

Some Windows features expose both a policy-management surface and a separate runtime consent or preference surface.

Examples:

- `LetAppsAccessCamera` and the other `LetAppsAccess*` values are policy-backed app-privacy controls.
- `CapabilityAccessManager\\ConsentStore\\<capability>` is a runtime consent surface.

Rule:

- Do not validate a runtime consent key from policy documentation alone.
- Do not claim that a policy-backed app-privacy setting fully governs all Win32 desktop apps unless the primary source explicitly says so.

### Security Feature Override Caveat

Security features such as VBS, Credential Guard, and Windows Hello can have registry surfaces that are real but not fully authoritative by themselves.

Practical interpretation:

- A documented registry key may still be overridden by firmware state, policy state, Windows version behavior, or security-feature lock-in.
- Keep `SYSTEM` runtime paths and `SOFTWARE\\Policies` paths separate.
- For disable-style records involving VBS, Credential Guard, or Windows Hello, it is reasonable to keep `decision.needs_vm_validation = true` even when the registry path itself is documented.
- If a setting can require UEFI interaction or physical presence to fully disable, do not model it as a normal reversible casual tweak.

### Unit And Surface Mismatch Caveat

Some Windows features expose similar-looking settings across more than one surface, but the units or semantics differ.

Examples:

- Event Log runtime channel `MaxSize` uses bytes.
- Event Log policy `MaxSize` uses kilobytes.

Rule:

- Do not validate one surface from documentation for the other.
- Always capture the exact surface, unit, and value range in `validation_proof.exact_quote_or_path`.

### Security Mitigation Override Caveat

Registry values that disable vulnerability mitigations or core platform hardening should not be treated as ordinary performance tweaks.

Examples:

- `FeatureSettingsOverride`
- `FeatureSettingsOverrideMask`

Practical interpretation:

- Default to `decision.apply_allowed = false` for general-user exposure unless there is an explicit, narrowly scoped troubleshooting workflow.
- Default to `recommended_for_general_users = false`.
- Treat these as high-risk security decisions even when the registry path itself is officially documented.

### Windows 11 Settings Reference Caveat

The Windows 11 settings reference page can document a setting as a subkey-based surface instead of a simple value under the parent key.

Example pattern:

- documented path: `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarDa`
- documented value name: `SystemSettings_DesktopTaskbar_Da`
- documented type: `REG_SZ`

This is not the same surface as:

- app path: `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced`
- app value name: `TaskbarDa`
- app type: `REG_DWORD`

Rule:

- If Microsoft documents a subkey and the app writes a value on the parent key, treat that as a surface mismatch.
- Do not validate from feature similarity alone.
- Mark the record as needing either an implementation fix or a Procmon/settings-diff follow-up, depending on whether the app should migrate to the documented surface or intentionally track a separate observed runtime key.

### Service Control Surface Caveat

Service-disable tweaks do not fit the same proof shape as ADMX, CSP, or direct registry-policy records.

Typical service evidence looks different:

- the exact service exists in the local Service Control Manager database
- Microsoft may publish general service guidance or disable taxonomy
- there is usually no `enabledValue` or `disabledValue` block to quote

Rule:

- Do not fake registry-style proof for a service tweak.
- Do not mark a service record `validated` just because the service exists locally.
- Separate three questions:
  - Is the exact service identifier real on the reviewed build?
  - Does Microsoft publish guidance for that service or service family?
  - Does the app implementation target that same service surface?

For the repeatable workflow and evidence requirements, see [notes/service-proof-validation.md](./notes/service-proof-validation.md).

## Validation Proof Gate

From schema version `1.1` onward, every new `validated` or `published` record must carry a machine-checkable `validation_proof` block:

```json
{
  "validation_proof": {
    "source_url": "https://...",
    "exact_quote_or_path": "The exact phrase, setting name, registry path, or control path captured on the source page",
    "key_found_on_page": true
  }
}
```

Rules:

- If `key_found_on_page` is `false`, the record must not be promoted to `validated`.
- `key_found_on_page: false` means the record stays research-only and should be treated as `docs-first`.
- `source_url` must point to the exact Microsoft or vendor page used for the validation step.
- `exact_quote_or_path` must capture the literal page phrase or control path that justified the validation.
- Old `1.0` records may still exist without this block, but new validation work should move to `1.1`.
- A "verified source URL" pool is useful for faster research, but a URL alone is not record-level proof until the exact quote or path has been captured for that specific record.
- A verified URL can still be legacy, version-scoped, or incomplete for the current Windows release; if so, keep `decision.needs_vm_validation = true` or leave the record in research-only status until runtime behavior is confirmed.
- If a primary Microsoft source explicitly says that newer Windows versions do not need or do not use the change, it is reasonable to keep `decision.needs_vm_validation = false` and classify the record as a documented legacy or no-op setting instead of treating VM testing as required.

### Service-Type Validation Proof

Service records may use a checked-in local Service Control Manager snapshot as the machine-checkable `validation_proof` source when a web page alone is not exact enough.

Allowed pattern:

- `validation_proof.source_url`: path to a checked-in service snapshot or transcript captured during review
- `validation_proof.exact_quote_or_path`: exact service identifier plus the captured startup-type or service-configuration lines
- `validation_proof.key_found_on_page`: `true` only when the exact service name appears in that captured source artifact

Required companion evidence:

- an official Microsoft source that gives service guidance, service purpose, or supportability context
- repo-code evidence showing that the app targets the same service name

Practical interpretation:

- the local snapshot proves the exact service control surface exists on the reviewed build
- the Microsoft source proves the service is real and gives the safety context for disabling it
- the app evidence proves the implementation surface matches

This is still not enough when Microsoft guidance explicitly says `Don't disable`, `No guidance`, or otherwise conflicts with a safe user-facing recommendation. In that case the record may stay `review-required` even when the service identity itself is proven.

## Apply Gate

Normal user-facing Apply should stay disabled unless all of the following are true:

- `record_status` is `validated` or `published`
- `decision.apply_allowed` is `true`
- evidence is strong enough to explain both benefits and tradeoffs
- the target path and value mapping are not under dispute
- rollback can restore both `previous` and `default` semantics

If there is a path mismatch, unclear value meaning, or version ambiguity that changes behavior, the record should stay `review-required`.

## Record Lifecycle

- `draft`: Started, incomplete, not ready for product decisions
- `review-required`: Significant contradiction, missing mapping, or insufficient confidence
- `validated`: Good enough for product and UX planning, with clear source links
- `published`: Stable and ready for wider documentation reuse
- `deprecated`: Kept for history, no longer recommended

## Folder Layout

- `setting-research.schema.json`: JSON schema for machine-readable research records
- `records/`: individual setting research records
- `notes/`: incoming research leads, source reminders, and validation hints that are not yet promoted into record-level proof

## Recommended Workflow

1. Start from the current app implementation and capture what it writes today.
2. Add the authoritative target from official docs if available.
3. Add the Windows default or default policy state.
4. List the plausible values and explain them in casual language.
5. Add scenario-based recommendations instead of a single magic value.
6. Fill `validation_proof` with the exact source URL and page phrase or path when the record is moving toward `validated`.
7. Attach evidence with source strength.
8. Decide whether Apply is allowed.
9. Leave explicit open questions when something is still uncertain.

## Design Note: Default vs Previous

The app should eventually expose both of these actions when the underlying setting supports them:

- `Restore previous`: return to the exact runtime snapshot captured before Apply
- `Restore default`: return to the Windows baseline state for the current version and scope

This research system exists so those two actions can be described and implemented without guessing.
