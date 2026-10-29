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

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 2026-10-28.
