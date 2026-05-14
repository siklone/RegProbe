# Operator 96 Registry Value Campaign

- Generated UTC: `2026-05-13T06:53:15Z`
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
| `operator96-058-idleprocessorsrequireqosmanagement-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-20.806` | `-7.14` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-058-idleprocessorsrequireqosmanagement-0.json` |
| `operator96-058-idleprocessorsrequireqosmanagement-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-14.774` | `16.41` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-058-idleprocessorsrequireqosmanagement-1.json` |
| `operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-79.274` | `-1.91` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-0.json` |
| `operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-29.075` | `3.97` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-060-allowaudiotoenableexecutionrequiredpowerrequests-1.json` |
| `operator96-061-deepiocoalescingenabled-0` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `80.362` | `0.65` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-061-deepiocoalescingenabled-0.json` |
| `operator96-061-deepiocoalescingenabled-1` | `low_confidence` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `127.332` | `5.52` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-061-deepiocoalescingenabled-1.json` |
| `operator96-062-ignorecscompliancecheck-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-21.26` | `2.92` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-062-ignorecscompliancecheck-1.json` |
| `operator96-062-ignorecscompliancecheck-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-11.624` | `0.94` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-062-ignorecscompliancecheck-0.json` |
| `operator96-063-dripsswhwdivergenceenablelivedump-0` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-75.271` | `-25.8` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-063-dripsswhwdivergenceenablelivedump-0.json` |
| `operator96-063-dripsswhwdivergenceenablelivedump-1` | `harmful` | `low` | `ok` | `ok` | `True` | `ok`/`0` | `-10.306` | `0.63` | `registry-research-framework/audit/registry-value-experiments-low-noise-20260510/operator96-063-dripsswhwdivergenceenablelivedump-1.json` |
