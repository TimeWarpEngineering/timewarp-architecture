# Review framework

## Budget (by-diff)

- Lines changed: 581
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-28
**Host task:** kanban/in-progress/536-001-dual-otlp-export-so-aspire-run-mode-services-export-to-both-aspire-dashboard-and-sig-noz/
**Diff scope:** branch `task/536-001-dual-otlp-export-so-aspire-run-mode-services` (HEAD 9674f707a) vs merge-base with `master` (1d86386de)
**Plan / brief:** task.md Requirements 1–3 and `## Goal (implementer restatement)`: run-mode AppHost forwarding of `Observability__SecondaryOtlp__*`, ServiceDefaults per-signal primary + named `secondary` exporters, tests.
**Effort:** 3 (by-diff budget); roster axis general only
**Reviewer roster:** general (review oracle, single pass)
**Session IDs:** review oracle claude-opus-5-5 (ganda task work, print mode)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to
  the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2 scope

**Date:** 2026-11-28
**Diff scope:** fix-pass delta `9674f707a..5ea5b31a7` (HEAD 5ea5b31a7)
**Effort:** 2 (by-diff budget, 581 lines); roster axis general only
**Reviewer roster:** general (review oracle, single pass)
**Session IDs:** review oracle claude-opus-5-5 (ganda task work, print mode)
