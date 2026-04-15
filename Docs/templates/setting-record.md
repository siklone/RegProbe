# Setting: <SettingName>

**Verdict:** <Recommended | Experimental | Research-only | Blocked | Archived>

## Plain-English Summary

State in one to three sentences what the setting is believed to control without overstating confidence.

Example:

This setting is commonly associated with <feature or behavior>. Current evidence suggests it may affect <scope>, but the present record does not yet prove consistent runtime behavior across all supported Windows builds.

## Quick Status

| Field | Value |
|---|---|
| Category | <Networking / Graphics / Input / Privacy / etc.> |
| Scope | <User / Machine / Service / Policy> |
| Registry path | `<HKLM\\...>` |
| Value name | `<ValueName>` |
| Value type | `<REG_DWORD / REG_SZ / etc.>` |
| Current app status | <Recommended / Experimental / Research-only / Blocked / Archived> |
| Risk level | <Low / Medium / High> |
| Rollback defined | <Yes / No / Partial> |
| Rollback tested | <Yes / No / Partial> |
| Build coverage | <Validated builds> |
| Last reviewed | <YYYY-MM-DD> |

## Evidence Snapshot

| Type | Status | Notes |
|---|---|---|
| Docs | <Yes / No / Partial> | <What was found> |
| Policy | <Yes / No / Partial> | <ADMX / CSP / control surface notes> |
| VM validation | <Yes / No / Partial> | <Before/after or reproduction notes> |
| Trace | <Yes / No / Partial> | <Procmon / ETW / WPR notes> |
| Reverse engineering | <Yes / No / Partial> | <Binary, symbol, or reference notes> |
| Rollback | <Yes / No / Partial> | <Restore path notes> |

**Badge summary**  
`Docs <✓/~/✗>  Policy <✓/~/✗>  VM <✓/~/✗>  Trace <✓/~/✗>  RE <✓/~/✗>  Rollback <✓/~/✗>`

## Proof Ladder

`Docs -> Policy -> VM diff -> Registry observation -> Trace -> RE -> Recommendation`

**Current strongest rung reached:** <value>  
**Missing rung(s):** <value>

## What We Observed

- <Observation 1>
- <Observation 2>
- <Observation 3>

Keep this section factual. Use direct observations, not conclusions.

## What This Proves

State only what the current evidence actually establishes.

Examples:

- The registry value exists as a real configurable surface.
- The control surface is legitimate.
- The setting can be changed and restored in a controlled way.
- Runtime reads were observed under specific conditions.

## What This Does Not Prove

State the remaining uncertainty clearly.

Examples:

- It does not yet prove the claimed performance benefit.
- It does not prove stable runtime effect across current builds.
- It does not prove safe interaction with neighboring settings.
- It does not prove equivalence between policy exposure and runtime enforcement.

## Why This Is <Blocked / Research-only / Experimental / Recommended>

- <Reason 1>
- <Reason 2>
- <Reason 3>

This section should connect evidence quality to the current shipping decision.

## Safe Interpretation

Explain how maintainers and users should interpret the current record.

Examples:

- Keep in research, do not expose in standard apply flow.
- Expose as recommended because control and runtime proof are both strong.
- Expose as experimental because behavior is promising but cross-build coverage is still incomplete.

## Rollback Behavior

| Action | Result |
|---|---|
| Previous value captured | <Yes / No / Partial> |
| Value delete supported | <Yes / No / Partial> |
| Restoration path defined | <Yes / No / Partial> |
| Restoration tested | <Yes / No / Partial> |
| Reboot required for restore | <Yes / No / Unknown> |

## Tested Environments

| Build | Environment | Result |
|---|---|---|
| <Build> | <VM / Host / Both> | <Outcome> |
| <Build> | <VM / Host / Both> | <Outcome> |

## Interactions And Caveats

- <Interaction with other settings>
- <Policy precedence caveat>
- <Build-specific caveat>
- <Service restart or reboot dependency>

## Artifacts

| Artifact | Purpose |
|---|---|
| `<file1>` | <Why it exists> |
| `<file2>` | <Why it exists> |
| `<file3>` | <Why it exists> |

Optional:

- Capture date: `<YYYY-MM-DD>`
- Analyst: `<name or handle>`
- Hash manifest: `<path>`

## Sources

- <Primary source 1>
- <Primary source 2>
- <Artifact set>
- <Internal notes>

## Final Maintainer Decision

**Decision:** <Recommended | Experimental | Research-only | Blocked | Archived>

One short paragraph summarizing why this is the correct current disposition.
