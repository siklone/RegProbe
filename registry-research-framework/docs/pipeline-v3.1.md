# Pipeline v3.1

v3.1 keeps the current research record schema and adds a machine evidence layer under `evidence/{tweak-id}/`.

- Official Microsoft sources can still carry a record to `A`.
- Non-official records are re-audited through runtime, static, and behavior phases.
- Heavy raw artifacts stay out of git and are referenced through `artifact_refs.release_url`.
