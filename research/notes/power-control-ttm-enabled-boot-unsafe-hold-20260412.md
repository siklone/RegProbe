# power.control.ttm-enabled boot-unsafe hold - 2026-04-12

## Decision

Mark `power.control.ttm-enabled` as an intentional hold.

The record has enough docs, baseline, string, header, and init-table adjacency evidence for schema-backed tracking, but the important runtime result is negative for safety: `DWORD=1` reproduced `boot-unsafe` in an isolated pilot profile.

Reason: boot-unsafe, do not probe without dedicated boot lane.
