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

The same page also notes that on modern Windows builds value `1` or `2` can both land in advanced membership behavior. The app only exposes the documented advanced value `2`, so that edge does not block the current record.

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

## Class result

This record is now app-ready for the documented `SpyNetReporting = 2` path.

- The path and value are documented.
- The runtime read for value `2` is direct and clean.
- The app only writes the documented advanced-membership value `2`.
- The `1` versus `2` split stays as background context, not a blocker.

So the current state is:

- validated
- app-mapped
- actionable
- `Class A`
