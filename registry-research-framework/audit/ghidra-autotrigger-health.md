# Ghidra Autotrigger Health

- Generated UTC: `2026-04-14T01:57:53.990156Z`
- Input bundles selected: `0`
- Queue jobs: `4`
- Autotrigger seeds: `1`
- Symbol resolution requests: `16`
- Symbol resolution batch jobs: `5`
- Symbol resolution blocked jobs: `0`
- Symbol resolution run selected jobs: `5`
- Symbol resolution handoff selected jobs: `5`
- Symbol resolution transfer selected jobs: `5`
- Symbol resolution transfer pack selected jobs: `0`
- Symbol resolution transfer pack check errors: `0`
- Symbol resolution execution run ready jobs: `0`
- Symbol resolution execution run blocked jobs: `0`
- Symbol resolution execution run check errors: `0`
- ETW stackwalk plan status: `ready`
- ETW stackwalk plan check: `ok`
- Dispatch jobs: `4`
- Autotrigger dispatch jobs: `1`
- Run selected jobs: `4`
- Symbol runner available: `True`
- Runner available: `False`
- Runner mode: `dry-run`

## Focus

- Top input bundle: `None`
- Top queue candidate: `power.control.allow-system-required-power-requests`
- Top autotrigger candidate: `power.control.allow-system-required-power-requests`
- Top symbol resolution request: `KernelBase.dll+0x2e436`
- Top symbol resolution batch request: `ghidra-symbol-01-kernelbase-dll-0x2e436`
- Top symbol resolution handoff request: `ghidra-symbol-01-kernelbase-dll-0x2e436`
- Top symbol resolution transfer request: `ghidra-symbol-01-kernelbase-dll-0x2e436`
- Top symbol resolution transfer pack request: `None`
- Top symbol resolution execution run request: `None`

## Coverage

- Input bundle paths: `0`
- Queued candidate ids: `4`
- Seed candidate ids: `1`
- Symbol resolution requests: `16`
- Symbol resolution batch request ids: `5`
- Symbol resolution handoff request ids: `5`
- Symbol resolution transfer request ids: `5`
- Symbol resolution transfer pack request ids: `0`
- Symbol resolution execution run request ids: `0`
- Autotrigger dispatch candidate ids: `1`

## Symbol Handoff

- Handoff status: `ready`
- Operator blocker: `symbol-resolution-ready`
- Selected jobs: `5`
- Blocked jobs: `0`

## Symbol Transfer

- Transfer status: `ready`
- Operator blocker: `transfer-pack-ready`
- Selected jobs: `5`
- Missing repo files: `0`

## Transfer Pack

- Pack status: `None`
- Operator blocker: `None`
- Selected jobs: `0`
- Repo files copied: `0`
- Command files written: `0`

## Transfer Pack Check

- Check status: `None`
- Error count: `0`
- Checked pack files: `0`
- Checked archive files: `0`

## Transfer Execution Run

- Run status: `None`
- Operator blocker: `None`
- Planned jobs: `0`
- Ready jobs: `0`
- Blocked jobs: `0`
- Check status: `None`
- Check errors: `0`

## ETW Stackwalk Capture

- Plan status: `ready`
- Check status: `ok`
- Profile: `kernel-registry-stackwalk-v1`
- Run id: `wave4-registry-stackwalk`
- Stack expected: `True`
- Stackwalk event count: `7`
- Handoff ETL path: `evidence/files/etw-stackwalk/wave4-registry-stackwalk/wave4-registry-stackwalk.etl`
- Plan errors: `0`
- Check errors: `0`

## Symbol Batch Diagnostics

- Missing host tools: `none`
- Missing input counts: `{}`
- Resolution kind counts: `{"module_offset": 16}`
