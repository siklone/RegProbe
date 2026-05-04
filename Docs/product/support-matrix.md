# Support Matrix

This page summarizes the public support surface: what ships, what is packaged, and what the repo treats as supported versus research depth.

## Release Artifacts

| Artifact | Status | Purpose | Notes |
|--------|--------|---------|-------|
| `RegProbe-Portable-<version>-win-x64.zip` | Default desktop artifact | Desktop app with the shipped Tweaks, Recovery, and Diagnostics surfaces | Includes bundled docs and ElevatedHost output expected by the app |
| `RegProbe-Cli-<version>-win-x64.zip` | Supported | Scripted and audit-friendly CLI workflows | Mirrors the same SAFE bias and research gating as the desktop app |
| `RegProbe-<version>-win-x64-sha256.txt` | Required with every release | Integrity verification for release artifacts | Compare local hashes before running the package |

## Product Surfaces

| Surface | Checked-in status | Safe apply path | Rollback expectation |
|--------|----------------|-----------------|----------------------|
| `Tweaks` | Shipped | Supported for settings that are promoted into the app surface | Explicit rollback story remains visible before and after mutation |
| `Recovery` | Shipped | Supported for rollback and cleanup actions | First-class part of the product, not an afterthought |
| `Diagnostics` | Shipped | Read-only context, logs, and support information | No mutation path by itself |
| `CLI` | Supported | Scripted detect, plan, apply, verify, rollback, and research utilities | Same SAFE expectations as the desktop app |

## Windows Coverage

RegProbe runs on modern Windows 10 and Windows 11 systems, but evidence freshness is record-specific rather than one global guarantee.

| Coverage layer | Checked-in public position |
|--------|--------------------------|
| Host OS support | Windows 10 and Windows 11 |
| Evidence-backed build references seen across checked-in records | Commonly `22631`, `26100`, and newer active-lane Windows 11 builds such as `26200` where individual records explicitly say so |
| Shipping decision | A setting can still remain blocked or research-only even if the app can technically show it |

Use the per-record proof and build notes in the research docs whenever a setting claim depends on a specific Windows build.

## Status Language

| Status | Meaning |
|--------|---------|
| `Recommended` | Sufficient proof and rollback confidence for a normal app-facing path |
| `Experimental` | Promising, but still under validation |
| `Research-only` | Useful to keep in the repo, not ready for normal apply |
| `Blocked` | Control surface known, runtime proof still incomplete |
| `Archived` | Kept so the same dead end is not rediscovered later |

## Public Boundaries

- The desktop app is the checked-in user-facing surface.
- The repo still carries broader research, audit, ETW, VM, and static-analysis depth behind that surface.
- Release artifacts should never promise more than the shipped product or checked-in evidence model can support.
