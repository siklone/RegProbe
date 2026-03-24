# Microsoft Defender Sample Submission

Record: `security.disable-defender-sample-submission`

This record uses:

- `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet`
- value: `SubmitSamplesConsent`

## Source check

Microsoft documents this policy directly in the Defender Policy CSP:

- path: `Windows Components > Microsoft Defender Antivirus > MAPS`
- registry key: `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet`
- value: `SubmitSamplesConsent`
- documented values:
  - `0 = Always prompt`
  - `1 = Send safe samples automatically`
  - `2 = Never send`
  - `3 = Send all samples automatically`

Microsoft also documents the tradeoff in the Block at First Sight notes: if sample submission is set to `2`, Block at First Sight will not function.

## VM proof

Snapshot:

- `baseline-20260324-high-risk-lane`

Artifacts:

- state `2`:
  - `H:\Temp\vm-tooling-staging\submitsamples-ui-state2.txt`
- absent-value check on the same branch:
  - `H:\Temp\vm-tooling-staging\spynet-ui-state2.txt`

The state-`2` run showed a direct policy-path read:

```text
SecurityHealthService.exe | RegQueryValue | HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet\SubmitSamplesConsent | SUCCESS | Type: REG_DWORD, Length: 4, Data: 2
```

The same run also showed the sibling `SpyNetReporting` value as absent, which helps prove that this probe was not just reading a fully populated branch:

```text
SecurityHealthService.exe | RegQueryValue | HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet\SpyNetReporting | NAME NOT FOUND | Length: 16
```

In the separate `SpyNetReporting = 2` run on the same clean snapshot, `SubmitSamplesConsent` was still `NAME NOT FOUND`, which gives us the missing-value baseline for this policy branch.

## Why this stays Class B

This record is strong enough to show and to map in the app, but it is not ready for one-click apply.

- The path and value model are documented.
- The runtime read for value `2` is direct and clean.
- Value `2` lowers Defender cloud protection.
- Microsoft documents that Block at First Sight will not function with this value.

So the current state is:

- validated
- app-mapped
- research-gated
- `Class B`
