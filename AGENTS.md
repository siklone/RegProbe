# Agent Rules (Windows Optimizer)

**Last Reviewed:** 2025-12-27

## Safety
- SAFE tweaks must be reversible: Detect -> Apply -> Verify -> Rollback.
- Default behavior is DryRun/Preview; do not apply system changes automatically.
- Do NOT add "disable Defender/Firewall/SmartScreen" under SAFE.

## Architecture
- WPF MVVM with a navigation Shell.
- Admin operations must run via ElevatedHost (separate process). App is not always-admin.
- All actions must be logged; logs must be exportable.

## Quality
- Prefer small, composable services.
- Add unit tests for engine contracts and adapters where possible.
