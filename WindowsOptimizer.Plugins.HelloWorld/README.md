# Hello World Plugin

A sample plugin demonstrating the Windows Optimizer plugin system.

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
2. Copy `bin/Release/net8.0-windows/WindowsOptimizer.Plugins.HelloWorld.dll` to the `Plugins` directory
3. Restart Windows Optimizer
4. The "Hello World Plugin Example" tweak will appear in the Tweaks list

## Files Created

- `%USERPROFILE%\Desktop\HelloFromWindowsOptimizer.txt`

