# Review framework — task 053-004

**Date:** 2026-09-11
**Host task:** kanban/in-progress/053-004-stable-public-apiroute-attributes-for-incremental-mixins/
**Diff scope:** branch `task/053-004-stable-public-apiroute-attributes-for-incremental` vs `origin/master` (commit `2b359cc0`)
**Plan / brief:** Emit `[ApiRoute]` / `[AuthApiRequest]` / `[OpenDataQueryParameters]` as public types in `TimeWarp.Foundation.Features` via `RegisterPostInitializationOutput`; discover with `ForAttributeWithMetadataName`; FastEndpoint/ingress/TWA0006 match FQN. Out of scope: 053-003 parser, 053-005 SyntaxProvider tightening, 053-006 fewer members, 006-001 referenced-assembly walk.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle Grok session `01a08ed1-1284-71d2-aa1c-8e9a336ca9f5` (2026-09-11)

Round 2 (2026-09-11): re-verify M1/M2 against the post-fix uncommitted delta (FastEndpoint + ingress foreign-attribute tests; ingress Design region). Round 1 files are frozen.

Round 3 (2026-09-11): host re-walk of the review oracle after implement=Done. Scope is the full branch vs `origin/master` through `d2f33a9c`, plus the post-disposition product delta (`skills/tw-web-api-contracts/evals/eval.yaml` hyphenated grader name). Carry M1/M2 as `fixed`. Do not clobber rounds 1–2. Session: review oracle Grok `01a08eea-52e7-71a3-93e1-a255e912ed33`.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
