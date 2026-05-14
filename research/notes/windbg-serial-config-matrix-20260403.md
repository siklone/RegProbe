# WinDbg Serial Config Matrix

Date: `2026-04-03`

## Summary

- executed profiles: `4`
- kernel-connected profiles: `4`
- transport-error profiles: `1`
- no-debuggee profiles: `0`

## Profiles

- `guest-restart-kd-bonc-rxloss-true` -> status `runner-ok`, transport `transport_ok`, score `4`, kernel_connected `True`, rxloss `TRUE`, bonc `bonc`
- `guest-restart-kd-none-rxloss-true` -> status `trace-error`, transport `transport_error`, score `3`, kernel_connected `True`, rxloss `TRUE`, bonc `none`
- `guest-restart-kd-bonc-rxloss-false` -> status `runner-ok`, transport `transport_ok`, score `4`, kernel_connected `True`, rxloss `FALSE`, bonc `bonc`
- `guest-restart-kd-none-rxloss-false` -> status `runner-ok`, transport `transport_ok`, score `4`, kernel_connected `True`, rxloss `FALSE`, bonc `none`

## Follow-Up

- keep this phase focused on transport only
- do not widen to more keys until a guest-restart or cold-boot serial variant is reproducibly usable
- if these four variants stay weak, move next to pipe endpoint and attach/start-order combinations

## Retained audit bundle

- Matrix:
  - [windbg-serial-config-matrix-20260403.json](../../registry-research-framework/audit/windbg-serial-config-matrix-20260403.json)
  - [windbg-serial-config-execution-20260403.json](../../registry-research-framework/audit/windbg-serial-config-execution-20260403.json)
  - [windbg-serial-config-recovery-20260403.json](../../registry-research-framework/audit/windbg-serial-config-recovery-20260403.json)
  - [windbg-serial-config-variants-20260403.json](../../registry-research-framework/audit/windbg-serial-config-variants-20260403.json)
- Prepare bundles:
  - [windbg-serial-config-prepare-guest-restart-kd-bonc-rxloss-false-20260403.json](../../registry-research-framework/audit/windbg-serial-config-prepare-guest-restart-kd-bonc-rxloss-false-20260403.json)
  - [windbg-serial-config-prepare-guest-restart-kd-bonc-rxloss-true-20260403.json](../../registry-research-framework/audit/windbg-serial-config-prepare-guest-restart-kd-bonc-rxloss-true-20260403.json)
  - [windbg-serial-config-prepare-guest-restart-kd-none-rxloss-false-20260403.json](../../registry-research-framework/audit/windbg-serial-config-prepare-guest-restart-kd-none-rxloss-false-20260403.json)
  - [windbg-serial-config-prepare-guest-restart-kd-none-rxloss-true-20260403.json](../../registry-research-framework/audit/windbg-serial-config-prepare-guest-restart-kd-none-rxloss-true-20260403.json)
- Executed profile bundles:
  - [windbg-serial-config-bundle-guest-restart-kd-bonc-rxloss-false-20260403.json](../../registry-research-framework/audit/windbg-serial-config-bundle-guest-restart-kd-bonc-rxloss-false-20260403.json)
  - [windbg-serial-config-bundle-guest-restart-kd-bonc-rxloss-true-20260403.json](../../registry-research-framework/audit/windbg-serial-config-bundle-guest-restart-kd-bonc-rxloss-true-20260403.json)
  - [windbg-serial-config-bundle-guest-restart-kd-none-rxloss-false-20260403.json](../../registry-research-framework/audit/windbg-serial-config-bundle-guest-restart-kd-none-rxloss-false-20260403.json)
  - [windbg-serial-config-bundle-guest-restart-kd-none-rxloss-true-20260403.json](../../registry-research-framework/audit/windbg-serial-config-bundle-guest-restart-kd-none-rxloss-true-20260403.json)
