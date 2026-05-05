# Security Policy

RegProbe can inspect and change Windows configuration, so security posture matters as much as feature coverage. This file explains how to report problems, what the elevated host is expected to do, and where the current trust boundaries sit.

## Reporting A Vulnerability

Please use GitHub's private vulnerability reporting flow if it is available for the repository. If private reporting is not available, open a minimal public issue without exploit details and clearly mark it as security-related so follow-up can move to a safer channel.

Include the smallest reproduction you can:

- affected commit, release tag, or branch
- Windows build and whether the issue happens on host, VM, or both
- whether the issue requires elevation
- the exact tweak, repair action, or script lane involved
- logs, screenshots, or trace artifacts with secrets removed

## Security Priorities

The main security goals for RegProbe are:

- do not mutate the system on startup or in the background
- keep elevated work separate from the main desktop process
- keep tweak execution bounded to explicit, user-triggered actions
- make rollback and verification first-class parts of system changes
- avoid shipping "research only" registry ideas as trusted user actions

## Elevated Host Boundary

The intended boundary is that `RegProbe.ElevatedHost` performs bounded operating-system actions that the app has already mapped and requested explicitly. That may include registry writes, file or directory changes used by repair flows, scheduled task changes, or service configuration changes that belong to a defined tweak or repair action.

The elevated host is not intended to behave like a general remote shell. It should not silently download payloads, accept arbitrary internet-sourced commands, or run background persistence unrelated to the requested tweak flow.

If you find a path where untrusted input can turn into arbitrary elevated execution, treat that as a security issue.

## Threat Model Notes

RegProbe is most predictable when used deliberately on a known machine or validation VM. The main risks we care about are:

- privilege boundary mistakes between the app and the elevated host
- incomplete rollback for settings that look reversible but are not
- shipping a tweak whose evidence overstates what Windows actually does
- unsafe documentation or scripts that encourage casual use of experimental lanes
- accidental secret exposure in VM scripts, logs, traces, or published artifacts

The repo also contains research tooling. Some of that tooling is intentionally sharp, especially around ETW, VM orchestration, runtime tracing, and static analysis. Those lanes are contributor tooling, not the public quick-start path.

## What Users Should Expect

- Preview first: the app should not apply changes before the user chooses to do so.
- Supported shipped actions should have a rollback story.
- Experimental or research-only lanes should stay out of the normal user flow.
- Security-sensitive changes should be documented in the repo rather than hidden behind vague release notes.

## Hard Boundaries For Public Docs

Public documentation should not:

- include plaintext VM credentials
- rely on workstation-specific absolute paths
- describe experimental runtime lanes as if they are normal end-user features
- imply a setting is safe or Microsoft-backed when the evidence is still incomplete

If you see any of those, that is worth reporting even if it is not a code execution bug.
