# Product Docs

This folder is the quickest public path into RegProbe.

- [User guide](user-guide.md)
- [Support matrix](support-matrix.md)
- [Product media](media.md)
- [10-minute user test](10-minute-user-test.md)

The goal here is clarity: what the app does, what it does not do, what a safe session looks like, and how the product explains trust without asking a new reader to absorb the whole research pipeline first.

Contributor Lab is not part of the normal end-user flow. In repo/dev builds it
provides a gated Windows-first workspace for contributors to inspect readiness,
copy canonical Python commands, and review research-only observations without
promoting them into shipped cards.

For user-supplied key/value research, Contributor Lab presents the workflow as
ordered evidence discovery steps: repo/evidence lookup, existing app-card QA
mapping, app readiness/contracts, certified VM health, and one-value VM
experiments. The app does not turn a clean observation into an end-user card
until current/default/target, rollback, app write, low-noise proof, and bounded
claim text are all present.

The app-side runner is intentionally narrow: it can run Contributor Lab's own
allowlisted read-only lookup/readiness commands and show the output inline.
Registry mutation, reboot, benchmark, and campaign commands stay copy-only and
belong in an explicit disposable VM session.
