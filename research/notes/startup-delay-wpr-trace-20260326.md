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

- [startup-delay-wpr-summary.json](../evidence-files/vm-tooling-staging/startup-delay-wpr-20260326-024701/startup-delay-wpr-summary.json)
- [startup-delay-missing.summary.json](../evidence-files/vm-tooling-staging/startup-delay-wpr-20260326-024701/missing/startup-delay-missing.summary.json)
- [startup-delay-0.summary.json](../evidence-files/vm-tooling-staging/startup-delay-wpr-20260326-024701/0/startup-delay-0.summary.json)
- [startup-delay-missing.etl.md](../evidence-files/vm-tooling-staging/startup-delay-wpr-20260326-024701/missing/startup-delay-missing.etl.md)
- [startup-delay-0.etl.md](../evidence-files/vm-tooling-staging/startup-delay-wpr-20260326-024701/0/startup-delay-0.etl.md)

Takeaway:

The startup-delay record now has runtime diff, Procmon, Ghidra, and WPR coverage on the current VM. It should stay research-gated because the shell contract is still undocumented, not because the runtime lane is missing.
