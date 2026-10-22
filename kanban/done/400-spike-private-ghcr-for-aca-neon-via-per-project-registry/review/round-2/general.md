# Round 2 — general
**Date:** 2026-10-22
**Scope reviewed:** fix delta `94b1b38f1..28369a335` (`skills/tw-deploy/references/azure-container-apps-neon.md`
and task.md). Prior round-1 IDs re-verified against the post-fix tree.

## Summary

The fix pass rewrote "Why the registry stays ACR" as Aspire 13.6.2 facts and linked the
container-registry article. The run log, the `/tmp` path, and the maintainer's name are gone, and
task ids stay as parentheticals. `https://aspire.dev/app-host/container-registry/` returns 200. It
is titled "Container registry configuration" and contains `WithContainerRegistry(environment)`.
M1 and M2 are fixed. The present-tense rewrite turned one conditional into a false current claim
about `dev deploy` (M3).

`dev lint-skills` did not run here. `dev` is not on PATH, and the runfile refused because vally
0.6.0 is installed and the repo pins `@microsoft/vally-cli@0.19.0`. The edit is prose in a
`references/` file with no frontmatter or eval change.

## Prior findings

- M1 (suggestion): fixed. `rg` for the round-1 log phrases finds nothing.
- M2 (nit): fixed. The link is live and matches the cited example.

## Issues

### Issue 1 — Severity: suggestion
- File: skills/tw-deploy/references/azure-container-apps-neon.md:99
- Description: "A token a later Aspire release can use lives as an AppHost user secret. `dev deploy`
  copies it into the aspire process the way it copies the Neon strings". That reads as current
  behavior. No such secret or copy exists: `rg -i "ghcr|github.*token" tools/dev-cli` finds
  nothing. Round 1 had "would live" / "would copy", which is a conditional design, not a past-tense
  log. M1 did not ask for that wording to change. A generated-app operator could believe
  `dev deploy` already forwards a GHCR token.
- Suggestion: State it as the intended design, for example "If a later Aspire release removes the
  ACR, the token belongs in an AppHost user secret, and `dev deploy` should copy it into the aspire
  process the way it copies the Neon strings". Alternatively, say plainly that `dev deploy` does not
  handle a registry token today.
- Status: open
