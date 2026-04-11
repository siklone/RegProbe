# VM Storage Incident - 2026-04-02

The current `Win25H2Clean` runtime lane is blocked by host storage instability, not by a guest parser bug.

Observed host facts:

- VMware surfaced a disk error for `H:\Yedek\VMs\Win25H2Clean\Win25H2-target.vmdk`
- `vmware.log` recorded `errCode=1117` for the same VMDK path
- host reads against adjacent VMware log files also returned `The request could not be performed because of an I/O device error`
- System event logs contained recent `disk` retry events and `Ntfs` I/O failure events for `H:`
- the backing disk for `H:` was reported as USB-attached storage

Implication:

- runtime evidence collected from the current `H:\Yedek\VMs\Win25H2Clean` path is not trustworthy until the storage path is repaired or the VM is migrated
- the mega-trigger pilot should fail fast with `storage-unsafe` instead of being misread as a parser-only failure

Decision:

1. do not resume live runtime collection on the current `H:` VM path
2. gate runtime runs through storage preflight first
3. migrate the VM to healthy local storage before the next live mega-trigger rerun

Follow-up:

- added `scripts/vm/test-vm-storage-health.ps1`
- added `registry-research-framework/tools/run-power-control-batch-mega-trigger-runtime-safe.ps1`
- preserved the guest ETL path-discovery patch so the next healthy rerun starts from the improved parser lane
