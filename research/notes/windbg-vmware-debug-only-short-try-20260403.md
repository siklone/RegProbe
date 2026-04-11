# VMware Debug-Only Short Try

- Date: `20260403`
- Status: `error`
- Branch status: `blocked`
- Source profile: `secondary`
- Frozen lane return allowed: `False`

## Sequence
- debugger-first fresh provision
- transport-first smoke
- minimal attach matrix
- breakin smoke

## Stop Rules
- same transport blocker repeats
- same breakin packet error repeats
- command execution remains unreliable

