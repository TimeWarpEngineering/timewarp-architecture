# Round 2 — general
**Date:** 2026-10-29
**Scope reviewed:** fix delta `3d5ab53f3..HEAD` (round-1 `base.sha` to `191d64c14`), product paths `agent-link-onboarding-prompt.cs`, `AgentLinksPage.razor`, `agent-link-onboarding-prompt-tests.cs`, `agent-links-onboarding-playwright-tests.cs`

## Summary

The fix pass closes all three round-1 findings. The Playwright onboarding test now GETs the
`/llms.txt` and `/auth.md` URLs that the rendered prompt names and requires HTTP 200. Both
paths are served (see `platform/agent-discovery/markdown-content-negotiation-tests.cs`). The
prompt, its Design region, and the page caption now say the blocks are bash and do not run in
PowerShell, and they limit the curl alias remark to Windows PowerShell. The integration test
pins the full `b64url_decode` line, which matches `agent-link-onboarding-prompt.cs:57`. The
goal restatement still matches the task; the round-1 fix items were added to it, and that is
not a misread.

Re-run here: `Build_Should` in `web-spa-integration-tests` passed 1/1
(`TIMEWARP_TEST_PORT_BASE=43300`, `--filter-class Build_Should`, Release). The Playwright
test was not re-run in this round. The implementer reports 1/1.

## Prior findings

- M1 (suggestion): fixed. `RequireHttp200Async` GETs both prompt URLs through `page.APIRequest`.
- M2 (nit): fixed. The wording gives the bash reason and limits the alias remark to Windows PowerShell. The tests match.
- M3 (nit): fixed. The assertion is the full helper line.

## Issues

None new in the fix delta.
