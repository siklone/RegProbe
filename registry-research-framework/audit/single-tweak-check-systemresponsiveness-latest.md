Query: SystemResponsiveness
Status: ok
Inspect status: ok
Inspect matches: 2
QA candidates: 1
Expected values: 10, 30000

[1] power.disable-network-power-saving.policy
  card: Network Power and Multimedia Responsiveness [Power]
  promotion: promoted | apply_allowed=true
  rollback: restore_default=true | restore_previous=true
  research_doc: research/records/power.disable-network-power-saving.policy.review.json
  card_description: These settings affect whether TCP/IP task offloads stay enabled and how MMCSS reserves CPU for multimedia work.
  expected_values:
    - 10 -> matched
    - 30000 -> not found
  commands:
    - inspect: dotnet run --project cli/cli.csproj -- research inspect SystemResponsiveness --expected-value 10 --expected-value 30000
    - readiness: dotnet run --project cli/cli.csproj -- research readiness
    - direct_app: & 'C:\Tools\AppSmoke\RegProbe.App.exe' --tweaks --qa-run-tweak 'power.disable-network-power-saving.policy' --qa-output 'C:\Tools\ValidationController\smoke\power.disable-network-power-saving.policy.qa.json' --qa-shutdown
    - direct_app_skip_rollback: & 'C:\Tools\AppSmoke\RegProbe.App.exe' --tweaks --qa-run-tweak 'power.disable-network-power-saving.policy' --qa-output 'C:\Tools\ValidationController\smoke\power.disable-network-power-saving.policy.qa.json' --qa-shutdown --qa-skip-rollback
    - guest_vm: powershell -ExecutionPolicy Bypass -File '.\scripts\vm\guest-app-tweak-qa.ps1' -TweakId 'power.disable-network-power-saving.policy' -OutputPath 'C:\Tools\ValidationController\smoke\power.disable-network-power-saving.policy.qa.json'
    - guest_vm_skip_rollback: powershell -ExecutionPolicy Bypass -File '.\scripts\vm\guest-app-tweak-qa.ps1' -TweakId 'power.disable-network-power-saving.policy' -OutputPath 'C:\Tools\ValidationController\smoke\power.disable-network-power-saving.policy.qa.json' -SkipRollback
    - kvm_batch: python3 'scripts/vm-kvm/run-guest-app-tweak-qa-batch.py' --id 'power.disable-network-power-saving.policy'
  qa_report_path: C:\Tools\ValidationController\smoke\power.disable-network-power-saving.policy.qa.json
  expected_report:
    - Success=true | Status=ok | RollbackRequested=true
    - required_stages: detect-before, apply, rollback, detect-after
    - card_snapshot: TweakId, Name, Category, EvidenceClass, ResearchStatus, RollbackSnapshotState, HasClaimBoundary, WhatWeKnowSummary, WhatWeDoNotClaimSummary, ProofLanes
  expected_report_skip_rollback:
    - Success=true | Status=ok | RollbackRequested=false
    - required_stages: detect-before, apply, detect-after
  evidence_locations:
    - https://learn.microsoft.com/en-us/windows-hardware/drivers/network/using-registry-values-to-enable-and-disable-task-offloading
    - https://learn.microsoft.com/en-us/windows/win32/procthread/multimedia-class-scheduler-service
    - app/Services/TweakProviders/PowerTweakProvider.cs
    - evidence/captures/power-disable-network-power-saving-policy-etw-qga-unblock-20260507.json and evidence/raw/etw-stackwalk/power-disable-network-power-saving-disabletaskoffload-etw-qga-unblock-20260507/power-disable-network-power-saving-disabletaskoffload-etw-qga-unblock-20260507-summary.json and evidence/raw/etw-stackwalk/power-disable-network-power-saving-disabletaskoffload-etw-qga-unblock-20260507/normalized-registry-bundle.json and evidence/raw/etw-stackwalk/power-disable-network-power-saving-disabletaskoffload-etw-qga-unblock-20260507/power-disable-network-power-saving-disabletaskoffload-etw-qga-unblock-20260507.etl and evidence/raw/etw-stackwalk/power-disable-network-power-saving-systemresponsiveness-etw-qga-unblock-20260507/power-disable-network-power-saving-systemresponsiveness-etw-qga-unblock-20260507-summary.json and evidence/raw/etw-stackwalk/power-disable-network-power-saving-systemresponsiveness-etw-qga-unblock-20260507/normalized-registry-bundle.json and evidence/raw/etw-stackwalk/power-disable-network-power-saving-systemresponsiveness-etw-qga-unblock-20260507/power-disable-network-power-saving-systemresponsiveness-etw-qga-unblock-20260507.etl
  operator_checklist:
    - Run the inspect command and confirm the expected values and tracked registry targets still match the record.
    - Run the readiness command before launching the desktop app so cards, evidence, rollback coverage, and KVM smoke status stay green.
    - Open the app card 'Network Power and Multimedia Responsiveness' and verify the title, category, and linked research record (research/records/power.disable-network-power-saving.policy.review.json) match this plan.
    - Run the direct app or guest VM QA command and keep the JSON report it writes.
    - Check the report fields Success, Status, RollbackRequested, and the stage list before trusting the result.
    - Check the report Card snapshot: title/category, evidence tier, proof lanes, rollback state, and claim-boundary text should match the visible card.
    - If the normal run fails only because you need to observe the post-apply state manually, rerun the skip-rollback variant and record that fact in your notes.
