# Public Repo Boundary

This repository is public and forkable, so checked-in files must help one of
the supported audiences: end users, contributors, CI, or audit reviewers.
Everything else should stay local until it is sanitized and promoted through a
documented artifact lane.

## Keep In Git

- Product source, app cards, rollback logic, and tests required by CI.
- Contributor scripts that are the canonical API for research, app QA, VM
  validation, and agentic AI workflows.
- Small, sanitized JSON/Markdown artifacts that explain current app state,
  evidence gates, run tiers, or cleanup decisions.
- Historical artifacts that are still referenced by docs, tests, records, or
  promotion gates.
- Fixtures needed to make forks and PR validation reproducible.

Tests belong in the public repo when they protect a public contract. They are
not just local developer helpers; they let forks, CI, and future contributors
know whether app cards, evidence drawers, rollback contracts, and VM tooling
still behave as promised.

## Boundary Decision Matrix

| Item | Public git? | Why |
| --- | --- | --- |
| App source, card manifests, rollback providers | yes | End users and forks need the shipped product behavior. |
| Python contributor API scripts | yes | Contributors and agentic AI use these as the stable automation surface. |
| CI/unit/integration tests | yes | They protect public contracts and stop forks from silently breaking app/evidence behavior. |
| Small sanitized JSON/Markdown audit summaries | yes | They explain current decisions without forcing readers into raw parse folders. |
| Test fixtures with no secrets or machine identity | yes | They make tests reproducible across forks and CI. |
| Raw ETL/PML/WPR/Ghidra output | no by default | Too large/noisy/machine-specific; promote only a small normalized artifact when needed. |
| VM disks, ISOs, snapshots, memory dumps | no | Machine-local state with size, licensing, and privacy risk. |
| Noisy benchmark scratch output | no | Useful for local debugging, not reference evidence. |
| One-off maintainer scripts | no by default | Promote only if they become a documented contributor workflow. |
| Secrets, tokens, private URLs, passwords | never | Must stay out of git entirely. |

When a file serves both local debugging and public audit, split it: keep the raw
capture local, commit a small normalized summary with source metadata, and link
that summary from the relevant ledger or evidence record.

## Keep Local Only

- Plaintext credentials, VM passwords, tokens, private URLs, and personal
  machine paths.
- Local VM disk images, ISOs, snapshots, memory dumps, crash dumps, and
  unsanitized host or guest logs.
- Raw ETL/PML/WPR/Ghidra dumps unless a small normalized artifact is explicitly
  promoted for audit.
- Noisy benchmark scratch output, failed experiment scratch folders, and
  personal retest captures that have not been reviewed.
- One-off helper scripts that are useful only on one maintainer machine.

Use `.gitignore` for repeatable local-only paths. If a local artifact later
becomes important, first sanitize it, attach metadata, and add a reviewed
record or ledger entry before committing it.

## Promotion Path For Local Artifacts

Local-only does not mean lost. If a local capture becomes important evidence:

1. Normalize it into a small JSON/Markdown artifact with no secrets, hostnames,
   personal paths, or huge binary payloads.
2. Add `source_kind`, run tier, timestamp, environment summary, and claim
   boundary fields.
3. Reference the normalized artifact from the app-surface review, evidence
   ledger, or cleanup ledger.
4. Add or update tests only for the public contract, not for the private raw
   capture.
5. Commit the normalized artifact, not the original local dump.

## Deletion Rule

Deletion is ledger-first, never vibes-first:

1. Add the file or folder to a cleanup quarantine ledger.
2. Prove `rg` finds zero live references, or record every live reference that
   must migrate first.
3. Name the replacement artifact or explicit obsolete reason.
4. Run the app/readiness/tests that cover the affected surface.
5. Delete only after the ledger says it is delete-eligible.

If a candidate still has live references, it is not delete-eligible. Either
migrate those references or move the item into a retained inventory with a
clear reason.

## Naming Rule

Do not turn temporary campaign IDs into product language. For example,
`operator96` is a legacy artifact/campaign ID for the first 96-record seed
batch used to validate the custom registry value experiment pipeline. Public
and app-facing copy should say `custom registry value experiments` or
`user-supplied key/value experiments`; only low-level artifact paths need the
legacy ID.
