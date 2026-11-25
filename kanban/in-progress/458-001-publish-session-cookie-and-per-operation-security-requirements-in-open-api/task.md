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

## Goal (implementer restatement)

The published OpenAPI document must say which credential each operation takes. Today cookie-only
routes publish no `security` and read as anonymous, and dual routes publish only the bearer.

Acceptance:

- `components.securitySchemes` gains `IdentitySessionCookie` (`apiKey`, `in: cookie`, name
  `.timewarp.identity.session`), described as issued only by the browser passkey or Entra ceremony.
- Each operation's `security` comes from the endpoint's `AuthSchemeNames`: session only (or no
  schemes, since identity-session is the DefaultScheme) → session requirement; agent token →
  `JWTBearerAuth` with its scopes; both → two alternative requirement objects. The description
  sentence says "Requires …" or "Accepts … or …" to match.
- Anonymous verbs publish `security: []`, so every operation declares security (Redocly
  `security-defined`).
- A document test over every web-server endpoint: each operation has a `security` key, and each one
  that documents `401` has a non-empty requirement, except the Entra challenge.
- `open-api-bearer-security-tests.cs` and `open-api-payment-responses-tests.cs` stop asserting
  `security` is omitted.

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

- [x] Implement the requirements above
- [x] Tests or document checks named above pass

## Review

- Round 1: Done

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
- Implement (2026-11-26): found that FastEndpoints' document pass, which runs after operation
  transformers, writes every `AddAuth` scheme onto an endpoint whose `AuthSchemeNames` is null. Before
  this change that put `JWTBearerAuth` on authorized endpoints with no `AuthSchemes`. The schemes now
  go into components from our own document transformer, and `AddAuth` is not called.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session a71ad024-775d-4100-a707-04170ed8ba15, max-turns 500, Done — 2026-11-25T20:08:38Z
- Review oracle (2026-11-26): round 1, effort 3, roster general. Reviewer grok 4.7 (xAI), session 01a21453-33e4-7922-8c4a-7099963f9482, turn cap 200. Disposition clean, 0 open findings.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a21453-33e4-7922-8c4a-7099963f9482, max-turns 200, artifacts general.md, merged.md, disposition.md — 2026-11-25T20:17:52Z

## Results

What changed:

- `source/foundation/foundation-server/open-api-session-cookie.cs` (new): host-supplied session scheme
  name, cookie name, and whether it is the DefaultScheme.
- `source/foundation/foundation-server/open-api-operation-security-server.cs` (new): adds
  `JWTBearerAuth` and `IdentitySessionCookie` (`apiKey`, `in: cookie`, `.timewarp.identity.session`,
  described as issued only by the passkey or Entra ceremony) to components, and writes each operation's
  `security` from `AuthSchemeNames`: anonymous verb → `[]`; agent → bearer with scopes; session (or no
  schemes, since the session is the DefaultScheme) → session; both → bearer then session as
  alternatives. The description sentence is "Requires …" or "Accepts the browser session cookie
  (IdentitySessionCookie) or an agent bearer …".
- `open-api-bearer-security-server.cs`: keeps the bearer scheme, scopes, and 401/403 responses; the
  operation walk moved to the new transformer.
- `common-server-module.cs`: new optional `sessionCookie` parameter on `AddOpenApi`; no FastEndpoints
  `AddAuth`.
- `identity-host/open-api-session-cookie-registration-server.cs` (new) and `web-server/program.cs`:
  the web host passes `IdentitySessionDefaults.Scheme` and `.CookieName`.
- Tests: new `open-api-operation-security-tests.cs` over every web-server endpoint (every operation has
  a `security` key; every operation documenting 401 has a non-empty requirement except
  `GET /api/identity/entra/challenge`; scheme shape; `GET /api/admin/roles` session only;
  `GET /api/feedback/attachments/{attachmentId}` bearer + session). `open-api-bearer-security-tests.cs`
  now asserts session, dual, default-scheme, and `security: []`; its probe endpoints moved to
  `open-api-bearer-security-probes.cs` (500-line cap, TWA0038). `open-api-payment-responses-tests.cs`
  asserts `security: []` on the anonymous tip routes.
- No version bump: `2.0.0-beta.32` is not yet published (`dev check-version`: source is one increment
  ahead of `2.0.0-beta.31`), so the `AddOpenApi` addition ships in that pending release.

Gates (the web-server-integration-tests files are suite files, not standalone runfiles; each was run
with `./bin/dev test <file>`):

- `./bin/dev build` → pass, 0 warnings, 0 errors.
- `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-operation-security-tests.cs` → 6/6 pass.
- `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-bearer-security-tests.cs` → 15/15 pass.
- `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-payment-responses-tests.cs` → 6/6 pass.
- `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-anonymous-intent-tests.cs` → 6/6 pass (covers `CommonServerModule.AddOpenApi`).
- `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-discovery-coverage-tests.cs` → 6/6 pass (covers `CommonServerModule.AddOpenApi`).
- `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-agent-reviewer-guide-tests.cs` → 4/4 pass (reads `JWTBearerAuth` from components).
- `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-operation-tags-tests.cs` → 2/2 pass.
- `./bin/dev test tests/container-apps/api/api-server-integration-tests/features/open-api/open-api-document-tests.cs` → 2/2 pass (api-server `AddOpenApi`).
- `ganda repo audit` → passes; one advisory warning, `vscode-window-icon` (`.vscode/settings.json` is
  missing; that file is gitignored and local, so nothing to commit).

### How to validate

Smoke: `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-operation-security-tests.cs`,
then after deploy fetch `https://arch.timewarp.work/openapi/v1.json`.

Expect: 6/6 pass. In the live document, `components.securitySchemes.IdentitySessionCookie` is
`apiKey` in `cookie` named `.timewarp.identity.session`; `GET /api/admin/roles` has
`security: [{"IdentitySessionCookie": []}]`; `GET /api/feedback/attachments/{attachmentId}` has a
`JWTBearerAuth` requirement and an `IdentitySessionCookie` requirement, and its description starts
the auth sentence with "Accepts"; `GET /api/hello` has `security: []`; Redocly `security-defined`
reports 0 errors.

### Review

- Round 1. Roster: general. Effort 3 (1354 lines). Session 01a21453-33e4-7922-8c4a-7099963f9482.
- Counts: bug 0 open / 0 fixed / 0 wontfix; suggestion 0; nit 0. Final open count 0.
- Disposition: clean. The goal restatement matches the original task. No wontfix and no escalation.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
- This review read the diff and the recorded gates. It did not re-run build, test, or audit.
