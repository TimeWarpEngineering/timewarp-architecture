# Round 3 — fix verification (commits effa18a1 + 095bb0f0, merged to dev)
**Date:** 2026-08-02
**Sources:** fix agent (clean worktree, triple smoke) + orchestrator re-run on merged dev

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 1 | 0 |

## Issues

### R2-1 — bug — fixed (option b, decided by evidence)
- Census: 19 classes / 21 call sites; 100% web-only-meaningful (zero Graph.Api uses; the BFF
  ApiServiceName HttpClient is consumed only client-side by the SPA). Added
  HostGraphFactory.CreateWebAsync (#if(web)); every call site branches #if(api)
  CreateWebWithApiAsync #else CreateWebAsync. hello-tests.cs had the same dependency and is
  now web-only correct. TWA0010 satisfied (api symbol added to the two csprojs + hello's
  DefineConstants); TWA0008 caught literal directive tokens in Design prose (reworded).
### R2-2 — bug — fixed: 4 orphaned xUnit-family CPM pins removed again; audit cpm-consistency PASS.
### R2-3 — suggestion — fixed: hello-tests.cs added to CoLocatedTestFiles ("web"); comments reconciled.

## Gate results

Fix worktree (agent): build 0/0; full dev test green (suite 95+2 skip, parity exact); hello
2/2 standalone incl. inside a generated --api false app; smoke ×3 green in THREE separate
full runs; audit 23/23.
Merged dev (orchestrator re-run): build 0/0; audit 23/23 (after clearing stale pre-145-003
template-smoke artifacts that false-failed cpm-consistency — ganda task 197 filed for the
artifacts-scanning gap); template-smoke SUCCEEDED ×3 — SmokeNoApi: hello 2/2 standalone,
web aggregator 7/7 web-only, api suites correctly excluded. (One diagnosis cycle lost to the
stale bin/dev footgun — the old binary's harness expected web=5; fresh self-install resolved.)
