# Review framework

## Budget (by-diff)

- Lines changed: 701
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 350

**Date:** 2026-10-13
**Host task:** kanban/to-do/350-passkey-sign-up-and-sign-in-under-interactiveserver-never-reach-the-browser-session-cookie/
**Diff scope:** branch task/350-passkey-sign-up-and-sign-in-under-interactiveserve vs master (187cbbb14)
**Plan / brief:** Option B: session-issuing ceremony completions go out from the browser (web-authn.ts SendJson → BrowserFetchHttpMessageHandler under HttpApiService) and then navigate with forceLoad.
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5); session id not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
