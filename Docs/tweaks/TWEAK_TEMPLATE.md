# Tweak Documentation Template

Use this template when documenting a new tweak or refreshing an existing one.

## Tweak card

```yaml
id: tweak.category.name
name: Human-readable name
description: |
  Short explanation of what the tweak changes.
  Keep the summary readable in one or two sentences.
risk: Safe | Advanced | Risky
category: Privacy | System | Network | Power | Security | Visibility | etc.
area: Registry | Service | Task | Command | Composite
requires_elevation: true | false
reversible: true | false
windows_versions:
  - Windows 10 (22H2+)
  - Windows 11
```

## Detailed explanation

### What it does

Explain the technical behavior clearly. Call out which registry keys, services, scheduled tasks, commands, or policy surfaces are affected.

### Why people change it

Describe the main use cases, expected benefit, and who the change is meant for.

### Potential side effects

- List the most likely downsides or tradeoffs.
- Note which apps, services, or workflows can be affected.
- Call out compatibility or recovery concerns.

## Technical details

### Registry changes

```text
HKEY_CURRENT_USER\Software\...
  ValueName (REG_DWORD): OldValue -> NewValue
```

### Service changes

| Service | Original | New |
| --- | --- | --- |
| ServiceName | Automatic | Disabled |

### Scheduled task changes

| Task path | New state |
| --- | --- |
| \Microsoft\Windows\... | Disabled |

## Validation steps

1. Check the target state in Registry Editor or with a scripted registry query.
2. Confirm service changes in `services.msc` or with `sc.exe`.
3. Confirm scheduled task changes in Task Scheduler or with `schtasks.exe`.
4. When applicable, confirm runtime behavior in the `Win25H2Clean` VM.

## Rollback procedure

Tweaks should be automatically reversible. If a manual rollback note is still needed, document the exact steps here:

1. Step 1
2. Step 2

## References

- [Microsoft documentation: relevant page](https://learn.microsoft.com/)
- [Security or policy baseline reference](https://learn.microsoft.com/)
