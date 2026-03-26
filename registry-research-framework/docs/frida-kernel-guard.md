# Frida Kernel Guard

Do not use Frida for:

- `HKLM\SYSTEM\CurrentControlSet\*`
- `HKLM\SYSTEM\Setup\*`
- driver/service parameter lanes
- boot-phase keys

Use ETW, WPR, and current-build static analysis instead.
