# WinDbg Reconnect Command Matrix

Date: `2026-04-03`

## Summary

- executed profiles: `6`
- healthy-breakin profiles: `0`
- fatal-break profiles: `1`
- boot-unsafe profiles: `0`
- attach-ok-command-not-executed profiles: `6`

## Profiles

- `guest-restart-breakin-bonc-lead3` -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, lead `3`, bonc `bonc`, breakin_success_count `0`, shell_recovered `True`, fatal `False`
- `guest-restart-breakin-bonc-lead10` -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, lead `10`, bonc `bonc`, breakin_success_count `0`, shell_recovered `True`, fatal `False`
- `guest-restart-breakin-bonc-lead20` -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, lead `20`, bonc `bonc`, breakin_success_count `0`, shell_recovered `True`, fatal `False`
- `guest-restart-breakin-none-lead10` -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, lead `10`, bonc `none`, breakin_success_count `0`, shell_recovered `True`, fatal `False`
- `guest-restart-breakin-none-lead20` -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, lead `20`, bonc `none`, breakin_success_count `0`, shell_recovered `True`, fatal `True`
- `guest-restart-breakin-b-lead10` -> status `attach-ok-command-not-executed`, transport `attach_ok_command_not_executed`, lead `10`, bonc `b`, breakin_success_count `0`, shell_recovered `True`, fatal `False`

## Follow-Up

- keep this phase focused on reconnect-time host-side command injection
- do not widen to key-specific arbitration until one variant reaches a healthy post-restart prompt
- if all variants stay attach-ok-command-not-executed, move next to pipe endpoint or debugger launch-mode experiments

## Retained audit bundle

- [windbg-reconnect-command-matrix-20260403.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/windbg-reconnect-command-matrix-20260403.json)
- [windbg-reconnect-command-execution-20260403.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/windbg-reconnect-command-execution-20260403.json)
- [windbg-reconnect-command-recovery-20260403.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/windbg-reconnect-command-recovery-20260403.json)
- [windbg-reconnect-command-variants-20260403.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/windbg-reconnect-command-variants-20260403.json)
- [windbg-reconnect-command-bundle-guest-restart-breakin-bonc-lead3-20260403.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/windbg-reconnect-command-bundle-guest-restart-breakin-bonc-lead3-20260403.json)
- [windbg-reconnect-command-bundle-guest-restart-breakin-bonc-lead10-20260403.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/windbg-reconnect-command-bundle-guest-restart-breakin-bonc-lead10-20260403.json)
- [windbg-reconnect-command-bundle-guest-restart-breakin-bonc-lead20-20260403.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/windbg-reconnect-command-bundle-guest-restart-breakin-bonc-lead20-20260403.json)
- [windbg-reconnect-command-bundle-guest-restart-breakin-none-lead10-20260403.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/windbg-reconnect-command-bundle-guest-restart-breakin-none-lead10-20260403.json)
- [windbg-reconnect-command-bundle-guest-restart-breakin-none-lead20-20260403.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/windbg-reconnect-command-bundle-guest-restart-breakin-none-lead20-20260403.json)
- [windbg-reconnect-command-bundle-guest-restart-breakin-b-lead10-20260403.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/windbg-reconnect-command-bundle-guest-restart-breakin-b-lead10-20260403.json)
