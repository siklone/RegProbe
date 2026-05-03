# Kernel/Power 96 Path-Aware Follow-Up Queue (2026-03-30)

The generic residual value-exists queue is no longer the right entry point for the final two path-sensitive candidates.

## Candidate queue

- `policy.system.enable-virtualization`
  - current blocker: generic string search collides with `EnableVirtualizationBasedSecurity`
  - required next lane: exact-path static routing for `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\SYSTEM\EnableVirtualization`, then a policy-runtime follow-up

- `system.io-allow-remote-dasd`
  - current blocker: prior static routing collided with the removable-storage policy path
  - required next lane: exact-path static / Ghidra routing for `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\I/O System\AllowRemoteDASD`, then a Session Manager I/O runtime lane

## Execution note

These two values should stay out of the generic residual string-first queue until the substring and path-collision risks are removed.

## Audit artifact

- [kernel-power-96-path-aware-follow-up-20260330.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/kernel-power-96-path-aware-follow-up-20260330.json)
