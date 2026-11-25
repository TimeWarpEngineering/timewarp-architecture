# Publish session-cookie and per-operation security requirements in OpenAPI

## Description

The published document has one security scheme, `JWTBearerAuth`, and no root `security`. 58 of 79
operations have no `security` key. 34 of them are browser-session (cookie) routes that answer `401`
with an empty body and no `WWW-Authenticate` (for example `GET /api/Roles`, `GET /api/settings`,
`PUT /api/Users/Current/xai-api-key`, `POST /api/admin/feedback/{feedbackItemId}/replies`). A
generated client or agent reads them as anonymous. Operations that accept both the session and the
agent token (for example `GET /api/Feedback/attachments/{attachmentId}`, the `credential:manage`
routes) publish only the bearer requirement and say "Requires an agent bearer token".
`open-api-bearer-security-server.cs` deliberately leaves cookie-only operations alone.

## Requirements

- Add a session-cookie security scheme (`type: apiKey`, `in: cookie`, the identity-session cookie
  name) whose description says it comes only from the browser passkey or Entra ceremony.
- Emit each operation's `security` from the endpoint's authentication schemes: session only → the
  session requirement; agent token → `JWTBearerAuth` with its scopes; both → two alternative
  requirement objects. Fix the "Requires an agent bearer token" sentence to match.
- Emit `security: []` on intentionally anonymous operations, so Redocly `security-defined` reports
  0 errors.
- Add a document test: every operation that documents `401` has a security requirement (allowlist:
  the anonymous Entra challenge ceremony).

Owner: transformer: `source/foundation/foundation-server/open-api-bearer-security-server.cs` (or a sibling session transformer).

## Checklist

- [ ] Implement the requirements above
- [ ] Tests or document checks named above pass

## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-02 (Grok G-001, Fable F-003, F-012) in `kanban/.../458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`
  (lands with task 458); reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.
- Post-deploy verification: re-fetch the live OpenAPI document after deploy and confirm the finding is gone.
- Grooming note: pass `IdentitySessionDefaults.CookieName` from `web-server/program.cs` (like `OpenApiAgentBearer`) since `foundation-server` cannot reference `identity-host`; update `open-api-bearer-security-tests.cs` and `open-api-payment-responses-tests.cs` where they currently assert `security` is omitted.

## Session

- Created: 2227132 (2026-11-24)
