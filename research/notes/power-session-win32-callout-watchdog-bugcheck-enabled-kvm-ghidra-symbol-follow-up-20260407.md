# power.session-win32-callout-watchdog-bugcheck-enabled KVM Ghidra symbol follow-up - 2026-04-07

## Summary

- A canonical PDB-backed full-analysis Ghidra pass on the KVM current build completed successfully for `ntoskrnl.exe`.
- The pass staged the matching `ntkrnlmp.pdb` and confirmed that the current build still carries the exact Unicode string `Win32CalloutWatchdogBugcheckEnabled`.
- The string resolved at `140c7ba30`, but Ghidra recovered `0` direct or bounded references for it in this canonical pass.
- The structural result is therefore narrower than the earlier draft language: the sibling still exists in the current build, but this pass did not produce a usable caller or seeding path.

## Source artifacts

- `evidence/files/vm-tooling-staging/watchdog-win32-callout-ghidra-20260407b/summary.json`
- `evidence/files/vm-tooling-staging/watchdog-win32-callout-ghidra-20260407b/evidence.json`
- `evidence/files/vm-tooling-staging/watchdog-win32-callout-ghidra-20260407b/ghidra-matches.md`
- `evidence/files/vm-tooling-staging/watchdog-win32-callout-ghidra-20260407b/ghidra-run.log`
- `evidence/files/vm-tooling-staging/watchdog-win32-callout-ghidra-20260407b/host-review.json`

## Interpretation

- new proof gained:
  - current-build PDB-backed full-analysis confirmation that the value-name string still exists in `ntoskrnl.exe`
  - explicit structural no-xref outcome for the exact string in the canonical pass
- narrowed conclusion:
  - the current build still carries the sibling name, but this Ghidra pass did not recover a registry reader, seeding caller, or bounded xref path from it
- remaining gap:
  - identify a real current-build caller through a live symbol/global path rather than relying on string proximity
