[CmdletBinding()]
param([string]$TweakId, [string]$QueueCsv, [switch]$QueueOnly)

& (Join-Path $PSScriptRoot "_invoke-phase.ps1") -Phase "faz1" -TweakId $TweakId -QueueCsv $QueueCsv -QueueOnly:$QueueOnly
