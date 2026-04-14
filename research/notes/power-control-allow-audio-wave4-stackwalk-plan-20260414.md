# power.control.allow-audio-to-enable-execution-required-power-requests Wave 4 stackwalk plan - 2026-04-14

The audio execution-required sibling now has an operator-ready Wave 4 ETW stackwalk profile instead of relying on the old generic registry-stackwalk plan.

What this closes:

- The repo already had retained docs, KD value/reader evidence, exact INIT-table binding, static init-walker recovery, and a retained soft-reboot ETW `no-hit` lane for `AllowAudioToEnableExecutionRequiredPowerRequests`.
- The next missing proof is narrower: capture an exact current-build runtime query or read path for the audio-specific value, then resolve its caller stack the same way the system-required sibling was resolved on 2026-04-14.

What changed in the tooling surface:

- `registry-research-framework/config/etw-stackwalk-profiles.json` now exposes `execution-required-audio-stackwalk-v1`.
- `registry-research-framework/config/tweak-vm-runners.json` now maps `power.control.allow-audio-to-enable-execution-required-power-requests` directly to that ETW stackwalk profile, so the planner can resolve the lane from the candidate id instead of requiring a memorized profile name.
- The capture planner now emits a repo-native `repo_guest_capture` command that runs `scripts/vm-kvm/run-guest-etw-stackwalk-capture.py`, ingests the ETL/XML into `evidence/files/etw-stackwalk/`, and refreshes the Ghidra autotrigger lane automatically.
- The active generated plan can therefore pivot directly into a retained `wave4-allow-audio-e2e` bundle when the focused guest is available again.

Expected next proof shape:

- Exact runtime hit for `HKLM\\SYSTEM\\CurrentControlSet\\Control\\Power\\AllowAudioToEnableExecutionRequiredPowerRequests`.
- Caller stack resolving through `reg.exe` / `kernelbase.dll` / `ntdll.dll` / `ntoskrnl.exe`, or an alternate current-build reader if the audio lane is serviced indirectly.
- If the lane still returns `no-hit`, the blocker remains the same narrow pair: no exact current-build runtime hit and no named Microsoft publication for the internal audio-specific setting.
