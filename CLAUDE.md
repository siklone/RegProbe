# Claude Notes (Windows Optimizer)

> **⚠️ AI AGENT: STOP! Before doing ANY work, you MUST read these files:**
> 1. **`DEVELOPMENT_ROADMAP.md`** - Implementation plan with code examples
> 2. **`AGENTS.md`** - Safety rules (BREAKING THESE = REJECTED CODE)
> 3. **`DEVELOPMENT_STATUS.md`** - Known issues to avoid duplicating work
>
> **Skip this and your work will be rejected.**

---

**Last Updated:** 2026-01-19
**Branch:** `main`

Start with:
- `HANDOFF_REPORT.md` (what changed recently + what's incomplete)
- `DEVELOPMENT_STATUS.md` (known issues + recent fixes)
- `DEVELOPMENT_ROADMAP.md` (approved v2.1 roadmap for major features)
- `AGENTS.md` (non-negotiable safety/architecture rules)

Current focus (2026-01-19):
- **NEW: Development Roadmap v2.1** approved and documented in `DEVELOPMENT_ROADMAP.md`
- Roadmap includes: Single Instance, Multi-threading, Splash Preloading, Hardware Database, Monitor Redesign, Process Management
- Legacy tweak catalog restored via `LegacyTweakProvider` (temporary bridge).
- Theme coverage: MainWindow/Dashboard/Tweaks/Monitor now use `DynamicResource` for theme-bound brushes (light theme parity update).
- Docs linking: tweak catalog HTML anchors + per-tweak "Catalog entry" links.
- Docs coverage audit: `scripts/audit_tweak_sources.py` validates registry/service tokens in category docs; audit report lives in `Docs/tweaks/tweak-source-audit.md`.
- Docs coverage report: `scripts/report_tweak_docs.py` checks per-tweak anchors across category docs, catalog, and details HTML (see `Docs/tweaks/tweak-docs-report.*`).
- Startup flow: theme applies before splash + scan runs before MainWindow.
- Splash shows scan progress (X/Y + current tweak).
- Startup preload now initializes metric threading and warms hardware identifiers via `PreloadManager`.
- Hardware database scaffolding (SQLite) is in place; embedded seed/import + update check added, CPU/GPU/RAM/Storage/Motherboard tables seeded, CPU/GPU cards resolve specs via fallback (DB -> identity).
- Hardware detail windows load async and show live metrics (quick stats use clock/power, RAM used/free, disk read/write); MetricDataBus emits CPU clock/power, GPU clock/power/memory, RAM available, disk read/write/health.
- Docs linking now also shows `Source file` from catalog CSV.
- Tweak cards show compact area badge + `Current → Target` on collapsed view.
- Tweak catalog (CSV/MD/HTML) now includes Changes + Risk columns.
- Category docs include generated Tweak Index anchors (links jump to tweak sections).
- Monitor upgrades: multi-target latency (gateway + 1.1.1.1 + 8.8.8.8), disk health and fan RPM detection improvements, and layout reordering.
- Monitor disk health list + Sensor Diagnostics export + optional shadow toggle (default OFF) to help isolate missing SMART/fan data and reduce scroll jank.
- Sensor coverage: CPU temp/fan fallbacks and disk health WMI fallbacks; Tweaks list caching to reduce scroll lag.
- Packaging: `scripts/package_windows.cmd` builds a self-contained zip for Windows testing (ReadyToRun disabled by default; pass `-ReadyToRun` if you want it).

Next Steps (from Roadmap):
- Sprint 1: Single Instance Manager + MetricDataBus + Threading Architecture
- Sprint 2: Hardware Database (SQLite) + Fallback Data Providers
- Sprint 3: Splash Screen Preloading improvements
- Sprint 4: Monitor View Redesign with Hardware Cards
- Sprint 5: Process Management (Priority, Affinity, Memory Trim)
- Sprint 6: Polish and Testing

Agent checks requested:
- Verify no dark→light flicker on startup (splash + main).
- Confirm splash stays responsive while scan runs.
- Validate `Source file` links open the correct local file.
- Validate tweak docs anchors (catalog/details HTML) open at the correct section.
- Verify Monitor animations are smooth (no Freezable animation errors).
- Verify per-disk health list renders and Sensor Diagnostics export creates a report.
- Validate Card Shadows toggle removes drop shadows across Dashboard/Tweaks/Monitor.

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
- Do NOT add "disable Defender/Firewall/SmartScreen" under SAFE
- Admin-required operations must run via ElevatedHost (separate process)
- All actions must be logged; logs must be exportable

---

## Agent Collaboration Rules

**IMPORTANT: All AI agents (Claude, Copilot, Codex, Cursor, etc.) MUST follow these rules.**

### 1. Before Starting Work
1. Read `DEVELOPMENT_ROADMAP.md` for the implementation plan
2. Read `AGENTS.md` for safety rules (DO NOT BREAK!)
3. Check `DEVELOPMENT_STATUS.md` for known issues

### 2. Commit Rules (MANDATORY)

Every commit MUST include `Co-Authored-By` with your agent name:

```bash
git commit -m "feat/fix/refactor(scope): Description

Co-Authored-By: [YOUR_AGENT_NAME] <email>"
```

**Agent Names to Use:**
| Agent | Co-Authored-By Line |
|-------|---------------------|
| Claude Opus | `Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>` |
| Claude Sonnet | `Co-Authored-By: Claude Sonnet <noreply@anthropic.com>` |
| GitHub Copilot | `Co-Authored-By: GitHub Copilot <noreply@github.com>` |
| OpenAI Codex | `Co-Authored-By: OpenAI Codex <noreply@openai.com>` |
| Cursor AI | `Co-Authored-By: Cursor AI <noreply@cursor.com>` |
| Other | `Co-Authored-By: [Agent Name] <noreply@example.com>` |

### 3. After Each Progress

1. **Commit your code changes:**
   ```bash
   git add [changed files]
   git commit -m "feat(scope): What you did

   Co-Authored-By: [Your Agent] <email>"
   ```

2. **Update documentation:**
   - Add your changes to `DEVELOPMENT_STATUS.md` under "Recent Fixes & Changes"
   - Update `CLAUDE.md` "Current focus" if needed

3. **Commit documentation:**
   ```bash
   git add DEVELOPMENT_STATUS.md CLAUDE.md
   git commit -m "docs: Update status with [feature/fix]

   Co-Authored-By: [Your Agent] <email>"
   ```

4. **Push to remote:**
   ```bash
   git push origin [branch-name]
   ```

### 4. If You Find a Bug

1. Document it in `DEVELOPMENT_STATUS.md` under "Known Issues"
2. Include severity, affected files, and reproduction steps
3. Commit with your agent name

### 5. Sprint Assignment

Work on sprints from `DEVELOPMENT_ROADMAP.md`:
- **Sprint 1:** Single Instance + MetricDataBus + Threading
- **Sprint 2:** Hardware Database (SQLite) + Fallback Providers
- **Sprint 3:** Splash Screen Preloading
- **Sprint 4:** Monitor View Redesign
- **Sprint 5:** Process Management
- **Sprint 6:** Polish & Testing

### 6. Code Quality

- Run `dotnet build` before committing - don't break the build!
- Follow existing code patterns in the codebase
- Use roadmap code examples as reference, not copy-paste
- Write testable code

---

**Example Workflow:**
```bash
# 1. Implement SingleInstanceManager
# ... write code ...

# 2. Commit code
git add WindowsOptimizer.App/Services/SingleInstanceManager.cs
git commit -m "feat(app): Add SingleInstanceManager with Mutex + IPC

- Implement named mutex for single instance enforcement
- Add Named Pipe IPC for argument forwarding
- Handle abandoned mutex edge case

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"

# 3. Update docs
# ... edit DEVELOPMENT_STATUS.md ...

git add DEVELOPMENT_STATUS.md
git commit -m "docs: Add SingleInstanceManager to recent changes

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>"

# 4. Push
git push origin sprint0/solution-skeleton
```
