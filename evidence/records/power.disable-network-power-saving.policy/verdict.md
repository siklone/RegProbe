# power.disable-network-power-saving.policy

- Class: `A`
- Pipeline: `v3.2`
- Official doc: `true`
- Cross-layer: `true`
- Cross verification: `insufficient`
- Layer set: `official_doc`
- Tools: `official-doc`

This child record keeps only the documented DisableTaskOffload and SystemResponsiveness values. SystemResponsiveness is supported here for path plus rounding/clamping behavior; the opaque NetworkThrottlingIndex write remains outside this child in the deprecated parent audit trail.

## Current verdict

DisableTaskOffload and the narrowed SystemResponsiveness claim are documented and machine-checkable. This record now treats the MMCSS page as proof of path plus rounding/clamping behavior, not as proof of the unresolved opaque NetworkThrottlingIndex value.
