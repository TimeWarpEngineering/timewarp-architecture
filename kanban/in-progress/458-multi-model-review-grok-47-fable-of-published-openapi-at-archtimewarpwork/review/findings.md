# Merged findings — task 458, round 1

- **Snapshot:** `openapi-v1.snapshot.json` (SHA-256 `496a0506…9b98`, OpenAPI 3.2.0, 64 paths, 79
  operations). Grok also confirmed the live document matched this hash at review time.
- **Inputs:** `round-1/grok-4.7.md` (Grok 4.7, G-001…G-013: 0 blocker, 9 major, 3 minor, 1 nit) and
  `round-1/fable.md` (Fable `claude-fable-5-1`, F-001…F-018: 1 blocker, 7 major, 7 minor, 3 nit).
  Both wrote to separate scratch files outside the repo and were told not to read each other's.
  Both files were copied into `round-1/` after both runs ended. The prompts are beside them.
- **Merger:** the implement oracle (Claude Opus 5.5), 2026-11-24. The merger did not take any
  reviewer's word. Each row's **Verified** column says what the merger checked: a python3 read of the
  snapshot pointer, a repo source read, or an unauthenticated `GET` against the live host.
- **Severity:** where the two reviewers disagreed, the merged severity follows the brief's scale and
  the reason is given.

`Both` = both models found it independently.

## Summary

| ID | Severity | Title | Found by | Verified |
|----|----------|-------|----------|----------|
| M-01 | blocker | [held] operator security finding on the live host | Fable F-001 | live GET |
| M-02 | major | Cookie-session operations have no `security`; no session scheme; dual-scheme operations look bearer-only | **Both** G-001, F-003, F-012 | snapshot + source + live |
| M-03 | major | Typed-id schemas (`PrincipalId`, `CredentialId`, nullable) have no `type` | **Both** G-002, F-008 | snapshot |
| M-04 | major | Attachment upload and download are documented as JSON objects | **Both** G-003, F-004, F-005 | snapshot + source |
| M-05 | major | Validation 400 is `application/json` with `errors`; documented problem schema is `problem+json` without `errors`; three problem shapes | **Both** G-004, F-009 (+ F-013 part) | snapshot + live |
| M-06 | major | operationIds are endpoint class names; `[Operation]` ids and descriptions do not reach the document; scheme guide cites ids that do not exist | **Both** G-006, F-002, F-010 (summary part), F-011, F-016 | snapshot + source |
| M-07 | major | Request bodies omit `required` for fields validators reject when empty | Grok G-005 | snapshot |
| M-08 | major | Browser-navigation endpoints (sign-out, antiforgery token, Entra challenge) document responses that never happen | Fable F-006, F-007, F-013; Grok G-010 noted the sign-out shape | snapshot + source + live |
| M-09 | major | `GET /api/Roles` and siblings require `userId` and publish OData paging the handler ignores | Grok G-008 | snapshot + source |
| M-10 | major | `/api` tip alias has no response content or headers; `PAYMENT-SIGNATURE` / `PAYMENT-RESPONSE` undocumented | Grok G-007 | snapshot |
| M-11 | minor | Liveness `/web-server/alive` is served and named in prose but is neither an operation nor an exclusion | Grok G-009 (major), Fable F-016 part | snapshot + live |
| M-12 | minor | `info.description` glues the payment paragraph onto the last bullet; "402/503 only on…" omits health 503 | Grok G-011 | snapshot |
| M-13 | minor | Path casing and `/api` prefix are mixed | **Both** G-010, F-014 | snapshot |
| M-14 | minor | No root `tags`; no `info.contact` / `info.license` | **Both** G-012, F-017 | snapshot + lint |
| M-15 | minor | `servers[0].url` has a trailing slash | **Both** G-013, F-015 | snapshot + lint |
| M-16 | minor | 0 of 52 parameters and 58 of 462 properties have a description | Fable F-010 (parameter/property part) | snapshot |
| M-17 | nit | Structurally identical components published under separate names | Fable F-018, F-011 (list wrappers) | snapshot |
| L-01 | — | Linter rules that are wrong for this document | Grok (linter note), Fable (F-003 aside) | spec reading |

Counts after merge: 1 blocker, 9 major, 6 minor, 1 nit, plus one linter note.

---

## M-01 — [held] operator security finding on the live host

- **Severity:** blocker · **Lens:** security, doc-vs-reality · **Found by:** Fable F-001 only
- **Status:** held. This repository is public. The finding concerns the live host. Its text is held
  outside the repository until the operator has remediated it. Grok did not find it.
- **Verified:** the merger reproduced the reviewer's evidence with one unauthenticated `GET`
  on 2026-11-24 and kept no copy of the response.
- **Restore:** when remediation is confirmed, this section and `round-1/fable.md` F-001 get the
  reviewer's original text back, and the follow-on task is filed.

## M-02 — Cookie-session operations have no `security`; no session scheme; dual-scheme operations look bearer-only

- **Severity:** major · **Lens:** security, consumability · **Found by:** Both (G-001, F-003, F-012)
- **Pointers:** `/components/securitySchemes` (only `JWTBearerAuth`); `/paths/~1api~1Roles/get`;
  `/paths/~1api~1settings/get`; `/paths/~1api~1admin~1feedback~1{feedbackItemId}~1replies/post`;
  `/paths/~1api~1Feedback~1attachments~1{attachmentId}/get/security`
- **Verified:** snapshot: 58 operations have no `security` key and there is no root `security`.
  35 of the 58 document a `401`. One is the anonymous Entra ceremony; the other 34 are session
  routes. Live: `GET /api/settings` and `GET /api/Roles` return `401`, 0 bytes, no
  `WWW-Authenticate`. Source: `open-api-bearer-security-server.cs` Design region says it leaves
  cookie-only operations alone. The attachment download lists the session schemes and the agent
  scheme in source, and publishes only `[{"JWTBearerAuth": []}]`.
- **Merged fix:** add a session-cookie scheme (`apiKey` in cookie). Emit per-operation `security`
  from the endpoint's authentication schemes: session → `[{session: []}]`, agent → bearer with
  scopes, both → two alternative requirement objects. Emit `security: []` on anonymous operations,
  which also clears Redocly's 18 false `security-defined` errors. Document test: every operation
  with a 401 has a requirement, with an allowlist for the Entra ceremony.
- **Owner:** transformer (`source/foundation/foundation-server/open-api-bearer-security-server.cs`,
  sibling session transformer).

## M-03 — Typed-id schemas have no `type`

- **Severity:** major · **Lens:** contracts, consumability · **Found by:** Both (G-002, F-008)
- **Pointers:** `/components/schemas/TimeWarpIdentityPrincipalId`,
  `/components/schemas/TimeWarpIdentityCredentialId`,
  `/components/schemas/SystemNullableOfTimeWarpIdentityPrincipalId`,
  `/components/schemas/TimeWarpArchitectureFeaturesIdentityGetCurrentSession_Response/properties/principalId`
- **Verified:** the first two are description-only. The nullable one is `{}`. The session
  `principalId` is `oneOf [null, $ref {}]`.
- **Merged fix:** a schema transformer maps every `[TypedId]` to `type: string, format: uuid` and its
  `Nullable<T>` to `type: ["null","string"]`. Document test: no component is `{}` or lacks
  `type`/`oneOf`/`enum`.
- **Owner:** transformer in foundation-server; typed-id generator is the source of the set.

## M-04 — Attachment upload and download are documented as JSON objects

- **Severity:** major · **Lens:** contracts, doc-vs-reality · **Found by:** Both (G-003, F-004, F-005)
- **Pointers:** `/paths/~1api~1Feedback~1attachments/post/requestBody/content/image~1png/schema`;
  `/paths/~1api~1Feedback~1attachments~1{attachmentId}/get/responses/200/content/application~1json`
- **Verified:** snapshot: six upload media types each carry the `{content, fileName, contentType}`
  command; no parameters (no `X-File-Name`); download 200 is `application/json` with the response
  object. Source: the hand-written endpoints read the raw body and `X-File-Name`, and write raw bytes
  with the stored content type. Fable also notes the undocumented 413.
- **Merged fix:** for `IFileUploadRequest`, publish `type: string, format: binary` per media type, a
  required `X-File-Name` header, and 413. For a Stream-bearing response, publish binary content per
  allowed type with `Content-Disposition`. Detect both generically from the markers, not by route.
- **Owner:** transformer (beside `open-api-one-of.cs`), driven by the contract markers.

## M-05 — Validation 400 does not match the documented problem shape

- **Severity:** major (Fable rated minor, Grok major; the brief's scale makes a doc-vs-live
  contradiction major) · **Lens:** doc-vs-reality, contracts · **Found by:** Both (G-004, F-009; F-013 part)
- **Pointers:** `/paths/~1api~1Hello/get/responses/default/content/application~1problem+json`;
  `/components/schemas/TimeWarpFoundationTypesSharedProblemDetails`;
  `/components/schemas/MicrosoftAspNetCoreMvcProblemDetails`
- **Verified:** live `GET /api/Hello` without `name` → `400 application/json` with an `errors` map.
  `SharedProblemDetails` has `type,title,status,detail,instance` and no `errors`. A second component
  `MicrosoftAspNetCoreMvcProblemDetails` exists; bearer 401/403 and payment 402 use inline shapes.
- **Merged fix:** make FastEndpoints validation failures `application/problem+json`, add `errors`
  (and the extension bag) to the one problem schema, document 400 on operations with a validator,
  and point every problem response at that one component. Probe test on `GET /api/Hello`.
- **Owner:** host config (FastEndpoints error builder in `common-server-module.cs`) and transformers
  (`open-api-one-of.cs`, `open-api-bearer-security-server.cs`, payment responses).

## M-06 — operationIds are class names; `[Operation]` metadata does not reach the document

- **Severity:** major · **Lens:** consumability, consistency · **Found by:** Both (G-006, F-002,
  F-010 summary part, F-011, F-016)
- **Pointers:** `/paths/~1api~1Roles/get/operationId`; `/paths/~1api~1health/get/operationId`;
  `/components/securitySchemes/JWTBearerAuth/description`;
  `/paths/~1api~1Users~1Current~1xai-api-key/put/summary`
- **Verified:** 76 of 79 ids are `TimeWarpArchitectureFeatures…Endpoint`; three hand-added ids are
  camelCase. The scheme description names `start-agent-key-registration` and three other kebab ids
  that are not operationIds. 38 operations have no `summary`; some summaries are the command's XML
  summary ("The new key. It is not returned.") or a route string ("POST api/Feedback/attachments").
- **Merged fix:** operationId = `[Operation]` id. `summary` = `[Operation].Description`;
  `description` = XML summary or remarks. The reviewer guide reads ids from the contracts. Optional
  in the same task: a `CreateSchemaReferenceId` that drops the `TimeWarpArchitectureFeatures`
  prefix. Document checks: every operation has a summary; every id quoted in a description
  resolves.
- **Owner:** generator (`fast-endpoint-emitter.cs` / `fast-endpoint-source-generator.cs`,
  `endpoint-documentation-resolver.cs`), transformer (`open-api-agent-reviewer-guide-server.cs`),
  analyzer TWA0035 if `[Operation]` becomes mandatory.

## M-07 — Request bodies omit `required`

- **Severity:** major · **Lens:** contracts · **Found by:** Grok G-005 only
- **Pointers:** `/components/schemas/TimeWarpArchitectureFeaturesAdminRolesCreateRole_Command`;
  `/components/schemas/TimeWarpArchitectureFeaturesAgentLinksRequestAgentHumanLink_Command/required`
- **Verified:** 34 `*_Command` schemas; only `RequestAgentHumanLink_Command` has `required`.
  `CreateRole_Command` publishes `name` as optional.
- **Merged fix:** derive `required` from FluentValidation `NotEmpty`/`NotNull` rules (and
  non-nullable reference types). Document check that each such rule's member is in `required`.
  Write the rule in `tw-web-api-contracts`.
- **Owner:** schema pipeline / transformer; skill.

## M-08 — Browser-navigation endpoints document responses that never happen

- **Severity:** major · **Lens:** doc-vs-reality · **Found by:** Fable F-006, F-007, F-013; Grok G-010
  (sign-out shape, as part of the path finding)
- **Pointers:** `/paths/~1identity~1sign-out/post/responses/204`;
  `/paths/~1identity~1sign-out~1antiforgery-token/get/responses/204`;
  `/paths/~1api~1identity~1entra~1challenge/get/responses/204`
- **Verified:** snapshot shows only 204 for both sign-out routes and 204/400/401/403/404 with no
  `mode` parameter for the Entra challenge. Live `GET /identity/sign-out/antiforgery-token` →
  `200 application/json` with `formFieldName`, `requestToken`. Source: sign-out returns 303 or 400.
- **Merged fix:** either document the real contracts (303 + `Location`, 400, the antiforgery
  field, 200 token body, `mode` enum, 302) from the endpoint declarations, or exclude these three
  browser-navigation routes and list them in `x-timewarp-excluded-routes`. The follow-on task decides.
- **Owner:** hand-written endpoints; `open-api-discovery-coverage.cs` for the exclusion variant.

## M-09 — Required `userId` and ignored OData parameters

- **Severity:** major · **Lens:** contracts · **Found by:** Grok G-008 only
- **Pointers:** `/paths/~1api~1Roles/get/parameters/1` (`userId`, required);
  `/paths/~1api~1Roles/get/parameters/6` (`returnTotalCount`, required)
- **Verified:** snapshot parameters `search, userId*, top, skip, filter, orderBy, returnTotalCount*`.
  `get-roles-handler-application.cs` orders by name and reads none of the paging members or `UserId`.
- **Merged fix:** stop publishing `[AuthApiRequest]` `userId` as a required query parameter and
  drop `[OpenDataQueryParameters]` where the handler ignores them, or implement paging with a test.
  Sweep every contract with those attributes.
- **Owner:** contracts (`get-roles-contracts.cs` and siblings); `i-auth-api-request.cs` design.

## M-10 — Tip alias and payment headers

- **Severity:** major · **Lens:** contracts · **Found by:** Grok G-007 only
- **Pointers:** `/paths/~1api/get/responses/402`; `/paths/~1api~1tip/get/responses/402/headers/PAYMENT-REQUIRED`
- **Verified:** `/api` responses are description-only (no content, no headers). `PAYMENT-REQUIRED`
  appears 16 times; `PAYMENT-SIGNATURE` and `PAYMENT-RESPONSE` appear nowhere.
- **Merged fix:** clone the tip operation's responses for the alias; add optional request header
  `PAYMENT-SIGNATURE` and response header `PAYMENT-RESPONSE` on tip and the metered capability from
  the `PaymentHeaders` constants. No probing of paid routes was done or is needed.
- **Owner:** transformers (`open-api-discovery-coverage.cs`, `open-api-payment-responses-server.cs`).

## M-11 — Liveness route is neither an operation nor an exclusion

- **Severity:** minor (Grok rated major; a missing liveness listing does not lead to a wrong call)
  · **Lens:** doc-vs-reality · **Found by:** Grok G-009; Fable F-016 noted the same route
- **Pointers:** `/paths/~1api~1health/get/description`; `/x-timewarp-excluded-routes`
- **Verified:** the health description names `GET /web-server/alive`; live it returns 200 `Healthy`;
  it is not a path and not in the six exclusions, while `info.description` says a missing path is an
  exclusion. Part of G-009 is held with M-01.
- **Merged fix:** list liveness in `x-timewarp-excluded-routes` from `IngressPaths` constants and
  generate the health description from the same constants.
- **Owner:** transformer (`open-api-discovery-coverage.cs`).

## M-12 — `info.description` formatting and the "only" sentence

- **Severity:** minor · **Lens:** consistency · **Found by:** Grok G-011 only
- **Pointer:** `/info/description`
- **Verified:** the webmcp bullet runs straight into "Free and discovery operations never return
  HTTP 402." with a single space. `GET /api/health` documents a 503.
- **Merged fix:** append with a blank line; generate the "documented only on" list from operations
  that carry 402; say health's 503 is liveness, not payment.
- **Owner:** transformer (`open-api-payment-responses-server.cs`).

## M-13 — Mixed path casing and prefix

- **Severity:** minor · **Lens:** consistency · **Found by:** Both (G-010, F-014)
- **Pointers:** `/paths/~1Analytics~1TrackEvent`; `/paths/~1api~1Feedback`; `/paths/~1api~1admin~1feedback`;
  `/paths/~1identity~1sign-out`
- **Verified:** PascalCase `/api/Feedback`, `/api/Roles`, `/api/Users/Current/Profile`, `/api/Hello`;
  kebab `/api/agent-links`, `/api/identity/…`; `/Analytics/TrackEvent` and `/identity/sign-out` outside `/api`.
- **Merged fix:** one route grammar (kebab segments under `/api/`) written in the skill and enforced
  by an analyzer on `[ApiRoute]`. Breaking for callers of the demo routes; its own task.
- **Owner:** contracts (`[ApiRoute]`), skill (`route-grammar.md`), analyzer.

## M-14 — Root tags, contact, license

- **Severity:** minor (Fable nit, Grok minor; 79 Spectral warnings and no tag descriptions) ·
  **Lens:** consumability · **Found by:** Both (G-012, F-017)
- **Pointer:** `/info`; root (no `tags`)
- **Verified:** no root `tags`, `info.contact`, or `info.license`.
- **Merged fix:** document transformer emits root `tags` from the tags in use (with descriptions),
  plus `info.contact.url` and `info.license`.
- **Owner:** transformer (document options in `common-server-module.cs`).

## M-15 — Server URL trailing slash

- **Severity:** minor (Fable minor, Grok nit; Redocly reports an error) · **Lens:** validity ·
  **Found by:** Both (G-013, F-015)
- **Pointer:** `/servers/0/url` (`https://arch.timewarp.work/`)
- **Verified:** snapshot value; Redocly `no-server-trailing-slash`, Spectral `oas3-server-trailing-slash`.
- **Merged fix:** trim in `OpenApiPublicServer`, with a test assertion.
- **Owner:** transformer (`open-api-public-server.cs`).

## M-16 — Parameter and property descriptions

- **Severity:** minor · **Lens:** consumability · **Found by:** Fable F-010 only
- **Pointers:** `/paths/~1api~1Roles/get/parameters/1`; `/paths/~1api~1admin~1feedback/get/parameters/0`
- **Verified:** 0 of 52 parameters and 58 of 462 schema properties have a description.
- **Merged fix:** emit XML `<summary>` of each DTO member and fixed descriptions for shared
  parameter sets; add descriptions in contracts where missing.
- **Owner:** generator / contracts.

## M-17 — Duplicate structural components

- **Severity:** nit · **Lens:** contracts · **Found by:** Fable F-018 (and the list-wrapper part of F-011)
- **Pointers:** `/components/schemas/TimeWarpArchitectureFeaturesTipSubmitTipPost_Response`;
  `/components/schemas/TimeWarpArchitectureFeaturesTipSubmitTip_Response`
- **Verified:** the two tip responses are identical; several empty `BaseResponse` subclasses exist.
- **Reviewer's own fix:** "None required"; it is the contract-per-endpoint design.

## L-01 — Linter rules that are wrong for this document

- `oas3-operation-security-defined` (Spectral, 17): scopes listed on an `http` bearer requirement.
  OpenAPI 3.1+ allows role names in a non-OAuth requirement, and `type: http` has no scopes field.
  The scheme description lists the known scopes. Not a defect.
- `no-ambiguous-paths` (Redocly, 1): `GET /api/Feedback/attachments/{attachmentId}` vs
  `POST /api/Feedback/{feedbackItemId}/replies`. Different methods and a literal segment; not a
  runtime ambiguity. M-13 removes the reason it looks odd.
- `security-defined` (Redocly, 58): real for the 34 session operations (M-02); the other anonymous
  ones clear when M-02 emits `security: []`.
