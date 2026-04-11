#!/usr/bin/env bash
set -euo pipefail

uri="${1:-qemu:///session}"
vm_name="${2:-regprobe-win11-25h2-session}"
iso_path="${3:?usage: attach-bootstrap-iso.sh [uri] [vm_name] /absolute/path/to.iso}"
device="${4:-sdb}"
fallback_device="${5:-sdc}"

if ! [[ -f "$iso_path" ]]; then
  echo "ISO not found: $iso_path" >&2
  exit 1
fi

for args in \
  "change-media \"$vm_name\" \"$device\" \"$iso_path\" --update --live --config --force" \
  "change-media \"$vm_name\" \"$device\" \"$iso_path\" --update --live --force" \
  "change-media \"$vm_name\" \"$device\" --insert \"$iso_path\" --live --config" \
  "change-media \"$vm_name\" \"$device\" --insert \"$iso_path\" --live"
do
  if eval virsh -c "\"$uri\"" "$args" >/dev/null 2>&1; then
    echo "$device"
    exit 0
  fi
done

for args in \
  "attach-disk \"$vm_name\" \"$iso_path\" \"$fallback_device\" --type cdrom --mode readonly --live --config" \
  "attach-disk \"$vm_name\" \"$iso_path\" \"$fallback_device\" --type cdrom --mode readonly --live"
do
  if eval virsh -c "\"$uri\"" "$args" >/dev/null 2>&1; then
    echo "$fallback_device"
    exit 0
  fi
done

echo "Failed to attach bootstrap ISO to $vm_name" >&2
exit 1
