# Disposition — task 182 (permission-centric authorization)

**Status: PENDING — round 2 in progress. Do not cut children yet.**

## Review state

| Round | Reviewer | Artifact | Verdict |
|-------|----------|----------|---------|
| 1 | Claude (Fable 5) | `review/round-1/claude.md` | Accept with amendments (6 blocking, 7 non-blocking) |
| 2 | Grok | `review/round-2/grok.md` (expected) | — pending |

## Decision (so far)

User direction (2026-08-12): hold final disposition until Grok completes a second review pass
over the same inputs (`task.md`, `research/decision-brief.md`, current code hotspots) plus
Claude's round-1 review. Grok should confirm or contest the round-1 verdict, the answers to
decision-brief §7, and specifically the blocking amendments:

1. Phase 1 split into three sequential children (model / server swap / SPA swap + dead-code delete).
2. Single registry + single registration helper replacing both `AuthorizationConstants.Policies`
   and `AuthorizationPolicyNames`.
3. `IPermissionEvaluator` as the sole decision seam (claims projection only as internal
   optimization of the default evaluator).
4. ModuleRequirement / ModuleIds / AuthorizationState.Modules deleted in Phase 1, not Phase 4.
5. Lockout guards (last-admin + protected-core) ship with the Phase 2 editing UI.
6. Admin read/manage permission split in the seed vocabulary.

## After round 2

Fold both reviews into this file: per-section accepted / amended / rejected, permission id
format finalized, §7 answers closed. Then cut children with
`ganda kanban create "…" --parent 182`.
