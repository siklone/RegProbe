# Service-Type Validation Proof

Use this note for records whose primary control surface is a Windows service, driver service, or other Service Control Manager entry rather than an ADMX or CSP-backed policy.

This is a methodology note for service-type records. It does not weaken the normal proof gate.

## Why Service Records Are Different

ADMX-backed policy records usually let us point to:

- an exact registry path
- an exact value name
- an explicit `enabledValue` and `disabledValue`

Service-disable records usually do not have that shape.

Instead, we often have:

- a local SCM identity such as `WerSvc` or `PrintWorkflowUserSvc`
- a startup type and service configuration visible through `sc qc`, `Get-Service`, or `Win32_Service`
- Microsoft guidance that is descriptive or category-based rather than a registry mapping

This means the validation question is different:

- not "did we find the exact policy value"
- but "did we prove the exact service surface, the implementation match, and the Microsoft guidance context"

## Minimum Evidence Stack

To move a service-type record toward `validated`, capture all of the following:

1. Local SCM proof
   Exact service identity and startup configuration from the reviewed build.
2. Official Microsoft guidance
   Service guidance, service purpose, or supportability context from Microsoft.
3. App implementation proof
   Repo evidence that the app targets the same service name or start-mode surface.

If any of those three are missing, keep the record in `review-required`.

## Acceptable Local SCM Sources

Use one or more of these as the machine-checkable identity source:

- `sc qc <service-name>`
- `Get-Service -Name <service-name>`
- `Get-CimInstance Win32_Service -Filter \"Name='<service-name>'\"`

The goal is not just to prove that a similar service exists. The goal is to prove the exact identifier the app acts on.

## How To Fill `validation_proof`

For service-type records, `validation_proof` may point to a checked-in local service snapshot or transcript.

Suggested pattern:

```json
{
  "validation_proof": {
    "source_url": "Docs/tweaks/research/notes/service-snapshots/wersvc-sc-qc-2026-03-14.txt",
    "exact_quote_or_path": "SERVICE_NAME: WerSvc | START_TYPE : 3 DEMAND_START | DISPLAY_NAME: Windows Error Reporting Service",
    "key_found_on_page": true,
    "notes": "Local SCM snapshot captured on Windows 11 Pro 10.0.x during the review pass."
  }
}
```

Interpretation:

- `source_url` is the checked-in snapshot path rather than a public web page
- `exact_quote_or_path` captures the exact service identifier and relevant configuration lines
- `key_found_on_page` means the exact service identifier was found in the cited source artifact

Do not point `validation_proof` at a Microsoft page unless that page really shows the exact service identifier used by the record.

## How Microsoft Guidance Fits In

The local snapshot proves the service surface. It does not prove that disabling the service is safe.

That is what the Microsoft guidance evidence is for.

Examples of useful guidance:

- Microsoft says the service is `OK to disable`
- Microsoft says the service is `No guidance`
- Microsoft says `Don't disable`

These guidance classes are not interchangeable.

Practical interpretation:

- `OK to disable`: can support `validated` if the rest of the record is clean
- `No guidance`: usually keep `review-required` or at least `apply_allowed = false`
- `Don't disable`: do not treat the record as a safe disable recommendation

## What This Method Does Not Prove

A service snapshot plus Microsoft guidance still does not prove:

- that the service is safe to disable on every Windows edition and role
- that no dependency or side effect exists on the reviewed build
- that a startup-type change is equivalent to a higher-level feature policy

If the record is risk-sensitive or version-sensitive, keep `decision.needs_vm_validation = true` or keep the record in `review-required`.

## When A Service Record Still Stays `review-required`

Keep the record in `review-required` when any of the following is true:

- Microsoft guidance conflicts with disabling the service
- Microsoft gives only category-level guidance and we cannot responsibly map it to the exact service
- the app targets a different service name or a broader bundle than the record documents
- the service is only one layer of a bigger feature shutdown bundle

## Batch Workflow For Service Records

Use this sequence for service clusters:

1. Capture the local SCM snapshot for the exact service name.
2. Confirm the app targets the same service identifier.
3. Attach the Microsoft guidance evidence.
4. Decide whether the record is:
   - `validated`
   - `validated` but `apply_allowed = false`
   - or still `review-required`

Do not promote service records by analogy from other service records. Each one still needs its own identity proof and guidance context.
