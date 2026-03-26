# Hide Defender Exclusions From Local Admins

Record: `security.hide-defender-exclusions-from-local-admins`

This record uses:

- `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender`
- value: `HideExclusionsFromLocalAdmins`

## Source check

Microsoft documents this value in the Defender CSP:

- `0` or not configured = local admins can view exclusions
- `1` = local admins cannot view exclusions

Microsoft's Defender exclusions doc also says this setting changes visibility. It does not remove the managed exclusions themselves.

The local dump material adds one more detail for current builds:

- `Docs/security/assets/Windows-Defender.txt` shows `HideExclusionsFromLocalAdmins` on both:
  - `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender`
  - `HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Policy Manager`

## VM proof

Snapshot:

- `baseline-20260324-high-risk-lane`

Artifacts:

- baseline:
  - `research/evidence-files/vm-tooling-staging/hideexclusions-admins-baseline-1-20260325-001524/hideexclusions-admins-baseline-visibility.json`
  - `research/evidence-files/vm-tooling-staging/hideexclusions-admins-baseline-1-20260325-001524/hideexclusions-admins-baseline.txt`
- root path `1`:
  - `research/evidence-files/vm-tooling-staging/hideexclusions-admins-root-1-20260325-002348/hideexclusions-admins-root-1-visibility.json`
  - `research/evidence-files/procmon/security-hide-defender-exclusions-from-local-admins-validation-20260325/hideexclusions-admins-root-1.txt`
- Policy Manager alias `1`:
  - `research/evidence-files/vm-tooling-staging/hideexclusions-admins-policymanager-1-20260325-002004/hideexclusions-admins-policymanager-1-visibility.json`
  - `research/evidence-files/procmon/security-hide-defender-exclusions-from-local-admins-validation-20260325/hideexclusions-admins-policymanager-1.txt`

The baseline run created a managed exclusion under the policy exclusions branch and left both `HideExclusionsFromLocalAdmins` surfaces unset. In that state, `Get-MpPreference` still showed the exclusion:

```json
{
  "exclusion_visible_in_get_mppreference": true,
  "get_mppreference_exclusion_paths": [
    "C://Temp//CodexExclusionProbe"
  ],
  "root_policy_value": null,
  "policy_manager_value": null
}
```

When the root policy path was set to `1`, the runtime read was direct:

```text
wmiprvse.exe | RegQueryValue | HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/HideExclusionsFromLocalAdmins | SUCCESS | Type: REG_DWORD, Length: 4, Data: 1
powershell.exe | RegQueryValue | HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/HideExclusionsFromLocalAdmins | SUCCESS | Type: REG_DWORD, Length: 4, Data: 1
```

And the visibility probe flipped:

```json
{
  "exclusion_visible_in_get_mppreference": false,
  "get_mppreference_exclusion_paths": [
    "N/A: Administrators are not allowed to view exclusions"
  ],
  "root_policy_value": 1,
  "policy_manager_value": null,
  "managed_registry_query_contains_exclusion": true
}
```

The Policy Manager run showed a current-build alias:

```text
wmiprvse.exe | RegQueryValue | HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/HideExclusionsFromLocalAdmins | NAME NOT FOUND | Length: 16
wmiprvse.exe | RegQueryValue | HKLM/SOFTWARE/Policies/Microsoft/Windows Defender/Policy Manager/HideExclusionsFromLocalAdmins | SUCCESS | Type: REG_DWORD, Length: 4, Data: 1
```

That run produced the same hidden-exclusions behavior in `Get-MpPreference`.

## What this means

The evidence is strong for the root path and strong enough to keep the Policy Manager path as a current 25H2 alias.

What we can say with confidence:

- the value model is documented
- the root path works
- the Policy Manager alias also works on this 25H2 VM
- the behavior is visibility-only
- the managed exclusions branch stays populated

## Why this stays Class B

This record is strong enough to show and map in the app, but it still stays gated.

- Microsoft documents the behavior clearly.
- The root path has direct runtime reads.
- The current 25H2 build also honors a Policy Manager alias.
- The app should stay on the documented root path until we know more about when the alias matters.

This is not a benchmark candidate. It changes exclusion visibility, not Defender scan performance or system scheduling.
