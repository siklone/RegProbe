# User Guide

RegProbe is the careful registry tool. The goal is not to expose every registry idea with a button; the goal is to expose supported changes with preview, verification, and rollback expectations that are easy to understand.

## What To Expect

- the app shows current state before you change it
- higher-risk operations keep elevated work separated from the main process
- supported settings should carry a rollback story
- research-only records stay in the repo instead of being surfaced as casual one-click actions

## Before You Change Anything

Create a restore point, backup, or VM snapshot first. RegProbe aims to be reversible, but Windows configuration work still deserves an extra safety net.

Read the [security policy](../../SECURITY.md) if you want the elevated-host boundary and threat model in one place.

## The Main User Flow

1. Open the app and inspect the current state.
2. Review what the setting claims to change.
3. Preview before apply.
4. Apply deliberately.
5. Verify the result.
6. Keep rollback close until you are satisfied.

## What The App Ships Today

The current shipped app is intentionally focused:

- `Tweaks` is the main tweak workspace
- `Recovery` is for rollback and cleanup flows
- `Diagnostics` keeps build, repo, and log context nearby

The research pipeline is much broader than the shipped UI. Most users can ignore the repo's traces, audits, ETW captures, and static-analysis exports unless they want to understand why a setting is trusted or blocked.

## How To Read A Setting

Every serious setting should answer:

- what is it?
- how strong is the proof?
- can I safely apply it?
- how do I undo it?

The quick model is:

- `Docs` and `Policy` prove the control surface
- `VM` and `Trace` strengthen runtime proof
- `Rollback` tells you whether reversal was tested
- `Blocked`, `Research-only`, and `Experimental` mean the repo is deliberately not overclaiming

If you want the longer version, use [How to read a record](../research/how-to-read-a-record.md).

## Build From Source

If you are not using a release archive yet, the shortest path is:

```powershell
dotnet build RegProbe.sln -c Release
dotnet run --project app/app.csproj
```

If `dotnet` is not on `PATH` on your host, use the checked-in wrapper instead:

```bash
./dotnetw build RegProbe.sln -c Release -p:EnableWindowsTargeting=true
./dotnetw run --project app/app.csproj
```

```powershell
.\dotnetw.ps1 build RegProbe.sln -c Release
.\dotnetw.ps1 run --project app/app.csproj
```

The fuller build, test, package, and publish commands live in the root [README](../../README.md).

## Releases

The default desktop download path is the portable desktop archive plus the matching checksum file.

- use `RegProbe-Portable-<version>-win-x64.zip` for the desktop app
- use `RegProbe-Cli-<version>-win-x64.zip` if you only want scripted workflows
- verify integrity against `RegProbe-<version>-win-x64-sha256.txt`

The compact release surface is summarized in the [support matrix](support-matrix.md).

## CLI

If you prefer scripted workflows, use the [CLI reference](cli.md). The CLI mirrors the same SAFE bias as the desktop app: dry-run by default where possible, explicit apply when mutation is intended, and research gating that stays honest about blocked or research-only tweaks.
