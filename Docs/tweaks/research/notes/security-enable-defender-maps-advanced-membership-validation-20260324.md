# Microsoft Defender MAPS Membership

Record: `security.enable-defender-maps-advanced-membership`

This record uses:

- `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet`
- value: `SpyNetReporting`

## Source check

Microsoft documents this policy on the Defender Spynet path:

- source: `Policy CSP - ADMX_MicrosoftDefenderAntivirus`
- path: `Windows Components > Microsoft Defender Antivirus > MAPS`
- registry key: `HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet`
- value: `SpyNetReporting`
- documented values:
  - `0 = disabled`
  - `1 = basic membership`
  - `2 = advanced membership`

The same page also notes that on modern Windows builds value `1` or `2` can both land in advanced membership behavior. That is why this record stays gated even though value `2` is clean.

## VM proof

Snapshot:

- `baseline-20260324-high-risk-lane`

Artifacts:

- baseline:
  - `H:\Temp\vm-tooling-staging\spynet-ui-baseline.txt`
- state `2`:
  - `H:\Temp\vm-tooling-staging\spynet-ui-state2.txt`

Baseline showed the policy branch absent and the live non-policy store reading `2`:

```text
SecurityHealthService.exe | RegOpenKey | HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\SpyNet | NAME NOT FOUND | Desired Access: Read
SecurityHealthService.exe | RegQueryValue | HKLM\SOFTWARE\Microsoft\Windows Defender\Spynet\SpyNetReporting | SUCCESS | Type: REG_DWORD, Length: 4, Data: 2
```

After writing the policy value, the same service read `2` from the policy path directly:

```text
SecurityHealthService.exe | RegQueryValue | HKLM\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet\SpyNetReporting | SUCCESS | Type: REG_DWORD, Length: 4, Data: 2
```

The same state-`2` trace also showed the sibling `SubmitSamplesConsent` value still absent on the branch, which keeps this probe narrow.

## Why this stays Class B

This record is strong enough to show and to map in the app, but it is not ready for one-click apply.

- The path and value are documented.
- The runtime read for value `2` is direct and clean.
- The `1` versus `2` split is still not clear enough on modern builds.
- MAPS membership also interacts with other Defender cloud policies on the same branch.

So the current state is:

- validated
- app-mapped
- research-gated
- `Class B`
