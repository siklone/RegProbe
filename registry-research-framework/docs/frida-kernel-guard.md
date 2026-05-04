# Frida Kernel Guard

Do not use Frida for:

- `HKLM\SYSTEM\CurrentControlSet\*`
- `HKLM\SYSTEM\Setup\*`
- driver/service parameter lanes
- boot-phase keys

Use ETW, WPR, and checked-in build static analysis instead.
