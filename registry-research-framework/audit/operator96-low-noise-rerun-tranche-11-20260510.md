# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-11T10:25:37Z`
- Status: **ok**
- Planned experiments: `10`
- Completed in this run: `10`

## Plan

| # | Experiment | Target | Value | Default | Source quality |
|---:|---|---|---:|---|---|
| 58 | `operator96-058-idleprocessorsrequireqosmanagement-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IdleProcessorsRequireQosManagement` | `0` | `absent` | `vm-observed` |
| 58 | `operator96-058-idleprocessorsrequireqosmanagement-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IdleProcessorsRequireQosManagement` | `1` | `absent` | `vm-observed` |
| 60 | `operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AllowAudioToEnableExecutionRequiredPowerRequests` | `0` | `absent` | `vm-observed` |
| 60 | `operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\AllowAudioToEnableExecutionRequiredPowerRequests` | `1` | `absent` | `vm-observed` |
| 61 | `operator96-061-deepiocoalescingenabled-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DeepIoCoalescingEnabled` | `0` | `absent` | `vm-observed` |
| 61 | `operator96-061-deepiocoalescingenabled-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DeepIoCoalescingEnabled` | `1` | `absent` | `vm-observed` |
| 62 | `operator96-062-ignorecscompliancecheck-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IgnoreCsComplianceCheck` | `1` | `absent` | `vm-observed` |
| 62 | `operator96-062-ignorecscompliancecheck-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\IgnoreCsComplianceCheck` | `0` | `absent` | `vm-observed` |
| 63 | `operator96-063-dripsswhwdivergenceenablelivedump-0` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DripsSwHwDivergenceEnableLiveDump` | `0` | `absent` | `vm-observed` |
| 63 | `operator96-063-dripsswhwdivergenceenablelivedump-1` | `HKLM\SYSTEM\CurrentControlSet\Control\Power\DripsSwHwDivergenceEnableLiveDump` | `1` | `absent` | `vm-observed` |

## Results

| Experiment | Verdict | Confidence | Host noise | Status | Hard smoke | Interactive | Primary Δ% | Post-reboot IO Δ% | Artifact |
|---|---|---|---|---|---|---|---:|---:|---|
| `operator96-058-idleprocessorsrequireqosmanagement-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-32.63` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-058-idleprocessorsrequireqosmanagement-0.json` |
| `operator96-058-idleprocessorsrequireqosmanagement-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-29.07` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-058-idleprocessorsrequireqosmanagement-1.json` |
| `operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-0` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `-27.3` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-0.json` |
| `operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-1` | `noisy` | `low` | `noisy` | `ok` | `True` | `ok`/`0` | `None` | `30.49` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-1.json` |
| `operator96-061-deepiocoalescingenabled-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-23.938` | `-3.97` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-061-deepiocoalescingenabled-0.json` |
| `operator96-061-deepiocoalescingenabled-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-16.072` | `1.28` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-061-deepiocoalescingenabled-1.json` |
| `operator96-062-ignorecscompliancecheck-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `143.615` | `6.83` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-062-ignorecscompliancecheck-1.json` |
| `operator96-062-ignorecscompliancecheck-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `195.35` | `64.39` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-062-ignorecscompliancecheck-0.json` |
| `operator96-063-dripsswhwdivergenceenablelivedump-0` | `harmful` | `high` | `ok` | `ok` | `True` | `ok`/`0` | `-7.338` | `4.78` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-063-dripsswhwdivergenceenablelivedump-0.json` |
| `operator96-063-dripsswhwdivergenceenablelivedump-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-10.992` | `-1.61` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510-tranche-11/operator96-063-dripsswhwdivergenceenablelivedump-1.json` |
