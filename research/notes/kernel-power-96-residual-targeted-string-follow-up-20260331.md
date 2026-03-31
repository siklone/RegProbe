# Kernel/Power 96 Residual Targeted String Follow-Up

Date: 2026-03-31

## Goal

The residual broad-batch hold still had three low-signal value-exists candidates with no exact hit in the earlier `ntoskrnl.exe` / `powrprof.dll` pass:

- `power.customize-during-setup`
- `power.source-settings-version`
- `power.session-power-setting-profile`

This follow-up asked a narrower question before paying for any new runtime lane:

- do these names appear in the most plausible early-boot or service-host readers?

## New targeted string passes

Power-setting-profile lane:

- `evidence/files/vm-tooling-staging/power-setting-profile-targeted-string-primary-20260331-133427/summary.json`
- `evidence/files/vm-tooling-staging/power-setting-profile-targeted-string-primary-20260331-133427/results.json`

Bootstrap pair lanes:

- `evidence/files/vm-tooling-staging/power-customize-during-setup-targeted-string-primary-20260331-133521/summary.json`
- `evidence/files/vm-tooling-staging/power-customize-during-setup-targeted-string-primary-20260331-133521/results.json`
- `evidence/files/vm-tooling-staging/power-source-settings-version-targeted-string-primary-20260331-133521/summary.json`
- `evidence/files/vm-tooling-staging/power-source-settings-version-targeted-string-primary-20260331-133521/results.json`

New binaries checked:

- `smss.exe`
- `svchost.exe`
- `services.exe`
- `powrprof.dll`
- `winload.exe` for the two bootstrap-style values

## Result

All three stayed exact `no-hit`.

- `PowerSettingProfile`
  - earlier no-hit in `ntoskrnl.exe` and `powrprof.dll`
  - new no-hit in `smss.exe`, `svchost.exe`, `services.exe`, and `powrprof.dll`
- `CustomizeDuringSetup`
  - earlier no-hit in `ntoskrnl.exe` and `powrprof.dll`
  - new no-hit in `smss.exe`, `svchost.exe`, `services.exe`, `powrprof.dll`, and `winload.exe`
- `SourceSettingsVersion`
  - earlier no-hit in `ntoskrnl.exe` and `powrprof.dll`
  - new no-hit in `smss.exe`, `svchost.exe`, `services.exe`, `powrprof.dll`, and `winload.exe`

## Interpretation

This does not prove that the values are dead flags.

It does make one routing decision clearer:

- these three are no longer strong next-lane candidates for a fresh runtime or Ghidra spend on the current VMware baseline

The most important clarification is `PowerSettingProfile`:

- it is still real as a baseline value under `Session Manager\Power`
- it still appears as adjacent context in the watchdog baseline export
- but it now lacks exact string support in both the first kernel-oriented pass and the narrower boot/service-host pass
- that makes a new dedicated runtime lane low-yield for now

## Project decision

Move all three into a lower-priority residual hold:

- `power.customize-during-setup`
- `power.source-settings-version`
- `power.session-power-setting-profile`

Keep them out of the strongest immediate queue until either:

- a broader binary family hypothesis emerges
- or an outside docs/static lead appears that justifies reopening the lane
