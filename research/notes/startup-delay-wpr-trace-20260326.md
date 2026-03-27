# Startup Delay WPR Trace - 2026-03-26

This pass adds a bounded WPR lane for `StartupDelayInMSec` on `Win25H2Clean`.

States exercised:

- `missing`
- `0`

What the VM did:

- applied the requested state on `HKCU/Software/Microsoft/Windows/CurrentVersion/Explorer/Serialize/StartupDelayInMSec`
- started `wpr -start GeneralProfile -filemode`
- restarted `explorer.exe`
- waited for the shell to settle
- stopped WPR and copied the summaries back to the host
- restored the original missing baseline

Results:

- `missing`:
  - before: missing
  - applied: missing
  - restored: missing
  - Explorer restart window: `25.04s`
  - shell after trace: `explorer=true`, `sihost=true`, `ShellHost=true`, `ctfmon=true`
- `0`:
  - before: missing
  - applied: `0`
  - restored: missing
  - Explorer restart window: `22.43s`
  - shell after trace: `explorer=true`, `sihost=true`, `ShellHost=true`, `ctfmon=true`

Artifacts:

- [startup-delay-wpr-summary.json](../../evidence/files/vm-tooling-staging/startup-delay-wpr-20260326-024701/startup-delay-wpr-summary.json)
- [startup-delay-missing.summary.json](../../evidence/files/vm-tooling-staging/startup-delay-wpr-20260326-024701/missing/startup-delay-missing.summary.json)
- [startup-delay-0.summary.json](../../evidence/files/vm-tooling-staging/startup-delay-wpr-20260326-024701/0/startup-delay-0.summary.json)
- [startup-delay-missing.etl.md](../../evidence/files/vm-tooling-staging/startup-delay-wpr-20260326-024701/missing/startup-delay-missing.etl.md)
- [startup-delay-0.etl.md](../../evidence/files/vm-tooling-staging/startup-delay-wpr-20260326-024701/0/startup-delay-0.etl.md)

Related supporting artifacts:

- [vm-batch-probe-20260320.json](../../evidence/files/vm-tooling-staging/vm-batch-probe-20260320.json)
- [procmon-startup-delay.pml.md](../../evidence/files/procmon/system.disable-startup-delay/procmon-startup-delay.pml.md)
- [ghidra_explorer_serialize.txt](../../evidence/files/ghidra/system.disable-startup-delay/ghidra_explorer_serialize.txt)

v3.1 alignment:

This note supplies the `behavior_wpr` layer only. The record's retained `A` classification under the current v3.1 contract still depends on the checked-in reversible probe, the repo-friendly Procmon placeholder for the original shell-restart capture, and the Explorer Ghidra string-search output.

Takeaway:

The startup-delay record is not on the official-policy lane because Microsoft still does not publish StartupDelayInMSec as a supported registry contract. Under the current v3.1 rules, though, the full record can still remain `A` on the converged-vm lane because the reversible probe, Procmon reference, Ghidra string support, and bounded WPR lane all point at the same current-build behavior.
