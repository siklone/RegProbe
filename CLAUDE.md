# Claude Notes (Windows Optimizer)

**Last Updated:** 2026-01-02  
**Branch:** `main`

Start with:
- `HANDOFF_REPORT.md` (what changed recently + what’s incomplete)
- `DEVELOPMENT_STATUS.md` (known issues + recent fixes)
- `AGENTS.md` (non-negotiable safety/architecture rules)

Current focus (2026-01-02):
- Legacy tweak catalog restored via `LegacyTweakProvider` (temporary bridge).
- Theme coverage: MainWindow/Dashboard/Tweaks/Monitor now use `DynamicResource` for theme-bound brushes (light theme parity update).
- Docs linking: tweak catalog HTML anchors + per-tweak "Catalog entry" links.
- Docs coverage audit: `scripts/audit_tweak_sources.py` validates registry/service tokens in category docs; audit report lives in `Docs/tweaks/tweak-source-audit.md`.
- Docs coverage report: `scripts/report_tweak_docs.py` checks per-tweak anchors across category docs, catalog, and details HTML (see `Docs/tweaks/tweak-docs-report.*`).
- Startup flow: theme applies before splash + scan runs before MainWindow.
- Splash shows scan progress (X/Y + current tweak).
- Docs linking now also shows `Source file` from catalog CSV.
- Tweak cards show compact area badge + `Current → Target` on collapsed view.
- Tweak catalog (CSV/MD/HTML) now includes Changes + Risk columns.
- Category docs include generated Tweak Index anchors (links jump to tweak sections).
- Monitor upgrades: multi-target latency (gateway + 1.1.1.1 + 8.8.8.8), disk health and fan RPM detection improvements, and layout reordering.
- Packaging: `scripts/package_windows.cmd` builds a self-contained zip for Windows testing.
- Next: verify light theme + startup flicker, and migrate remaining legacy tweaks into category providers.

Agent checks requested:
- Verify no dark→light flicker on startup (splash + main).
- Confirm splash stays responsive while scan runs.
- Validate `Source file` links open the correct local file.
- Validate tweak docs anchors (catalog/details HTML) open at the correct section.
- Verify Monitor animations are smooth (no Freezable animation errors).

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
