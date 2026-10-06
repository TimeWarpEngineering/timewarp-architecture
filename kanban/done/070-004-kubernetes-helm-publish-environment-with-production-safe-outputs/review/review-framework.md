# Review framework

## Budget (by-diff)

- Lines changed: 950
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 070-004

**Date:** 2026-10-06
**Host task:** kanban/to-do/070-004-kubernetes-helm-publish-environment-with-production-safe-outputs/
**Diff scope:** branch `task/070-004-kubernetes-helm-publish-environment-with-productio` vs `master` (commit 4efb065d9)
**Plan / brief:** task.md Requirements 1–6 — Kubernetes Helm publish target, production-safety chart test, postgres StatefulSet/PVC, ingress decision (a), CI publish check.
**Effort:** 3 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, ganda task work headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
