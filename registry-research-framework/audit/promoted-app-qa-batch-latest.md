# Promoted App QA Batch

- Status: PASS
- Generated UTC: 2026-05-05T17:19:56Z
- Catalog candidates: 240
- Selected candidates: 8
- Planned apply-allowed candidates: 8
- Live successes: 8
- Live failures: 0

## Selected Candidates

- `audio.disable-beep` | System Beep Driver | Audio
  docs: `research/records/audio.disable-beep.review.json`
  rollback: default=true | previous=true
- `audio.show-disconnected-devices` | Show Disconnected Audio Devices | Audio
  docs: `research/records/audio.show-disconnected-devices.review.json`
  rollback: default=true | previous=true
- `developer.docker-performance` | Docker Desktop WSL 2 Backend | Developer
  docs: `research/records/developer.docker-performance.review.json`
  rollback: default=true | previous=true
- `developer.dotnet-telemetry-disable` | .NET CLI Telemetry Opt-Out | Developer
  docs: `research/records/developer.dotnet-telemetry-disable.json`
  rollback: default=true | previous=true
- `explorer.always-show-icons-never-thumbnails` | Always Show Icons, Never Thumbnails | Explorer
  docs: `research/records/explorer.always-show-icons-never-thumbnails.review.json`
  rollback: default=true | previous=true
- `explorer.disable-low-disk-space-warning` | Low Disk Space Warning | Explorer
  docs: `research/records/explorer.disable-low-disk-space-warning.json`
  rollback: default=true | previous=true
- `network.disable-active-probing` | NCSI Active Probing Policy | Network
  docs: `research/records/network.disable-active-probing.review.json`
  rollback: default=true | previous=true
- `network.disable-default-shares` | Automatic Administrative Shares | Network
  docs: `research/records/network.disable-default-shares.json`
  rollback: default=true | previous=true

## Live Results

- `audio.disable-beep` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `audio.show-disconnected-devices` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `developer.docker-performance` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `developer.dotnet-telemetry-disable` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `explorer.always-show-icons-never-thumbnails` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `explorer.disable-low-disk-space-warning` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `network.disable-active-probing` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
- `network.disable-default-shares` | success=true | status=ok
  summary: Apply/verify path completed and rollback restored the tweak.
