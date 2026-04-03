# WinDbg Pipe And Launch Matrix

Date: `2026-04-03`

## Summary

- executed profiles: `4`
- healthy-breakin profiles: `0`
- fatal-break profiles: `0`
- attach-ok-command-not-executed profiles: `2`
- transport-error profiles: `2`

## Profiles

- `guest-restart-breakin-server-quiet` -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, endpoint `server`, launch `quiet`, breakin_attempted `True`, breakin_success_count `0`, shell_recovered `True`, fatal `False`
- `guest-restart-breakin-server-standard` -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, endpoint `server`, launch `standard`, breakin_attempted `True`, breakin_success_count `0`, shell_recovered `True`, fatal `False`
- `guest-restart-breakin-client-quiet` -> status `trace-error`, transport `transport_error`, endpoint `client`, launch `quiet`, breakin_attempted `True`, breakin_success_count `0`, shell_recovered `True`, fatal `False`
- `guest-restart-breakin-client-standard` -> status `trace-error`, transport `transport_error`, endpoint `client`, launch `standard`, breakin_attempted `True`, breakin_success_count `0`, shell_recovered `True`, fatal `False`

## Follow-Up

- keep this phase focused on pipe endpoint and debugger launch mode only
- do not widen to key-specific arbitration until one variant reaches a healthy post-restart prompt
- if all variants stay attach-ok-command-not-executed, the next step is likely outside the current named-pipe contract
