[CmdletBinding()]
param([string]$TweakId, [string]$QueueCsv, [switch]$QueueOnly, [switch]$ExecuteTools)

& (Join-Path $PSScriptRoot "_invoke-phase.ps1") -Phase "faz3" -TweakId $TweakId -QueueCsv $QueueCsv -QueueOnly:$QueueOnly -ExecuteTools:$ExecuteTools
