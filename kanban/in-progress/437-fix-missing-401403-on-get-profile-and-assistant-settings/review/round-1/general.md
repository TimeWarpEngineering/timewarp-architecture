# Round 1 — general
**Date:** 2026-10-29
**Scope reviewed:** implement commit `f6ea01eed` (generator, endpoint metadata, Entra challenge and antiforgery-token endpoints, generator tests, new web-server OpenAPI integration test), plus the contracts whose reasons now reach the document.

## Summary

The change copies the `[EndpointAllowAnonymous]` reason into the generated OpenAPI description as `Intentionally anonymous. …`. Profile GET and assistant GET stay anonymous, which matches what both contracts already decided. The two hand-written anonymous GETs say the same thing. The Entra challenge lists 400/401/403/404, and those match its handler: `Send.NotFoundAsync` and `Send.UnauthorizedAsync` send no body, and the 400 and 403 paths write ProblemDetails (`IEntraSignInPolicy` refuses only with `SignInDisabled`). `AnonymousReason` takes part in `Equals` and `GetHashCode`, so incremental caching stays correct. When there is no XML summary, the anonymous-only path does not invent a 400.

Verified: `tests/analyzers/timewarp-architecture-sourcegenerator-tests` `dotnet test -c Release` 154/154 passed; `tests/container-apps/web/web-server-integration-tests` `--filter-class OpenApiAnonymousIntent_` 6/6 passed. The restatement matches the task, which offered "intentionally anonymous" as one of its two outcomes. I found no misread.

The one problem: reason strings were written for reviewers, and the change now publishes them. That makes contract-reason wording part of the public document.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/features/profile/get-profile/get-profile-contracts.cs:28
- Description: The generator now publishes every `[EndpointAllowAnonymous]` reason verbatim in the OpenAPI description. The GetProfile reason ends with `Page stays PermissionIds.ProfileRead (task 148 D3 / 182-003).` That sentence is maintainer history: internal kanban task ids and a page-gate detail. So `GET /api/Users/Current/Profile` now shows `… Intentionally anonymous. Handler is deliberately dual-mode … (task 148 D3 / 182-003).` in the public spec at arch.timewarp.work, and in every generated app's spec. This is the exact operation the task set out to make clear to a spec reader. No other anonymous reason in `source/` carries task ids (checked with `grep -rn 'EndpointAllowAnonymous(' source`).
- Suggestion: Shorten the reason to the spec-facing part, for example: "Dual-mode read: an anonymous caller gets the demo profile, and a signed-in caller gets their stored profile." The task-id history is already in the contract's Design region (lines 18–22), so it can go. The integration test's `"dual-mode"` fragment still matches. Optionally, add one line to the `EndpointAllowAnonymousAttribute.Reason` XML doc saying the text is published in OpenAPI, so future reasons are written for spec readers.
- Status: open
