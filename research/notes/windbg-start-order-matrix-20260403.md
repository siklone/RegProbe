# WinDbg Start-Order Matrix

Date: `2026-04-03`

## Summary

- executed profiles: `6`
- roundtrip-ready profiles: `0`
- healthy-roundtrip profiles: `0`
- fatal-break profiles: `0`
- boot-unsafe profiles: `0`
- attach-ok-command-not-executed profiles: `6`

## Profiles

- `guest-restart-roundtrip-bonc-lead3` -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, lead `3`, bonc `bonc`, roundtrip_ready `False`, shell_recovered `True`, fatal `False`
- `guest-restart-roundtrip-bonc-lead10` -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, lead `10`, bonc `bonc`, roundtrip_ready `False`, shell_recovered `True`, fatal `False`
- `guest-restart-roundtrip-bonc-lead20` -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, lead `20`, bonc `bonc`, roundtrip_ready `False`, shell_recovered `True`, fatal `False`
- `guest-restart-roundtrip-none-lead10` -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, lead `10`, bonc `none`, roundtrip_ready `False`, shell_recovered `True`, fatal `False`
- `guest-restart-roundtrip-none-lead20` -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, lead `20`, bonc `none`, roundtrip_ready `False`, shell_recovered `True`, fatal `False`
- `guest-restart-roundtrip-b-lead10` -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, lead `10`, bonc `b`, roundtrip_ready `False`, shell_recovered `True`, fatal `False`

## Follow-Up

- keep this phase focused on attach/start-order and break policy only
- do not widen to key-specific arbitration until one variant reaches a healthy post-restart prompt
- if all variants stay attach-ok-command-not-executed, move next to reconnect-time command injection or pipe endpoint experiments
