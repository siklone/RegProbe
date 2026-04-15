# CLI Reference

RegProbe ships a command-line surface for scripted, automation-friendly workflows. The CLI uses the same research gating and SAFE execution model as the desktop app, so a tweak can still be visible yet blocked from mutation if the promotion state says it is not ready.

## Common Commands

```powershell
# List available tweaks
dotnet run --project cli/cli.csproj -- tweak list --risk safe

# Preview a tweak without applying it
dotnet run --project cli/cli.csproj -- tweak apply system.disable-game-recording-broadcasting

# Apply a tweak and keep verify + rollback-on-failure enabled
dotnet run --project cli/cli.csproj -- tweak apply system.disable-game-recording-broadcasting --apply

# Roll back a tweak
dotnet run --project cli/cli.csproj -- tweak revert system.disable-game-recording-broadcasting --apply

# Export the current state bundle
dotnet run --project cli/cli.csproj -- config export --file regprobe-config.json

# Validate JSON tweak definitions
dotnet run --project cli/cli.csproj -- research validate-json-tweaks --input-dir app/Config/Tweaks
```

## Main Command Groups

- `tweak`
  list, preview/apply, and rollback shipped tweaks
- `preset`
  list, preview/apply, and revert preset bundles
- `dns`
  inspect and set DNS provider profiles
- `config`
  export and import RegProbe configuration state
- `info`
  print lightweight machine and runtime context
- `research`
  inspect promotion gates, blocked worklists, regression pack generation, and JSON tweak validation

## SAFE Notes

- `tweak apply` defaults to dry-run unless `--apply` is passed.
- verify stays on by default unless `--no-verify` is used.
- rollback-on-failure stays on by default unless `--no-rollback` is used.
- contributor/debug override flags only affect research gating; they do not bypass elevation or execution requirements.

## Release Use

If you only want the CLI from a release artifact, prefer the CLI-only package once it is published for that release train. Until then, building from source is the reliable path.
