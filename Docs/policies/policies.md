# Windows Policies
> Update (2025-12-30): LegacyTweakProvider restored missing tweaks; verify this doc against the current catalog.

> **Doc note (2025-12-27):** This document is a reference for Windows policy (ADMX) notes and parsing. The app currently only links out to documentation; it does not ship a full policy editor UI.

Requires elevation: No.

This section is based on my [admx-parser](https://github.com/nohuto/admx-parser) project. You can get the whole parsed ADMX content via:

```powershell
python admx-parser.py [FLAGS]
```

### CLI Flags

| Flag | Description | Default |
| --- | --- | --- |
| `-d, --definitions PATH` | PolicyDefinitions directory | `C:\Windows\PolicyDefinitions` |
| `-l, --language LANG` | Include a language folder (repeatable) | Auto-detected + `en-US` |
| `-i, --ignore NAME` | Ignore an ADMX base name (repeatable) | None |
| `--class {Machine,User}` | Restrict to policy class (repeatable) | All |
| `--category TEXT` | Filter by category substring | None |
| `--policy TEXT` | Filter by policy/display name substring | None |
| `--include-obsolete` | Include obsolete/deprecated policies | Off |
| `--format {json,yaml}` | Output format | `json` |
| `--compress` | Write minified JSON (ignored for YAML) | Pretty |
| `--output PATH` | Custom destination file | `Policies.json`/`Policies.yaml` (in current dir) |
| `-h, --help` | Shows flags from above | - |

### Examples

```c
// Default (pretty JSON)
python admx-parser.py

// YAML output, ignore inetres and WindowsUpdate ADMX files
python admx-parser.py --format yaml --ignore inetres --ignore WindowsUpdate

// Machine-only policies under the Edge category, compressed JSON
python admx-parser.py --class Machine --category Edge --compress
```
