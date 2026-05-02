# developer.terminal-dev-mode

- Class: `E`
- Record status: `deprecated`
- Tested build: `26100`
- Reason: `class-e`

This record remains negative evidence on build 26100: the repo did not produce enough supporting proof to promote it into a normal actionable surface.

## Attempted coverage

- Layers: `none`
- Tools: `none`

## Why it stays negative

Archived audit trail only. Keep this out of the normal tweak surface.

## Attached references

- `official-doc` Microsoft Learn: Windows Terminal settings -> https://learn.microsoft.com/en-us/windows/terminal/customize-settings/startup
- `ghidra-headless` Ghidra headless raw-memory scan of TerminalApp.dll -> evidence/raw/ghidra/developer.terminal-dev-mode/terminal-ghidra.txt; evidence/raw/ghidra/developer.terminal-dev-mode/terminal-ghidra-enabledebugtap.txt
- `wpr-trace` WPR capture of Windows Terminal launch -> evidence/files/host-temp/terminal-launch.etl.md
- `repo-code` Current app implementation -> app/Services/TweakProviders/DeveloperTweakProvider.cs
- `etw-trace` Bounded ETW stackwalk timeout receipt -> evidence/raw/etw-stackwalk/developer.terminal-dev-mode-etw-20260424h/developer.terminal-dev-mode-etw-20260424h-summary.json and evidence/raw/etw-stackwalk/developer.terminal-dev-mode-etw-20260424h/developer.terminal-dev-mode-etw-20260424h-stage.json and evidence/captures/developer-terminal-dev-mode-etw-stackwalk-attempt-20260424.json
