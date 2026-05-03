# PowerRequestOverride Exact Experiment Sheet - 2026-04-22

Bu sheet, `PowerRequestOverride` lane'i icin hangi subtree'nin izlendigini, neyin zaten kanitlandigini, neyin kapandigini ve operator'in bir sonraki dar komutta ne aramasi gerektigini tek yerde toplar.

## Registry Surface

- Root path: `HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerRequestOverride`
- Active subkeys:
  - `Process`
  - `Service`
  - `Driver`
- Retained storage fact:
  - `powercfg /requestsoverride` materialization current lane icin en guclu user-visible storage proof'u
- Retained consumer clue:
  - observed request bitmask `7`

## What Is Already Closed

- Broad subtree existence sorusu kapanmis durumda.
- Generic `*PowerRequest*Reg*` symbol hunt lane'i kapanmis durumda.
- Yeni sprint'in amaci "registry'de var mi" degil:
  - response-side exact reader var mi
  - yoksa semantic boundary UMPO message path'inde mi

## Exact Targets

### Response-side target

- Symbol: `nt!PopPowerRequestHandleRequestOverrideQueryResponse`
- Why it matters:
  - eger burada veya ilk anlamli callee'de exact registry read gorunurse lane kernel-side exact reader proof'una tasinir

### Query-side target

- Symbol: `nt!PopUmpoSendPowerRequestOverrideQuery`
- Why it matters:
  - response tarafinin sadece payload apply / queue logic oldugu durumda query boundary icin context verir

### Message-side target

- Symbol: `nt!PopUmpoSendPowerMessage`
- Why it matters:
  - eger response path sadece deserialization ise gercek discriminator burada transport/payload sekline kayar

## Ready-To-Run Command Chain

Preflight:

```bash
python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --verify-only
```

Preview:

```bash
python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py --dry-run
```

Execute:

```bash
python3 scripts/vm-kvm/run-power-request-override-reader-binding-pipeline.py
```

## Current 2026-04-22 Readiness Snapshot

- `--verify-only` status: `ready_for_execute = true`
- Bundle verifier status: `ok`
- Blockers: none
- Retained counts:
  - `read_order_count = 14`
  - `command_file_count = 2`
  - `review_input_count = 2`
- Promotion consistency:
  - `promotion_blocks_match = true`

## Expected Runtime Artifacts

- Response-side:
  - `/tmp/regprobe-bridge/local-kd-powerrequest-response-reacquire-20260419a.stdout.txt`
  - `/tmp/regprobe-bridge/local-kd-powerrequest-response-reacquire-20260419a-summary.json`
- UMPO-side:
  - `/tmp/regprobe-bridge/local-kd-powerrequest-umpo-message-reacquire-20260419a.stdout.txt`
  - `/tmp/regprobe-bridge/local-kd-powerrequest-umpo-message-reacquire-20260419a-summary.json`

## Review Inputs

- `registry-research-framework/audit/power-request-override-reader-binding-review-rubric-20260419.md`
- `registry-research-framework/audit/power-request-override-reader-binding-result-ledger-template-20260419.md`

## Retained Handoff Bundle

- [power-request-override-handoff-index-20260419.md](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/power-request-override-handoff-index-20260419.md)
- [power-request-override-handoff-index-20260419.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/power-request-override-handoff-index-20260419.json)
- [power-request-override-reader-binding-execution-manifest-20260419.md](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/power-request-override-reader-binding-execution-manifest-20260419.md)
- [power-request-override-reader-binding-execution-manifest-20260419.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/power-request-override-reader-binding-execution-manifest-20260419.json)
- [power-request-override-reader-binding-reacquire-plan-20260419.md](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/power-request-override-reader-binding-reacquire-plan-20260419.md)
- [power-request-override-reader-binding-reacquire-plan-20260419.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/power-request-override-reader-binding-reacquire-plan-20260419.json)
- [power-request-override-reader-binding-review-rubric-20260419.md](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/power-request-override-reader-binding-review-rubric-20260419.md)
- [power-request-override-reader-binding-review-rubric-20260419.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/power-request-override-reader-binding-review-rubric-20260419.json)
- [power-request-override-reader-binding-result-ledger-template-20260419.md](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/power-request-override-reader-binding-result-ledger-template-20260419.md)
- [power-request-override-reader-binding-result-ledger-template-20260419.json](/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/registry-research-framework/audit/power-request-override-reader-binding-result-ledger-template-20260419.json)

## Success Conditions

### Best outcome

- `PopPowerRequestHandleRequestOverrideQueryResponse` path'inde exact registry read gorunur
- leaf context `Process`, `Service`, veya `Driver` ile baglanir

### Still useful outcome

- response path exact read vermese bile request-state apply / queue / bitmask handling netlesir
- `PopUmpoSendPowerMessage` path'i net transport boundary verir

### Not enough

- sadece yeni wildcard symbol listesi
- sadece generic queue plumbing
- subtree varliginin tekrar kanitlanmasi

## What Failed Already

- broad runtime subtree replay yeni semantic proof vermedi
- generic `*PowerRequest*Reg*` sweep exact reader bulmadi
- lane'i tekrar Procmon veya baska genis replay'e acmak bilgi/kazanc oranini dusuruyor

## Promotion Path After Review

Autofill outputs:

- `registry-research-framework/audit/power-request-override-reader-binding-result-ledger-autofill.json`
- `registry-research-framework/audit/power-request-override-reader-binding-result-ledger-autofill.md`

Promote only after review:

```bash
python3 registry-research-framework/scripts/promote_power_request_override_result_ledger.py --run-id power-request-override-reader-binding-reacquire
```

## Fast Read

- Bu lane icin bir sonraki dar karar:
  - response-side exact reader
  - ya da UMPO message boundary
- Bu lane icin tekrar acilmamasi gereken sey:
  - broad subtree search
  - generic `*PowerRequest*Reg*` sweep
