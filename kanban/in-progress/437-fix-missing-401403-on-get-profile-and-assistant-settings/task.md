# Fix missing 401/403 on GET profile and assistant settings

## Description

`GET /api/Users/Current/Profile` documents only 200 while `PUT` documents
401+403. Same for `GET /api/settings/assistant` (only 200) vs `PUT`
(401+403). Either the GETs are intentionally anonymous (surprising for
Current/Profile) or the spec is missing responses.

## Goal (implementer restatement)

The published OpenAPI document lists only 200 on `GET /api/Users/Current/Profile` and
`GET /api/settings/assistant`, while the matching PUTs list 401 and 403. That reads as a
missing response. The contracts already choose anonymous on purpose: profile GET is the
dual-mode demo read, and assistant GET is the name shown on every Ask button. Keep those
GETs anonymous. Say that choice on the operation itself, with the existing
`[EndpointAllowAnonymous]` reason, so a spec reader does not treat a missing 401 as an
oversight. Do the same audit for the other GETs that omit 401. Add 401 and 403 only where
the handler actually returns them (the Entra challenge). Leave cookie- and bearer-protected
operations on the responses FastEndpoints already emits.

## Checklist

- [x] Decide anon-vs-authed for both GETs
- [x] Add 401/403 responses if authed, or document anonymous intent if not
- [x] Audit remaining GETs without 401 (Hello, entra/challenge, entra/choice, session, sign-in-methods, tip, antiforgery-token)

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)
- Implementation: grok task-work (2026-10-29). `ganda repo audit` exited 0. Left the advisory `kanban-landed` warning (claims 070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244 have no worktree). `--fix` lands other kitchens, so it stays off this task. Version stays 2.0.0-beta.29 (already one prerelease ahead of NuGet).
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session 01a1850c-bc4a-7002-a034-105ec5a88eda, max-turns 500, Done — 2026-10-29T00:47:35Z
- Review oracle round 1: Claude Opus 5.5 (claude-opus-5-5), effort 2, roster general. 1 open suggestion (M1), 0 bugs. Fix pass requested. Files: `review/review-framework.md`, `review/round-1/merged.md` (2026-10-29)
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session 7d5638ef-94e4-4a9f-b7f9-28366648e095, max-turns 120 — 2026-10-29T00:50:44Z
- Review oracle round 2: Claude Opus 5.5 (claude-opus-5-5), effort 1, roster general, turn cap 80. The delta since round 1 was empty (HEAD = `0c6ffb48`). M1 is still open. Fix pass requested. Files: `review/round-2/general.md`, `review/round-2/merged.md` (2026-10-29)
- Review oracle round 3: Claude Opus 5.5 (claude-opus-5-5), effort 1, roster general, turn cap 80. Delta empty (HEAD `0c6ffb48`). implement-fix-2 and implement-fix-3 each ran `num_turns: 1` with no tool calls. M1 still open. Escalated to the operator, because the implementer is not acting. Files: `review/round-3/general.md`, `review/round-3/merged.md` (2026-10-29)
- Review oracle round 4 (the walk resumed at the review-3 node): Claude Opus 5.5 (claude-opus-5-5), effort 1, roster general, turn cap 80. The delta was empty (HEAD `0c6ffb48`), and no operator ruling was recorded. M1 is still open. Fix pass requested. Files: `review/round-4/general.md`, `review/round-4/merged.md` (2026-10-29)
- implement-fix-4: Claude Opus 5.5 (claude-opus-5-5). Fixed M1: the GetProfile `[EndpointAllowAnonymous]` reason is spec-reader prose with no task ids. `OpenApiAnonymousIntent_` 6/6 passed, `dev build` 0 warnings 0 errors, `ganda repo audit` exit 0 (the same advisory `kanban-landed` warning). (2026-10-29)
- Review oracle round 5 (review-4 node): Claude Opus 5.5 (claude-opus-5-5), effort 2, roster general, turn cap 120. Delta `0c6ffb48..75b50b45`. M1 is verified fixed, and the reviewer re-ran `OpenApiAnonymousIntent_` (6/6 passed). No new findings. Disposition: clean. Files: `review/round-5/general.md`, `review/round-5/merged.md`, `review/disposition.md` (2026-10-29)

## Results

Both GETs stay anonymous. `GetProfile` is the dual-mode demo read (anonymous mock, store-backed when a principal exists). `GetAssistantIdentity` is the name on every Ask button, including signed-out pages. Their PUTs stay `profile.write` and `settings.write`, so FastEndpoints keeps 401 and 403 there.

Generated `[EndpointAllowAnonymous]` reasons are copied onto the OpenAPI operation as `Intentionally anonymous. …`. A public operation no longer looks like a dropped 401. Authorized operations are unchanged.

Audit of the other GETs that omit 401:

| GET | Decision |
| --- | --- |
| `/api/Hello` | Anonymous sample. Description carries the reason. No 401. |
| `/api/identity/entra/choice` | Anonymous. The visitor has no session yet. No 401. |
| `/api/identity/session` | Anonymous ambient read. `IsAuthenticated=false` is a success. No 401. |
| `/api/identity/sign-in-methods` | Anonymous. The login page has no session. No 401. |
| `/api/tip` | Anonymous wallet tip. No 401. |
| `/identity/sign-out/antiforgery-token` | Anonymous. A signed-out browser gets a token. No 401. |
| `/api/identity/entra/challenge` | Anonymous at the gate so bootstrap can start. Link mode returns 401, sign-in disabled returns 403, a bad mode returns 400, and an unlisted method returns 404. Those statuses are now on the operation. |

### How to validate

Smoke: from `tests/analyzers/timewarp-architecture-sourcegenerator-tests`, `dotnet test -c Release -- --filter-class FastEndpointSourceGenerator_OpenApi_Tests`. From `tests/container-apps/web/web-server-integration-tests`, `dotnet test -c Release -- --filter-class OpenApiAnonymousIntent_`.

Expect: OpenAPI tests 2 passed. Anonymous-intent tests 6 passed. `GET /api/Users/Current/Profile` and `GET /api/settings/assistant` descriptions contain `Intentionally anonymous.` and omit 401 and 403. The matching PUTs include 401 and 403. Hello, entra/choice, session, sign-in-methods, tip, and the antiforgery-token GET state the anonymous reason and omit 401. `GET /api/identity/entra/challenge` states the anonymous reason and documents 401 and 403.

### Review

Round 1 (effort 2, general): 0 bugs and 1 open suggestion. M1: the GetProfile anonymous reason is now published in OpenAPI, and it contains internal task ids. There is no disposition yet, and a fix pass is pending. Ledger: `review/round-1/merged.md`.

Round 2 (effort 1, general): no product commits since round 1. M1 is still open (1 suggestion, 0 bugs). There is no disposition yet, and a fix pass is pending. Ledger: `review/round-2/merged.md`.

Round 3 (effort 1, general): again no product commits (HEAD `0c6ffb48`). Both fix passes returned `Done` after one model call and no tool calls, so the implementer did no work. M1 is still open (1 suggestion, 0 bugs). There is no disposition yet. An operator needs to check the implementer harness, fix M1 by hand, or mark it wontfix. Ledger: `review/round-3/merged.md`.

Round 4 (effort 1, general): the walk resumed with no product commits and no operator ruling. M1 is still open (1 suggestion, 0 bugs). There is no disposition yet, and another fix pass is requested. If that pass is also a no-op, the next round parks the walk again. Ledger: `review/round-4/merged.md`.

Fix pass after round 4: M1 fixed. The GetProfile anonymous reason in OpenAPI now reads "This read is dual-mode: an anonymous caller gets the demo profile, and a signed-in caller gets their stored profile." The task history stays in the Design region.

Round 5 (effort 2, general): reviewed the fix delta `0c6ffb48..75b50b45`. M1 is verified fixed: `OpenApiAnonymousIntent_` passed 6/6 on a re-run, and no other published anonymous reason contains a task id. No new findings.

**Review disposition: clean.** 5 rounds, general reviewer only. Final counts: 0 bugs, 1 suggestion (fixed), 0 nits. 0 open and 0 wontfix. Ledger: `review/round-5/merged.md`. Disposition: `review/disposition.md`. Framework: `review/review-framework.md`.

## Review

- Round 1: Blocked — open M1
- Round 2: Blocked — open M1
- Round 4: Blocked — open M1
- Round 5: Done
## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 2026-10-28.

## Blocked

2026-10-29: stalemate — operator decision needed: implementer-grok fix passes are no-ops (num_turns 1, no tool calls, no commits); check the implementer harness or fix/wontfix the carried suggestion by hand

2026-10-29: resolved. implement-fix-4 (Claude) fixed M1 in `75b50b45`, and the round-5 disposition is clean.
