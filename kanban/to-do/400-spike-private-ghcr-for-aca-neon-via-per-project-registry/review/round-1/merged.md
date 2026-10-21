# Round 1 — merged findings
**Date:** 2026-10-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: skills/tw-deploy/references/azure-container-apps-neon.md:23
- Description: The shipped skill section "Why the registry stays ACR" reads as a past-tense spike
  log. Examples: "Task 400 published that shape and stopped", "No GitHub token was created",
  "Probe (removed …; Steve decides …)", the `/tmp/task-400-…` output path, and "`aspire deploy`
  was not run", "That callback was not added", "This spike did not add that secret". `skills/` ships
  in generated apps and holds current facts. The run narrative is already in task.md Results.
- Suggestion: Rewrite in present tense as facts about Aspire 13.6.2. Keep the API table, the
  default-ACR mechanics, the pull shape with the Bicep excerpt, the token scopes and where the token
  would live, the recommendation, and the revisit condition. Parenthetical task-id provenance is
  fine. Drop the probe and run log and the maintainer's name.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: skills/tw-deploy/references/azure-container-apps-neon.md:39
- Description: The text names the "Container registry configuration" docs article without a link.
  The link was removed in `c58e4ef1c` because the URL returned 404, so the claim cannot be checked.
- Suggestion: Cite a reachable aspire.dev page, or drop the sentence. The point holds without it.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None.
