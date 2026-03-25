# WaitToKillServiceTimeout Validation

Record: `system.wait-to-kill-service-timeout`

This pass split the documented service-side shutdown timeout out of the older mixed shutdown bundle.

## What changed

- live app surface:
  - `system.wait-to-kill-service-timeout`
- historical research only:
  - `system.reduce-shutdown-timeouts`

The old bundle mixed one documented machine-wide value with weaker current-user values. This pass keeps only the documented machine-wide value in the app.

## Registry surface

- path:
  - `HKLM/SYSTEM/CurrentControlSet/Control`
- value:
  - `WaitToKillServiceTimeout`
- type:
  - `REG_SZ`

## Official behavior

Microsoft documents `WaitToKillServiceTimeout` as the service shutdown timeout used during restart and shutdown handling.

Source:

- `https://learn.microsoft.com/en-us/windows/win32/services/service-control-handler-function`

## VM proof

Artifact:

- `research/evidence-files/vm-tooling-staging/wait-to-kill-service-timeout-probe-20260325-103117/wait-to-kill-service-timeout-probe.txt`

Observed values:

```text
ORIGINAL={"path_exists":true,"value_exists":true,"value":"5000"}
AFTER={"path_exists":true,"value_exists":true,"value":"2500"}
RESTORED={"path_exists":true,"value_exists":true,"value":"5000"}
```

That gives this record three important things:

- current VM baseline is visible
- app value is visible
- restore path is visible

## Result

This is strong enough for a standalone app mapping.

What is now solid:

- exact path is documented
- exact app write is documented
- live VM baseline is known
- reversible VM apply/restore proof exists

What is not claimed:

- that `2500` is a Microsoft-recommended value
- that all systems share the same current baseline as this VM

So this record is good enough to keep in the app as its own service-timeout tweak, but the value still stays in the advanced bucket because it shortens service cleanup time.
