# Review Round 1 — merged findings

Reviewer: general-purpose (sonnet), effort 1. Diff: commit `cf4266b4`.

| # | Severity | File | Finding | Status |
|---|----------|------|---------|--------|
| F1 | minor | tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs:67 | `WaitForResourceAsync(..., TerminalStates)` discards the returned state; a FAILED migration (FailedToStart/Exited are terminal) would sail through and surface later as confusing schema/auth errors in the DB-backed facts | **fixed** — state captured and asserted `ShouldBe(Finished)` with explanatory message |
| F2 | nit | tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs:49 | CTS comment said the 2-minute budget "covers all three" health waits; the diff added a 4th (migration wait) and the reachability poll also consumes it | **fixed** — comment enumerates all gates |

Clean areas verified by reviewer: template-flag correctness (edit stays inside existing
`#if postgres`; removed local was the only `EFMigrationResource` use; bare fluent chain legal;
no TWA0010 exposure), Design-region/comment truthfulness (API names verified by decompile),
docs consistency (ADR original D6 intentionally preserved as history with amendment below;
repo-wide grep found no other stale wait-edge claims), timeout budget adequacy.

Re-run after fixes: aspire-tests 6/7 — sole failure is the pre-existing 401-vs-403
(task 158), unchanged.
