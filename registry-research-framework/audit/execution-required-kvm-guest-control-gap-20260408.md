# Execution-Required KVM Guest-Control Gap Audit

Date: 2026-04-09

## Outcome

- Runtime runner mapped for both tweaks: `True`
- Runtime probe vmrun-backed: `True`
- Controller doc still shared-folder based: `True`
- Controller script still vmrun-backed: `True`
- libvirt domain running: `True`
- qemu guest agent channel present: `True`
- qemu guest agent ping ok: `False`
- qga frontend-open: `False`
- qga qtree state: `port 2, guest off, host off, throttle off`
- qga unix socket connectable: `False`
- monitor socket connectable: `True`
- Bootstrap ISO exists on host: `True`

## Details

- `power.control.allow-system-required-power-requests` -> script=`registry-research-framework/tools/run-path-aware-runtime-probe.ps1` args=`['-CandidateIds', 'power.control.allow-system-required-power-requests']`
- `power.control.allow-audio-to-enable-execution-required-power-requests` -> script=`registry-research-framework/tools/run-path-aware-runtime-probe.ps1` args=`['-CandidateIds', 'power.control.allow-audio-to-enable-execution-required-power-requests']`
- Channel names: `['com.redhat.spice.0', 'org.qemu.guest_agent.0']`
- Serial console path: `/dev/pts/4`
- Guest ping stderr: `error: Guest agent is not responding: QEMU guest agent is not connected`
- query-chardev filename: `disconnected:unix:/run/user/1000/libvirt/qemu/run/channel/1-regprobe-win11-25h2-/org.qemu.guest_agent.0,server=on`
- qga socket path: `/run/user/1000/libvirt/qemu/run/channel/1-regprobe-win11-25h2-/org.qemu.guest_agent.0`
- qga socket connect error: `[Errno 111] Connection refused`
- monitor socket path: `/home/rai/.config/libvirt/qemu/lib/domain-1-regprobe-win11-25h2-/monitor.sock`
- monitor socket connect error: `n/a`
- Bootstrap ISO path: `/run/media/rai/535fc4a5-7434-4467-8561-a9411c215537/Dev/RegProbe-latest/dist/regprobe-kvm-bootstrap.iso`

## Interpretation

- The remaining execution-required runtime-trace gap is no longer runner design or candidate selection.
- The repo-side narrow lane exists, the qemu guest-agent channel is attached in XML/QEMU, but the live socket still does not expose a usable qga frontend attachment.
- The current evidence points earlier than generic protocol noise: the qga chardev stays `frontend-open=false`, the virtserial port stays `guest off, host off`, and even direct host-side socket connect is refused while the monitor socket stays healthy.
- The next decisive step is to debug the host/libvirt/QEMU-side qga attach path, or to run the same narrow lane on a vmrun-capable environment that bypasses this KVM guest-control failure.
