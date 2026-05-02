# audio.disable-spatial-audio

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

- `repo-doc` Repo source note for audio.disable-spatial-audio -> Docs/tweaks/tweak-provenance.json
- `repo-code` Current app implementation -> app/Services/TweakProviders/AudioTweakProvider.cs
- `vm-test` Guest string scan for spatial-audio registry contract -> evidence/files/vm-tooling-staging/spatial_audio_string_search.txt
- `etw-trace` KVM ETW stage receipt for DisableSpatialOnLowLatency -> evidence/captures/audio-disable-spatial-audio-etw-stackwalk-attempt-20260424.json and evidence/raw/etw-stackwalk/audio-disable-spatial-audio-etw-20260424-batch1/audio-disable-spatial-audio-etw-20260424-batch1-stage.json
- `vm-test` Guest Ghidra launch receipt for DisableSpatialOnLowLatency -> evidence/raw/ghidra/ghidra-audio-disable-spatial-audio-20260424-batch1/summary.json
