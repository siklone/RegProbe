[CmdletBinding()]
param([string]$TweakId, [string]$QueueCsv, [switch]$QueueOnly)

& (Join-Path $PSScriptRoot "_invoke-phase.ps1") -Phase "faz2" -TweakId $TweakId -QueueCsv $QueueCsv -QueueOnly:$QueueOnly
