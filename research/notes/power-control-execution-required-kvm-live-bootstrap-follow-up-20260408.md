## Scope

Record the first live guest-side KVM bootstrap attempt after restoring the qemu guest-agent channel and bootstrap ISO, so the remaining execution-required blocker reflects the real post-bootstrap failure mode instead of stopping at "agent not connected".

## Findings

1. A host-side keystroke fallback now exists for the active libvirt desktop:
   - `scripts/vm/send-kvm-text.py`
   - it types into the focused guest window through `virsh send-key`
2. The active Windows guest already had a logged-in desktop and an elevated PowerShell surface available after wake/unlock.
3. A direct official qemu guest-agent MSI was downloaded from Fedora People to the host checkout and exposed through the existing host bridge:
   - `https://fedorapeople.org/groups/virt/virtio-win/direct-downloads/latest-qemu-ga/qemu-ga-x86_64.msi`
   - served inside the guest as `http://10.0.2.2:8766/dist/qga.msi`
4. The live bootstrap typed and ran guest-side commands to:
   - create `C:\RegProbe-Diag\bootstrap`
   - download `qga.msi`
   - install it silently with `msiexec /i ... /qn /norestart`
5. The first live result was a real guest-side service install, not a no-op:
   - `sc.exe query type= service state= all | findstr /i qemu`
   - surfaced `QEMU-GA`
   - `sc.exe start QEMU-GA`
   - `sc.exe query QEMU-GA`
   - showed `STATE : 4 RUNNING`
6. Guest-side device enumeration also confirmed virtio-serial surfaces exist:
   - `Get-WmiObject Win32_PnPEntity | findstr /i vioserial`
   - returned two `VIOSERIALPORT` device paths
7. `qemu-ga.exe -h` confirmed the Windows agent's default transport path is the expected qga channel:
   - `\\.\Global\org.qemu.guest_agent.0`
8. A second official guest-tools package was then downloaded and installed from Fedora People:
   - `https://fedorapeople.org/groups/virt/virtio-win/direct-downloads/latest-virtio/virtio-win-gt-x64.msi`
   - served inside the guest as `http://10.0.2.2:8766/dist/virtio-win-gt-x64.msi`
   - installed silently with `msiexec /i ... /qn /norestart`
   - followed by a real guest reboot
9. Even after channel attach, qga MSI install, confirmed `QEMU-GA` service start, visible `VIOSERIALPORT` devices, guest-tools MSI install, and reboot, host-side guest control still did not recover:
   - `virsh qemu-agent-command regprobe-win11-25h2-session '{"execute":"guest-ping"}'`
   - now fails with `QEMU guest agent is not available due to an error`
10. This is a narrower blocker than the previous state:
   - no longer "channel missing"
   - no longer simply "guest agent not connected"
   - now "guest-side qga runtime/protocol error after bootstrap"

## Interpretation

The execution-required pair is no longer blocked by missing KVM bootstrap media or by the absence of a qemu guest-agent channel. It is also no longer blocked by a totally missing Windows qga install. The live guest now reaches a stronger but still failing state: the `QEMU-GA` service can be installed and started, virtio-serial devices enumerate in the guest, and the guest-tools MSI can be installed and rebooted, yet host-side `guest-ping` still returns `QEMU guest agent is not available due to an error`. The remaining environment gate is therefore a guest-side qga runtime/protocol fault, plus the still-vmrun-oriented controller stack, rather than missing bootstrap plumbing.
