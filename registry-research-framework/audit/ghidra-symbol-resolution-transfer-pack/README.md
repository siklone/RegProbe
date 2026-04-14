# Ghidra Symbol Resolution Transfer Pack

- Generated UTC: `2026-04-14T05:52:20.534225Z`
- Pack status: `idle`
- Transfer status: `idle`
- Operator blocker: `no-selected-symbol-jobs`
- Next action: `Refresh the autotrigger lane until the handoff surface exposes at least one selected symbol-resolution job.`
- Selected jobs: `0`
- Repo files copied: `9`
- Command files written: `0`
- Pack files checksummed: `0`

## Layout

- `manifests/` copied JSON and markdown manifests
- `repo/` repo-side scripts and guest helpers needed on the destination host
- `commands/` one file per selected request with its suggested command
- `CHECKSUMS.json` SHA-256 manifest for every file in this pack

## Destination Workflow

Use this pack from a full RegProbe checkout on the destination host. First validate the pack summary and archive, then unpack it, generate the imported-pack execution plan, dry-run that plan, and validate the dry-run surface before using `--execute`.

```bash
python3 registry-research-framework/scripts/check_ghidra_symbol_resolution_transfer_pack.py --summary /path/to/ghidra-symbol-resolution-transfer-pack.json
python3 registry-research-framework/scripts/unpack_ghidra_symbol_resolution_transfer_pack.py --summary /path/to/ghidra-symbol-resolution-transfer-pack.json --output-root /path/to/ghidra-symbol-resolution-transfer-pack-import
python3 registry-research-framework/scripts/generate_ghidra_transfer_pack_execution_plan.py --import /path/to/ghidra-symbol-resolution-transfer-pack-import.json
python3 registry-research-framework/scripts/run_ghidra_transfer_pack_execution_plan.py --plan /path/to/ghidra-symbol-resolution-transfer-pack-execution-plan.json
python3 registry-research-framework/scripts/check_ghidra_transfer_pack_execution_run.py --run /path/to/ghidra-symbol-resolution-transfer-pack-execution-run.json
```

