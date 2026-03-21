# Nohuto Ghidra / Dump Semantics

This note captures a few value semantics that can be justified from the local nohuto mirrors:

| Value | What it does | Source shape | Confidence |
| --- | --- | --- | --- |
| `HiberbootEnabled` | `0` disables Fast Startup; `1` enables it. The policy path overrides the local path when present. | `decompiled-pseudocode` + boot registry record | high |
| `AllowCrashDump` | Nonzero allows crash-dump initialization and secure dump provisioning; `0` blocks the crash-dump path. | `decompiled-pseudocode` + boot registry record | high |
| `EnableCpuQuota` | Nonzero appears to enable DFSS CPU quota configuration and the associated group-scheduling refresh path. | `decompiled-pseudocode` + Session Manager trace | medium |

## Evidence Snippets

```text
PopReadHiberbootPolicy.c / PopReadHiberbootGroupPolicy.c
- Reads HiberbootEnabled from Session Manager\Power.
- Policy path under Software\Policies\Microsoft\Windows\System overrides the local path.

IopInitializeDumpPolicySettings.c / IopCrashDumpPolicyChangeWnfCallback.c / IoInitializeCrashDump.c
- Reads AllowCrashDump from CrashControl.
- Nonzero value allows crash-dump initialization; zero blocks it.

PspIsDfssEnabled.c / PspReadDfssConfigurationValues.c
- Reads EnableCpuQuota.
- Nonzero value is used as the DFSS quota configuration gate.

CrashControl.txt
- `AllowCrashDump`
- `AutoReboot`
- `CrashDumpEnabled`
- These are adjacent CrashControl values, but they are not interchangeable.

Session-Manager.txt
- `DefaultDynamicHeteroCpuPolicy`
- `DefaultHeteroCpuPolicy`
- `DynamicHeteroCpuPolicyExpectedRuntime`
- `EnableCpuQuota`
- This trace shows `EnableCpuQuota` as a live Session Manager quota-system value, separate from the heterogeneous CPU policy values around it.
```

## Practical Use

- Use `HiberbootEnabled` as a Ghidra-backed support layer for Fast Startup records.
- Treat `AllowCrashDump` as the lower-level crash-dump gate, not as a synonym for dump-limiting policy values.
- Treat `EnableCpuQuota` as a tentative DFSS lead until a dedicated record is promoted.
- Do not conflate `AllowCrashDump` with `AutoReboot` or `CrashDumpEnabled`; they sit in the same CrashControl area but control different behaviors.
