# VMware Full ACPI VMX Follow-Up (2026-03-30)

This experiment tested whether a small VMX-level ACPI widening pass could unlock extra sleep states on the supported VMware baseline.

Baseline:
- VM: `Win25H2Clean`
- base snapshot: `RegProbe-Baseline-ToolsHardened-20260330`

Applied VMX keys:
- `firmware = "efi"` (already present)
- `monitor.virtual_exec = "hardware"`
- `isolation.tools.hibernate.disable = "FALSE"`
- `acpi.smbiosVersion = "2.4"`
- `gui.runVMWorkstation = "TRUE"`

Observed result:
- before the experiment, `powercfg /a` exposed only `Standby (S1)`
- after the experiment, `powercfg /a` still exposed only `Standby (S1)`
- `Standby (S3)` remained unavailable
- `Hibernate` remained unavailable
- `Standby (S0 Low Power Idle)` remained unavailable

Artifacts:
- `registry-research-framework/audit/regprobe-full-acpi-vmx-20260330.json`
- `registry-research-framework/audit/regprobe-full-acpi-vmx-20260330.before.txt`
- `registry-research-framework/audit/regprobe-full-acpi-vmx-20260330.after.txt`

Project decision:
- do not create `RegProbe-Baseline-FullACPI-20260330` as a new canonical snapshot
- keep `RegProbe-Baseline-ToolsHardened-20260330` as the only canonical baseline
- keep the remaining power-state-limited `B` records as environment-limited on the current VMware platform
