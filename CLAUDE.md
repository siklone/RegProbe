# Claude Notes (Windows Optimizer)

**Last Updated:** 2025-12-27  
**Branch:** `main`

Start with:
- `HANDOFF_REPORT.md` (what changed recently + what’s incomplete)
- `DEVELOPMENT_STATUS.md` (known issues + recent fixes)
- `AGENTS.md` (non-negotiable safety/architecture rules)

## Quick Commands

- Build: `dotnet build`
- Run: `dotnet run --project WindowsOptimizer.App`

## Operational Notes

- Logs:
  - Debug log: `%TEMP%\\WindowsOptimizer_Debug.log`
  - Tweak CSV log: `tweak-log.csv` (via the app’s log store/export)
- ElevatedHost override (useful for `dotnet run`): `WINDOWS_OPTIMIZER_ELEVATED_HOST_PATH=C:\\path\\to\\WindowsOptimizer.ElevatedHost.exe`

## Guardrails (Do Not Break)

- SAFE tweaks must be reversible: Detect → Apply → Verify → Rollback
- Default behavior is Preview/DryRun; never apply changes automatically
- Do NOT add “disable Defender/Firewall/SmartScreen” under SAFE
- Admin-required operations must run via ElevatedHost (separate process)
- All actions must be logged; logs must be exportable

