# ETW Stackwalk Execution Pack

- Generated UTC: `2026-04-14T18:07:42.841900Z`
- Pack status: `idle`
- Manifest status: `idle`
- Include holds: `False`
- Next action: `Review excluded hold candidates and reopen intentionally if needed.`
- Requested candidates: `2`
- Selected candidates: `0`
- Excluded candidates: `2`
- Repo files copied: `7`
- Command files written: `0`
- Pack files checksummed: `0`

## Layout

- `manifests/` current ETW execution surfaces copied into the pack
- `repo/` repo-side runner and config files for inspection on another host
- `commands/` one command file per selected ETW execution candidate
- `CHECKSUMS.json` SHA-256 manifest for the pack contents

## Workflow

Use this pack from a full RegProbe checkout. Inspect the copied manifest and command files first. When the pack status is `ready`, run the selected commands from the repo checkout. When the pack status is `idle`, use the hold-reopen plan in the manifest set before reopening execution lanes.

