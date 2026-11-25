# Round 1 — general
**Date:** 2026-11-26
**Scope reviewed:** branch `task/458-001-publish-session-cookie-and-per-operation-security` vs `master`. OpenAPI security transformers, web-server registration, and the OpenAPI document tests. Compared with `## Goal (implementer restatement)` and with `## Description` / `## Requirements`.

## Summary

The change publishes `IdentitySessionCookie` (`apiKey`, `in: cookie`, name from `IdentitySessionDefaults.CookieName`) and writes each operation's `security` from `EndpointDefinition.AuthSchemeNames`. Anonymous verbs get `security: []`. An agent scheme gets `JWTBearerAuth` with the existing scope list. The session scheme, or no schemes when the host marks the session as `DefaultScheme`, gets the cookie requirement. Both become two alternative requirement objects, bearer first, and the description sentence is "Requires …" or "Accepts … or …". FastEndpoints `AddAuth` is removed so its later document pass cannot stamp every registered scheme onto an endpoint with no `AuthSchemes`.

The implementer restatement matches the original requirements. The extra default-scheme clause is the same rule the task needs so an authorized endpoint with no `AuthSchemes` does not stay anonymous: `IdentitySessionDefaults` is already the host DefaultScheme, and permission policies do not list schemes. The recorded `## Results` cover build, the OpenAPI suites this change touches (including api-server `AddOpenApi` and the reviewer guide), and `ganda repo audit`. No gate was re-run.

Cookie routes list `identity-session` plus `mock-identity-session`. The transformer treats the session name as the cookie requirement and does not publish the mock scheme. That matches the task: the published credential is the ceremony cookie, and the mock handler is fail-closed outside Development. `GET /api/admin/roles` and `GET /api/feedback/attachments/{attachmentId}` are asserted on the real web-server document. The bearer and payment tests now assert `security: []` or the session requirement, which is the contract change the task asks for, not a silent weakening.

## Issues

None.
