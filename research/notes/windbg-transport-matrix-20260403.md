# WinDbg Transport Matrix

Date: `2026-04-03`

## Summary

- executed profiles: `9`
- stable profiles: `2`
- unstable profiles: `7`
- break-in profiles: `4`
- break-in success profiles: `0`

## Profiles

- `minimal-cold-boot` [minimal-matrix] -> status `trace-error`, transport `transport_error`, score `2`
- `minimal-attach-after-shell` [minimal-matrix] -> status `runner-ok`, transport `transport_ok`, score `4`
- `symbols-guest-restart` [minimal-matrix] -> status `trace-error`, transport `transport_error`, score `3`
- `attach-only-attach-after-shell` [breakin-matrix] -> status `runner-ok`, transport `transport_ok`, score `4`
- `breakin-once-attach-after-shell` [breakin-matrix] -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, score `4`, breakin 0/0
- `breakin-twice-attach-after-shell` [breakin-matrix] -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, score `4`, breakin 0/0
- `breakin-delayed-10-attach-after-shell` [breakin-matrix] -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, score `4`, breakin 0/0
- `breakin-delayed-30-attach-after-shell` [breakin-matrix] -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, score `4`, breakin 0/0
- `singlekey-smoke-cold-boot` [singlekey-smoke] -> status `trace-error`, transport `transport_error`, score `2`

## Follow-Up

- keep transport engineering isolated from registry classification
- repeat any potentially stable profile twice before using it as the arbiter base
- only resume single-key semantic arbitration after a transport-stable profile exists
