# Ghidra Autotrigger Health

- Generated UTC: `2026-05-05T11:11:05.402000Z`
- Input bundles selected: `2`
- Queue jobs: `0`
- Autotrigger seeds: `2`
- Symbol resolution requests: `16`
- Symbol resolution batch jobs: `5`
- Symbol resolution blocked jobs: `0`
- Symbol resolution run selected jobs: `0`
- Symbol resolution run completed jobs: `5`
- Symbol resolution handoff selected jobs: `0`
- Symbol resolution transfer selected jobs: `0`
- Symbol resolution transfer pack selected jobs: `0`
- Symbol resolution transfer pack check errors: `0`
- Symbol resolution execution run ready jobs: `0`
- Symbol resolution execution run blocked jobs: `0`
- Symbol resolution execution run check errors: `0`
- ETW stackwalk plan status: `ready`
- ETW stackwalk plan check: `ok`
- Dispatch jobs: `0`
- Autotrigger dispatch jobs: `0`
- Run selected jobs: `0`
- Symbol runner available: `True`
- Runner available: `False`
- Runner mode: `dry-run`

## Focus

- Top input bundle: `evidence/raw/etw-stackwalk/wave4-allow-audio-e2e/normalized-registry-bundle.json`
- Top queue candidate: `None`
- Top autotrigger candidate: `None`
- Top symbol resolution request: `KernelBase.dll+0x2e436`
- Top symbol resolution batch request: `ghidra-symbol-01-kernelbase-dll-0x2e436`
- Top symbol resolution handoff request: `ghidra-symbol-01-kernelbase-dll-0x2e436`
- Top symbol resolution transfer request: `None`
- Top symbol resolution transfer pack request: `None`
- Top symbol resolution execution run request: `None`

## Coverage

- Input bundle paths: `2`
- Queued candidate ids: `0`
- Seed candidate ids: `2`
- Symbol resolution requests: `16`
- Symbol resolution batch request ids: `5`
- Symbol resolution handoff request ids: `5`
- Symbol resolution transfer request ids: `0`
- Symbol resolution transfer pack request ids: `0`
- Symbol resolution execution run request ids: `0`
- Autotrigger dispatch candidate ids: `0`

## Symbol Handoff

- Handoff status: `idle`
- Operator blocker: `no-runnable-symbol-resolution-jobs`
- Selected jobs: `0`
- Blocked jobs: `0`

## Symbol Transfer

- Transfer status: `idle`
- Operator blocker: `no-selected-symbol-jobs`
- Selected jobs: `0`
- Missing repo files: `0`

## Transfer Pack

- Pack status: `idle`
- Operator blocker: `no-selected-symbol-jobs`
- Selected jobs: `0`
- Repo files copied: `9`
- Command files written: `0`

## Transfer Pack Check

- Check status: `ok`
- Error count: `0`
- Checked pack files: `15`
- Checked archive files: `15`

## Transfer Execution Run

- Run status: `blocked`
- Operator blocker: `execution-run-blocked`
- Planned jobs: `0`
- Ready jobs: `0`
- Blocked jobs: `0`
- Check status: `ok`
- Check errors: `0`

## ETW Stackwalk Capture

- Plan status: `ready`
- Check status: `ok`
- Profile: `execution-required-system-stackwalk-v1`
- Run id: `wave4-allow-system-required-e2e`
- Stack expected: `True`
- Stackwalk event count: `7`
- Handoff ETL path: `evidence/raw/etw-stackwalk/wave4-allow-system-required-e2e/wave4-allow-system-required-e2e.etl`
- Plan errors: `0`
- Check errors: `0`

## Symbol Batch Diagnostics

- Missing host tools: `none`
- Missing input counts: `{}`
- Resolution kind counts: `{"module_offset": 16}`
