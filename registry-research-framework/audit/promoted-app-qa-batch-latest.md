# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-05T21:13:12Z
- Catalog candidates: 242
- Selected candidates: 5
- Planned apply-allowed candidates: 5
- Live successes: 5
- Live failures: 0

## Selected Candidates

- `privacy.block-microsoft-accounts` | Microsoft Accounts on This Device | Privacy
  docs: `research/records/privacy.block-microsoft-accounts.json`
  rollback: default=true | previous=true
- `security.disable-defender-sample-submission` | Microsoft Defender Sample Submission | Security
  docs: `research/records/security.disable-defender-sample-submission.review.json`
  rollback: default=true | previous=true
- `visibility.default-account-picture` | Default Account Picture for All Users | Visibility
  docs: `research/records/visibility.default-account-picture.json`
  rollback: default=true | previous=true
- `network.disable-ipv6` | IPv6 Stack Disable Override | Network
  docs: `research/records/network.disable-ipv6.json`
  rollback: default=true | previous=true
- `system.bsod-disable-auto-reboot` | Automatic Restart on System Failure | System
  docs: `research/records/system.bsod-disable-auto-reboot.json`
  rollback: default=true | previous=true

## Live Results

- `privacy.block-microsoft-accounts` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `security.disable-defender-sample-submission` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `visibility.default-account-picture` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `network.disable-ipv6` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `system.bsod-disable-auto-reboot` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
