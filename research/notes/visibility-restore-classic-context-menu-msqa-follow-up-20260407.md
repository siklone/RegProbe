# Visibility Restore Classic Context Menu Microsoft Q&A Follow-up

Date: 2026-04-07
Candidate: `visibility.restore-classic-context-menu`

## Objective
- re-audit the checked-in official-source surface for the Windows 11 classic-context-menu workaround
- determine whether the record remains an HKCU workaround, moves to a newer path, or is downgraded further

## Result
- the checked-in Microsoft Q&A article still publishes the original HKCU workaround:
  - `reg.exe add "HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32" /f /ve`
  - `reg.exe delete "HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}" /f`
- the same thread now contains conflicting later comments:
  - one September 2025 reply says the method is deprecated in 24H2 and only third-party Explorer patching still works
  - other September 2025, December 2025, and March 2026 replies say the HKCU path still works when run elevated and Explorer is restarted
  - separate replies propose an HKLM `SOFTWARE\Classes\CLSID\...` ownership/edit workflow instead of the original HKCU path
- the HKLM ownership/edit path is not adopted into RegProbe research:
  - it is comment-level advice, not the article body
  - it requires ownership/ACL mutation on a protected machine-wide Classes path
  - it conflicts with the app's checked-in low-risk HKCU mapping

## Artifacts
- official source:
  - Microsoft Q&A article `Restore old Right-click Context menu in Windows 11`
- repo mappings:
  - `research/records/visibility.restore-classic-context-menu.review.json`
  - `app/Services/TweakProviders/VisibilityTweakProvider.cs`
  - `Docs/visibility/visibility.md`

## Short Take
- the app's checked-in HKCU CLSID workaround still matches the article body
- the latest official-source surface is now internally contradictory on 24H2+ behavior
- the record remains classified as a workaround, not as a stable Microsoft policy contract
- the HKLM ownership/edit variant remains out of RegProbe's normal tweak surface
