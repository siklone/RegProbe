# Execution-Required KVM Guest-Control Gap Audit

Date: 2026-04-08

## Outcome

- Runtime runner mapped for both tweaks: `True`
- Runtime probe vmrun-backed: `True`
- Controller doc still shared-folder based: `True`
- Controller script still vmrun-backed: `True`
- libvirt domain running: `True`
- qemu guest agent channel present: `False`
- qemu guest agent ping ok: `False`
- Bootstrap ISO exists on host: `True`

## Details

- `power.control.allow-system-required-power-requests` -> script=`registry-research-framework/tools/run-path-aware-runtime-probe.ps1` args=`['-CandidateIds', 'power.control.allow-system-required-power-requests']`
- `power.control.allow-audio-to-enable-execution-required-power-requests` -> script=`registry-research-framework/tools/run-path-aware-runtime-probe.ps1` args=`['-CandidateIds', 'power.control.allow-audio-to-enable-execution-required-power-requests']`
- Serial console path: `/dev/pts/4`
- Guest ping stderr: `error: argument unsupported: QEMU guest agent is not configured`
- Bootstrap ISO path: `/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/dist/regprobe-kvm-bootstrap.iso`

## Interpretation

- The remaining execution-required runtime-trace gap is no longer runner design or candidate selection.
- The repo-side narrow lane exists, but live guest execution on the current KVM session is blocked by environment shape rather than by missing research plumbing.
- The next decisive step is either to restore a usable guest-control surface on KVM or to run the narrow path-aware lane on a vmrun-capable host environment.
