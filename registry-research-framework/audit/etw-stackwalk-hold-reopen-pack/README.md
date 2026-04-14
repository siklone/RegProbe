# ETW Stackwalk Hold Reopen Pack

- Generated UTC: `2026-04-14T21:27:42.948165Z`
- Pack status: `ready`
- Default run mode: `dry-run`
- Default selected jobs: `0`
- Default skipped hold jobs: `2`
- Next action: `Review prerequisites, dry-run the include-holds plan command, then run the include-holds reopen command intentionally.`
- Reopen candidates: `2`
- Repo files copied: `6`
- Command files written: `2`
- Pack files checksummed: `0`

## Layout

- `manifests/` hold reopen plan, execution manifest, batch, and run surfaces
- `repo/` repo-side ETW scripts and config needed for an intentional reopen
- `commands/` one reopen command file per intentional-hold candidate
- `CHECKSUMS.json` SHA-256 manifest for the pack contents

## Workflow

Start by reading the prerequisites in each command file. Then dry-run the `include_holds` plan command for the candidate you want to reopen. Only run the `include_holds` execution command after we intentionally decide to reopen that lane.

