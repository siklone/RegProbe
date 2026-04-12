# power.control.hibernate-enabled-default decision gate review - 2026-04-12

## Decision

Keep `power.control.hibernate-enabled-default` blocked as an intentional environment hold.

The record has strong path, static, Procmon, ETW, and reboot-observation evidence, including persistence at `1` across a real KVM reboot. The remaining missing lane requires an actual hibernation trigger, and the current virtualized baselines do not expose hibernation.

This is not a dead-flag result and not a generic documentation review. The hold reason is specific: the current VM fleet cannot exercise the required hibernation transition.
