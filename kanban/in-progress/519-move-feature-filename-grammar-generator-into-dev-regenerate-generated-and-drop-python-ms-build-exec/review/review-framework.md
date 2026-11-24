# Review framework — task 519

**Date:** 2026-11-24
**Host task:** kanban/in-progress/519-move-feature-filename-grammar-generator-into-dev-regenerate-generated-and-drop-python-ms-build-exec/
**Diff scope:** branch `task/519-move-feature-filename-grammar-generator-into-dev` vs merge-base `f47562750` (master). Head `d1823a79c`. 17 files, +656/−283. Product change is `a963f68ce`.
**Plan / brief:** Move the feature-filename grammar generator from the Python MSBuild `<Exec>` into `dev regenerate-generated`, keep the four committed outputs, and drop the build-time Python call.
**Effort:** 3 (by-diff, 939 lines). Roster is general only.
**Reviewer roster:** general
**Session IDs:** implementer-claude `ad1561c4-583c-4372-bdb6-452459f2f603`; reviewer grok `01a20d69-3125-7b32-8b69-5aba2c6dd85a`

## Budget (by-diff)

- Lines changed: 939
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
