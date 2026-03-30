# AppCompat Policy Procmon Validation (2026-03-26)

This pass replaced the old host-only AppCompat Procmon placeholders with VM-side evidence from `Win25H2Clean`.

What ran:

- `privacy.disable-appcompat-engine.policy`
  - wrote `DisableEngine=1` and `SbEnable=0`
- `privacy.disable-switchback.policy`
  - reused the same bundle capture and exported a SwitchBack-only filtered hits file
- `privacy.disable-appdeviceinventory.policy`
  - wrote `DisableAPISamping=1`
  - wrote `DisableApplicationFootprint=1`
  - wrote `DisableInstallTracing=1`
  - wrote `DisableWin32AppBackup=1`
- `privacy.disable-program-compatibility-assistant`
  - wrote `DisablePCA=1`

What Procmon showed:

- `powershell.exe` created or opened `HKLM\Software\Policies\Microsoft\Windows\AppCompat`
- `powershell.exe` wrote the expected `REG_DWORD` values
- `reg.exe` queried the same values and read back the expected data
- each probe restored the original missing value state after capture

Checked-in evidence:

- [privacy.disable-appcompat-engine.policy raw Procmon files](../../evidence/files/procmon/privacy.disable-appcompat-engine.policy)
- [privacy.disable-switchback.policy filtered hits](../../evidence/files/procmon/privacy.disable-switchback.policy)
- [privacy.disable-appdeviceinventory.policy raw Procmon files](../../evidence/files/procmon/privacy.disable-appdeviceinventory.policy)
- [privacy.disable-program-compatibility-assistant raw Procmon files](../../evidence/files/procmon/privacy.disable-program-compatibility-assistant)

Key lines:

- `DisableEngine=1`
- `SbEnable=0`
- `DisableAPISamping=1`
- `DisableApplicationFootprint=1`
- `DisableInstallTracing=1`
- `DisableWin32AppBackup=1`
- `DisablePCA=1`

This is runtime proof for the exact registry path and exact values. The official Microsoft ADMX and CSP sources still carry the semantics.
