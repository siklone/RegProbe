# Source Enrichment System Informer Follow-up

- Generated: `2026-04-11T22:29:58Z`
- Candidate manifest: `registry-research-framework/audit/kernel-power-96-phase0-candidates-20260329.json`
- Source root override: `REGPROBE_SOURCE_ROOT_SYSTEMINFORMER=/tmp/regprobe-source-cache/systeminformer`

## Result

A shallow `winsiderss/systeminformer` clone provided a usable `phnt` header surface for the source-enrichment lane on the Linux host. The follow-up scanned `881` source files and produced `4` clean candidate hits after the generic-token and prefix-collision tightening.

## Supported candidates

- `power.control.hiberboot-enabled`
  - `phnt/include/ntpoapi.h`
  - `SystemHiberbootState` comment explicitly calls out effective `HiberbootEnabled` state
- `power.session.hiberboot-enabled`
  - same `ntpoapi.h` surface as the control-path candidate
- `power.control.ttm-enabled`
  - `phnt/include/ntpoapi.h`
  - direct `BOOLEAN TtmEnabled;` field
- `system.kernel.disable-exception-chain-validation`
  - `phnt/include/ntpsapi.h`
  - `DisableExceptionChainValidation` execution-option bit

## Explicit no-support

- `power.control.allow-audio-to-enable-execution-required-power-requests`
- `power.control.allow-system-required-power-requests`

The execution-required pair still has no direct source-enrichment support from the `systeminformer/phnt` lane, so the current blocker remains on the static/runtime/doc side rather than the header/source side.

## Takeaways

- `systeminformer/phnt` is a useful lightweight substitute when a full WDK mirror is unavailable.
- The value is narrow: this lane strengthened hiberboot, TTM, and exception-chain-validation, but it did not move the execution-required pair.
- Historical source-enrichment outputs that showed generic `Policy` or prefix-based hits are no longer trustworthy; the tightened scanner now filters those out.
