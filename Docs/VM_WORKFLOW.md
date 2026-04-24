# VM Workflow

This path is kept for compatibility.

The maintained page now lives at [Docs/research/vm-workflow.md](research/vm-workflow.md).

## Quickstart

Set the VM-specific environment variables before running host or guest orchestration scripts:

```bash
export REGPROBE_VM_DOMAIN=<your-libvirt-domain>
export REGPROBE_VM_USER=<your-vm-username>
export REGPROBE_VM_SNAPSHOT=<your-snapshot-name>
```

Optional overrides used by the flexible script layer:

```bash
export REGPROBE_VM_PATH=<your-vmx-path>
export REGPROBE_VM_BRIDGE_BASE_URL=<your-bridge-url>
export REGPROBE_VM_UPLOAD_DIR=<your-upload-dir>
export REGPROBE_HOST_USER=<your-host-username>
```
