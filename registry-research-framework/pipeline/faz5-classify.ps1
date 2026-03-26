[CmdletBinding()]
param([string]$TweakId, [string]$QueueCsv, [switch]$QueueOnly)

& (Join-Path $PSScriptRoot "_invoke-phase.ps1") -Phase "faz5" -TweakId $TweakId -QueueCsv $QueueCsv -QueueOnly:$QueueOnly
