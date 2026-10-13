# Disposition — task 345

**Date:** 2026-10-13
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Effort-3 review with a general reviewer over the branch diff. Round 1 found 10 issues: 6 suggestions and 4 nits, no bugs. Eight were fixed in 4181c4291 (analyzer shape gaps, Policy required, search error handling, session reset of picker state, word-start `@`, trailing space, Design notes) and two were left as wontfix. Round 2 confirmed the fixes and raised three new nits. One was fixed (a null-body response) and two were left as wontfix (timing windows with no cross-user data exposure). Gates after the fixes: `dev build` 0/0, analyzers 222/222, sourcegenerator 108/108, web-spa-integration 224/224, Playwright AskSurface 1/1 and AskSignIn 2/2, `ganda repo audit` passes.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M3 | suggestion | One contracts assembly in the template; cross-assembly duplicate prefixes are not a supported layout yet | review orchestrator |
| M10 | nit | Race test needs a gated fake API the SPA test host lacks; 6 analyzer cases were added; the remaining branches are trivial early returns | review orchestrator |
| M12 | nit | Small timing window on the word-start check; the user can retype `@`; better than the focus stealing it replaced | review orchestrator |
| M13 | nit | Small window; the stale list holds type names only and the server re-authorizes every search | review orchestrator |

## Escalations

- None.
