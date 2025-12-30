# Hello World Plugin
> Update (2025-12-30): LegacyTweakProvider restored missing tweaks; verify this doc against the current catalog.

A sample plugin demonstrating the Windows Optimizer plugin system.

**Last Updated:** 2025-12-27

## What It Does

This plugin creates a simple text file on your Desktop called `HelloFromWindowsOptimizer.txt`.

- **Apply**: Creates the file with a welcome message
- **Rollback**: Deletes the file
- **Verify**: Checks if the file exists

## Purpose

This is a proof-of-concept plugin showing how to extend Windows Optimizer with custom tweaks.

## Safety

- ✅ Safe (no system changes)
- ✅ Fully reversible
- ✅ No elevation required

## Building

```bash
dotnet build
```

## Installation

1. Build the plugin
2. Copy `bin/Release/net8.0-windows/WindowsOptimizer.Plugins.HelloWorld.dll` into the app's `Plugins` folder (next to `WindowsOptimizer.App.exe`)
3. Restart Windows Optimizer
4. The "Hello World Plugin Example" tweak will appear in the Tweaks list

> Tip: The app loads plugins from `AppDomain.CurrentDomain.BaseDirectory/Plugins` and will create that folder on startup if it doesn't exist.

## Files Created

- `%USERPROFILE%\Desktop\HelloFromWindowsOptimizer.txt`
