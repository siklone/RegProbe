# Tweak Source Audit (Generated)

Total tweaks: 80

Missing documentation: 2

| ID | Name | Call | Missing Tokens | Source |
| --- | --- | --- | --- | --- |
| `developer.terminal-dev-mode` | Enable Windows Terminal Developer Features | CreateRegistryValueSetTweak | Software\Microsoft\Windows Terminal | `app/Services/TweakProviders/DeveloperTweakProvider.cs#L40` |
| `developer.vscode-git-autofetch` | Disable VS Code Git Autofetch | CreateRegistryTweak | Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced, DisableGitAutofetch | `app/Services/TweakProviders/DeveloperTweakProvider.cs#L56` |
