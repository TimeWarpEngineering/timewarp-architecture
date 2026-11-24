# Review of the published OpenAPI document at arch.timewarp.work — fable

- **Model:** fable (Claude Fable 5.1)
- **Date:** 2026-11-24 (probes ran at roughly 20:20Z, five minutes after the snapshot fetch)
- **Task:** 458, round 1, independent reviewer
- **Read:** `review/openapi-v1.snapshot.json` (SHA-256 `496a0506…9b98`, OpenAPI 3.2.0, 64 paths, 79
  operations, 175 schemas), `review/snapshot-meta.md`, `review/lint/{readme,redocly,spectral}.txt`,
  and repo source for owners: `source/foundation/foundation-server/open-api-*.cs`,
  `common-server-module.cs`, `source/container-apps/web/platform/identity-host/open-api-agent-reviewer-guide-server.cs`,
  the feedback attachment, sign-out, Entra challenge, analytics, and GetRoles contracts and endpoints,
  `source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-source-generator.cs`,
  `goldens/ingress/WebServerApiRoutePrefixes.g.cs`, and `skills/tw-web-api-contracts/references/`.
- **Probed (unauthenticated GET only, no POST/PUT/DELETE, nothing under `/api/tip`, `/api`, or
  `/api/demo/metered-capability`, no identity ceremonies):** `/api/health`, `/health`, `/alive`,
  `/web-server/alive`, `/llms.txt`, `/auth.md`, `/index.md`, all six `/.well-known/*` documents,
  `/openapi/v1.json`, `/openapi/v1.yaml`, `/scalar/v1`, `/api/Hello` with and without `name`,
  `/api/settings/assistant`, `/api/identity/sign-in-methods`, `/api/identity/session`,
  `/api/identity/entra/choice`, `/api/identity/entra/challenge`, `/api/Users/Current/Profile`,
  `/identity/sign-out/antiforgery-token`, one route held under F-001, and the cookie- or bearer-protected
  GETs `/api/settings`, `/api/agent-chat/configuration`, `/api/Feedback`, `/api/Roles`,
  `/api/admin/feedback`, `/api/identity/credentials`, `/api/agent-links`, `/api/identity/agent/me`,
  `/api/Users/Current/xai-api-key`, `/api/settings/entra-tenant` (each expected to 401 and did).
- **Pointer verification:** every JSON pointer below was resolved against the snapshot with a
  python3 script before writing. Where a finding is about something *absent*, the pointer is the
  nearest existing parent and the text says what is missing.

Severity counts: **blocker 1 · major 7 · minor 7 · nit 3** (18 findings).

---

### F-001 — [held] operator security finding on the live host

- **Severity:** blocker
- **Lens:** security, doc-vs-reality
- **Status:** The reviewer's full text for this finding is held outside the repository until the
  operator has remediated it on the live host. This repository is public. The merger replaced only
  this section; every other section of this file is the reviewer's text unchanged. When the
  remediation is confirmed, the original section is restored here and in `review/findings.md`.

---

### F-002 — The bearer scheme's "how to obtain a token" guide names four operationIds that do not exist in the document

- **Severity:** major
- **Lens:** consumability, contracts
- **Pointer:** `/components/securitySchemes/JWTBearerAuth/description`;
  `/paths/~1api~1identity~1agent~1register~1options/post/operationId`
- **Evidence:** The scheme description says "call these operations in order: start-agent-key-registration
  (POST /api/identity/agent/register/options), then complete-agent-key-registration …, then
  start-agent-token-issuance …, then complete-agent-token-issuance …". The actual operationIds are
  `TimeWarpArchitectureFeaturesIdentityStartAgentKeyRegistrationEndpoint`,
  `…CompleteAgentKeyRegistrationEndpoint`, `…StartAgentTokenIssuanceEndpoint`,
  `…CompleteAgentTokenIssuanceEndpoint`. Nothing in the document carries the kebab ids. The source
  admits the gap: `open-api-agent-reviewer-guide-server.cs:9-11` says the step also names method and
  path "because the document does not set operationId to the kebab id". An agent or a generated client
  that follows the guide by operationId finds nothing; the path rescues a careful reader only.
- **Proposed fix:** Set `operationId` from the `[Operation]` id (for example `feedback.upload-attachment`
  → `feedback.upload-attachment` or a sanitized `feedback_upload_attachment`), and make the reviewer
  guide read the id from the contract's `[Operation]` attribute instead of a string literal, so the two
  cannot drift. Until every contract has `[Operation]` (see F-010), fall back to the current name. Add
  a test that every operationId quoted in any description resolves to an operation.
- **Owner:** generator (`fast-endpoint-source-generator.cs`, which currently lets FastEndpoints derive
  the id from the endpoint type name) and transformer (`open-api-agent-reviewer-guide-server.cs`).

---

### F-003 — 34 cookie-session operations document 401/403 but declare no security scheme; the document has no cookie scheme at all

- **Severity:** major
- **Lens:** security, consumability, validity
- **Pointer:** `/components/securitySchemes` (only `JWTBearerAuth` exists); example operations
  `/paths/~1api~1settings/get` (has `responses/401` = `{"description":"Unauthorized"}` and no `security`
  key), `/paths/~1api~1Roles/get`, `/paths/~1api~1agent-chat~1completions/post`,
  `/paths/~1api~1agent-links~1{linkId}~1approve/post`, `/paths/~1api~1Users~1Current~1xai-api-key/put`.
- **Evidence:** 34 operations have a bare `401: Unauthorized` / `403: Forbidden` pair with no body
  schema and no `security`. Live, `GET /api/settings`, `/api/Roles`, `/api/Feedback`,
  `/api/agent-chat/configuration`, `/api/Users/Current/xai-api-key`, and `/api/settings/entra-tenant`
  all return `401` with an empty body and no `WWW-Authenticate` — the `identity-session` cookie scheme
  (`identity-session-defaults-server.cs:21-22`, cookie `.timewarp.identity.session`). Redocly reports 58
  `security-defined` errors; 18 of those are false positives for the explicitly anonymous operations
  (see "where the linters are wrong" below), but the 34 cookie operations are real. The bearer transformer's
  Design region (`open-api-bearer-security-server.cs:17-18`) deliberately "leaves cookie-only operations
  alone", so nothing documents them. A Kiota or NSwag consumer sees 34 operations that look anonymous and
  then 401; an agent cannot tell "needs a browser session" from "needs a bearer".
- **Proposed fix:** Add a second scheme, e.g. `IdentitySession` of `type: apiKey, in: cookie,
  name: .timewarp.identity.session`, with a description that says it is obtained only through the
  browser passkey or Entra ceremony and is not available to agents. Extend `OpenApiBearerSecurity` (or
  add a sibling transformer) to read `EndpointDefinition.AuthSchemeNames`: emit `[{IdentitySession: []}]`
  when the list contains the session scheme, `[{JWTBearerAuth: [scopes]}]` when it contains the agent
  scheme, and both entries (alternatives) when it contains both. Document the cookie 401 as empty-body
  with no `WWW-Authenticate`. Also set a root `security: []` is *not* right here; leave root unset and
  make every operation explicit (the anonymous ones already are, via `security: []` on the three
  hand-added operations — extend that to the 23 `[EndpointAllowAnonymous]` operations so linters stop
  flagging them).
- **Owner:** transformer (`open-api-bearer-security-server.cs` / new `open-api-session-security-server.cs`)
  plus `common-server-module.cs` `AddAuth`.

---

### F-004 — Attachment download is documented as `application/json` with a JSON object body; the endpoint writes raw bytes with the file's own content type

- **Severity:** major
- **Lens:** contracts, doc-vs-reality
- **Pointer:** `/paths/~1api~1Feedback~1attachments~1{attachmentId}/get/responses/200/content/application~1json/schema`;
  `/components/schemas/TimeWarpArchitectureFeaturesFeedbackDownloadFeedbackAttachment_Response/properties/content`
- **Evidence:** The 200 response is `application/json` → `DownloadFeedbackAttachment_Response`
  `{content: $ref SystemIOStream (string/binary), contentType, fileName}`. The hand-written endpoint
  (`download-feedback-attachment-endpoint-server.cs:52-66`) sets `Response.ContentType` to the stored
  file's type (png/jpeg/gif/webp/pdf/text), adds `Content-Disposition`, `X-Content-Type-Options: nosniff`,
  `Cache-Control: private, no-store`, and copies the stream to the body. No JSON is ever written on
  success. A generated client would try to deserialize a PNG as JSON. The operation `summary` is also the
  XML doc text `GET api/Feedback/attachments/{id}` rather than the `[Operation]` description.
- **Proposed fix:** For `IFileDownload`-style responses (a `Stream` property on the Response) the
  OneOf/operation transformer should publish 200 as one entry per allowed content type
  (`FeedbackAttachmentRules.AllowedContentTypes`) with `schema: {type: string, format: binary}`, plus
  `Content-Disposition` and `Cache-Control` headers, and drop the JSON entry. Detect this generically
  (Response has a `Stream` member) so no hand patch is needed.
- **Owner:** transformer (`open-api-one-of.cs` or a sibling) and the endpoint's `Description(...)`.

---

### F-005 — Attachment upload is documented as six media types that all carry a JSON command object; the body is the raw file and the name travels in an undocumented `X-File-Name` header

- **Severity:** major
- **Lens:** contracts, doc-vs-reality
- **Pointer:** `/paths/~1api~1Feedback~1attachments/post/requestBody/content/image~1png/schema` (and the
  `image/jpeg`, `image/gif`, `image/webp`, `application/pdf`, `text/plain` siblings);
  `/paths/~1api~1Feedback~1attachments/post` (no `parameters`)
- **Evidence:** Each media type references `UploadFeedbackAttachment_Command` `{content: Stream,
  fileName, contentType}`. The contract (`upload-feedback-attachment-contracts.cs:5-9`) says "The body is
  the file (IFileUploadRequest), not JSON … The file name arrives in X-File-Name", and the endpoint
  installs a custom `RequestBinder` with an `AcceptsMetadata(contentTypes, typeof(Command))` allow-list.
  A client that follows the document would POST a JSON object with a base64 `content` field as
  `image/png` and get the handler's 415/400. The 413 `application/problem+json` path
  (`upload-feedback-attachment-endpoint-server.cs:57-66`) is also undocumented.
- **Proposed fix:** Publish each media type with `schema: {type: string, format: binary}`, add a
  required header parameter `X-File-Name` (string, max `FeedbackAttachmentRules.MaxFileNameLength`), and
  add a `413` response. Generate this from the `IFileUploadRequest` marker so it cannot drift.
- **Owner:** transformer (file-upload rule next to `OpenApiOneOf`) with the contract as the source.

---

### F-006 — `/identity/sign-out` is documented as "204 No Content" with an empty form body; it returns 303 or 400 and requires an antiforgery form field

- **Severity:** major
- **Lens:** doc-vs-reality, contracts
- **Pointer:** `/paths/~1identity~1sign-out/post/responses/204`;
  `/paths/~1identity~1sign-out/post/requestBody/content/application~1x-www-form-urlencoded/schema`
- **Evidence:** Source (`sign-out-browser-session-endpoint-server.cs:36-52`): invalid or missing
  antiforgery token → `400` and nothing cleared; success → `303 See Other` with `Location: /Login`.
  `MarkResponseStart` is called precisely so FastEndpoints does not emit 204. The document shows a single
  `204`, no `303`, no `400`, and a `required: true` body of `FastEndpointsEmptyRequest`
  (`{type: object}`) although the POST must carry `__RequestVerificationToken` (the live
  antiforgery-token response names that field). The operation also has no `description`, no `summary`,
  and no anonymous note, unlike every other anonymous operation. (Not probed: POST is out of scope.)
- **Proposed fix:** Describe the real contract in `Configure()`: `Summary(s => s.Description = …)`,
  `Produces(303)` with the `Location` header, `Produces(400)`, remove 204, and give the form body a schema
  with the antiforgery field (name from `IAntiforgery` options). Alternatively exclude both sign-out routes
  from the document (they are browser-navigation routes, like the Entra challenge) and list them in
  `x-timewarp-excluded-routes`.
- **Owner:** contract/endpoint (`sign-out-browser-session-endpoint-server.cs`); the exclusion variant is
  `open-api-discovery-coverage.cs`.

---

### F-007 — `/identity/sign-out/antiforgery-token` is documented as 204; it returns 200 with a JSON token body

- **Severity:** major
- **Lens:** doc-vs-reality
- **Pointer:** `/paths/~1identity~1sign-out~1antiforgery-token/get/responses/204`
- **Evidence:** Live `GET /identity/sign-out/antiforgery-token` → `200`, `content-type: application/json;
  charset=utf-8`, `cache-control: no-cache, no-store`, body `{"formFieldName":"__RequestVerificationToken",
  "requestToken":"CfDJ8…"}`. Source (`sign-out-antiforgery-token-endpoint-server.cs:41-47`) sets 200 and
  writes `SignOutBrowserSession.AntiforgeryToken` as JSON. The document's only response is `204 No
  Content`, and `SignOutBrowserSession.AntiforgeryToken` has no component schema.
- **Proposed fix:** `Description(b => b.Produces<SignOutBrowserSession.AntiforgeryToken>(200,
  "application/json"))` and drop 204. Better: make `EndpointWithoutRequest` hand-written endpoints
  declare their response type so FastEndpoints documents it, or use `Endpoint<EmptyRequest, TResponse>`.
- **Owner:** contract/endpoint (`sign-out-antiforgery-token-endpoint-server.cs`).

---

### F-008 — Typed-id components `TimeWarpIdentityPrincipalId`, `TimeWarpIdentityCredentialId`, and `SystemNullableOfTimeWarpIdentityPrincipalId` have no `type`; generated clients get `any`

- **Severity:** major
- **Lens:** contracts, consumability
- **Pointer:** `/components/schemas/TimeWarpIdentityPrincipalId`; `/components/schemas/TimeWarpIdentityCredentialId`;
  `/components/schemas/SystemNullableOfTimeWarpIdentityPrincipalId`;
  `/components/schemas/TimeWarpArchitectureFeaturesIdentityCompleteAgentTokenIssuance_Response/properties/principalId`;
  `/components/schemas/TimeWarpArchitectureFeaturesIdentityGetCurrentSession_Response/properties/principalId`
- **Evidence:** `TimeWarpIdentityPrincipalId` is `{"description": "Stable typed identity…"}` with no
  `type`; `SystemNullableOfTimeWarpIdentityPrincipalId` is `{}`. These are the only three schemas in the
  document without `type`, `oneOf`, or `enum`. The session response publishes `principalId` as
  `oneOf: [null, $ref {}]`, i.e. "anything". The token-issuance response, which an agent must parse,
  publishes `principalId` as `$ref` to an untyped schema. The typed-id generator installs a public
  `JsonConverter` (`typed-id-source-generator.cs:24-26`) and its Design region names "serialize fail-open
  as {}" as the defect it closes, so the wire value is a scalar Guid string; the OpenAPI schema exporter
  does not see through the converter. Kiota/NSwag emit `object`/`any` for these properties, and the
  Credential and Principal ids become indistinguishable from each other and from free text.
- **Proposed fix:** A schema transformer that maps every typed id (the generator knows the set; expose
  it or match the `Value`/`From` shape) to `{type: string, format: uuid}` and keeps the description;
  make `Nullable<T>` of a typed id `type: ["null","string"]`. Add a document test that no component lacks
  `type`/`oneOf`/`enum`.
- **Owner:** transformer (foundation-server, new `open-api-typed-id.cs`) with the typed-id generator as
  the source of truth.

---

### F-009 — Validation failures return `application/json` with an `errors` map, not the documented `application/problem+json` `SharedProblemDetails`

- **Severity:** minor (would be major if a client branches on content type; the status is right)
- **Lens:** doc-vs-reality, contracts
- **Pointer:** `/paths/~1api~1Hello/get/responses/default` (same `default` on every generated operation);
  `/components/schemas/TimeWarpFoundationTypesSharedProblemDetails`
- **Evidence:** Live `GET /api/Hello` (no `name`) → `400`, `content-type: application/json; charset=utf-8`,
  body `{"type":"https://tools.ietf.org/html/rfc7231#section-6.5.1","title":"One or more validation errors
  occurred","status":400,"detail":"'Query:Name' must not be empty.","errors":{"name":["'Query:Name' must
  not be empty."]}}`. The document's `default` says `application/problem+json` with
  `SharedProblemDetails` (`type,title,status,detail,instance`) — no `errors`. So the FastEndpoints
  validation path differs from the handler-problem path in both media type and shape. The 402/503 and
  bearer 401/403 responses already publish their own inline problem shapes; the validation 400 is the
  one path left undocumented.
- **Proposed fix:** Either configure FastEndpoints' error response to write
  `application/problem+json` with the `errors` extension (RFC 9457 allows it), and add `errors` to
  `SharedProblemDetails`'s schema as an optional map; or document a `400` response for every operation
  with a validator that names the validation shape. Prefer the first: one problem shape on the wire.
- **Owner:** host config (`common-server-module.cs` FastEndpoints `Errors.ResponseBuilder`/`ProducesMetaForErrorResponses`)
  and contract (`SharedProblemDetails` schema).

---

### F-010 — 38 operations have no `summary`, 35 have no `description`, 0 of 52 parameters have a description, 404 of 462 properties have none

- **Severity:** minor
- **Lens:** consumability
- **Pointer:** `/paths/~1api~1agent-chat~1completions/post` (neither summary nor description);
  `/paths/~1api~1settings/get`; `/paths/~1api~1Roles/get/parameters/1` (required `userId`, no description);
  `/paths/~1api~1admin~1feedback/get/parameters/0`
- **Evidence:** Redocly `operation-summary` ×38, Spectral `operation-description` ×35. The operations that
  do have a summary are exactly the contracts with `[Operation(..., Description = …)]` (31 contract files
  carry it); the generator copies that description to the summary. The remaining generated operations and
  all four hand-written ones lack it. Query parameters such as `userId` (required on list roles — it is the
  `[AuthApiRequest]` acting-user id, `get-roles-contracts.cs:44-57`), `top`, `skip`, `filter`, `orderBy`
  (`[OpenDataQueryParameters]`), `includeClosed`, `sortBy`, `sortDescending`, `page`, `pageSize` have no
  text, so an agent cannot know that `userId` is "the acting admin's id" rather than a filter. Enum
  parameters do get a `$ref` to a string enum — good.
- **Proposed fix:** Make `[Operation]` with a `Description` mandatory for `[ApiEndpoint]` contracts
  (promote TWA0035 from suggestion to warning, which fails the build), and have the generator emit
  `summary` from `[Operation].Description` and `description` from the XML `<summary>` or a new
  `[Operation].Detail`. For `[AuthApiRequest]` and `[OpenDataQueryParameters]` emit fixed parameter
  descriptions from the shared source. Property descriptions: emit the XML `<summary>` of each DTO member
  (System.Text.Json schema export already does this for the 58 that have one).
- **Owner:** generator (`fast-endpoint-source-generator.cs`), analyzer (TWA0035), contracts.

---

### F-011 — Operation ids are the fully-qualified endpoint class names; the three hand-added ones use a different style

- **Severity:** minor
- **Lens:** consistency, consumability
- **Pointer:** `/paths/~1api~1agent-chat~1completions/post/operationId`
  (`TimeWarpArchitectureFeaturesAgentChatsCompleteAgentChatEndpoint`); `/paths/~1api~1health/get/operationId`
  (`getApiHealth`); `/paths/~1api/get/operationId` (`getApiTipAlias`)
- **Evidence:** 76 ids are `TimeWarpArchitectureFeatures<Area><Name>Endpoint`; 3 are `camelCase`. All
  79 are unique. Kiota produces a method per operationId; NSwag strips nothing, so a client gets
  `TimeWarpArchitectureFeaturesFeedbackListMyFeedbackEndpointAsync()`. Schema component names have the
  same prefix (`TimeWarpArchitectureFeaturesFeedbackListMyFeedback_Response`), plus 20
  `SystemCollectionsGenericListOf…` / `IReadOnlyListOf…` array components (two structurally identical pairs:
  `…ListOfSystemGuid` vs `…IReadOnlyListOfSystemGuid`, same for `String`) that generators turn into
  wrapper types. The ids also leak the internal namespace layout (see F-016).
- **Proposed fix:** Same change as F-002: operationId from `[Operation]` id. Component names: a
  `CreateSchemaReferenceId` that strips the `TimeWarpArchitectureFeatures` prefix and inlines
  `List<T>`/`IReadOnlyList<T>` as `type: array` instead of registering components.
- **Owner:** generator and `common-server-module.cs` (`CreateSchemaReferenceId`, next to `OpenApiOneOf`'s).

---

### F-012 — Download attachment is published as bearer-only; the endpoint also accepts the browser session

- **Severity:** minor
- **Lens:** security, consumability
- **Pointer:** `/paths/~1api~1Feedback~1attachments~1{attachmentId}/get/security` (`[{"JWTBearerAuth": []}]`);
  `/paths/~1api~1Feedback~1attachments~1{attachmentId}/get/description` ("Requires an agent bearer token")
- **Evidence:** `download-feedback-attachment-endpoint-server.cs:31-34` lists `IdentitySession`,
  `MockIdentitySession`, and `AgentToken`. The bearer transformer emits only the bearer requirement and the
  sentence "Requires an agent bearer token", so a human-session client reading the document believes it
  cannot call this. The empty scope list is correct (no route policy; the handler checks owner-or-read).
  Same pattern on the eight `credential:manage` operations (`/api/identity/credentials*`), whose contract
  schemes are "IdentitySession + AgentToken" per `endpoint-auth.md`.
- **Proposed fix:** Covered by F-003: emit alternatives `[{IdentitySession: []}, {JWTBearerAuth: [...]}]`
  and phrase the description "Requires a browser session or an agent bearer token".
- **Owner:** transformer (`open-api-bearer-security-server.cs`).

---

### F-013 — Entra challenge documents a `204` that never happens and omits the 302 redirect and the `mode` query parameter

- **Severity:** minor
- **Lens:** doc-vs-reality, contracts
- **Pointer:** `/paths/~1api~1identity~1entra~1challenge/get/responses/204`; `/paths/~1api~1identity~1entra~1challenge/get`
  (no `parameters`)
- **Evidence:** The description says "mode must be link or bootstrap (400). Success is an OIDC redirect."
  The endpoint reads `Request.Query["mode"]` (`challenge-entra-endpoint-server.cs:82`) and issues
  `Challenge("entra")` (a 302). The document has no `mode` parameter and no `302`; it keeps FastEndpoints'
  default `204`. Live (Microsoft 365 not enabled on this host) the route returns `404` with no body,
  consistent with the documented 404. Both `400` and `403` reference `MicrosoftAspNetCoreMvcProblemDetails`,
  a second problem-details component structurally identical to `SharedProblemDetails`.
- **Proposed fix:** Declare `mode` as a required query enum (`link`, `bootstrap`), `Produces(302)` with
  `Location`, remove 204, and use `ProducesProblem<SharedProblemDetails>` so only one problem component
  exists. Or exclude the route as a browser-navigation route (same decision as F-006).
- **Owner:** contract/endpoint (`challenge-entra-endpoint-server.cs`).

---

### F-014 — Path style is mixed: PascalCase `/api/Feedback`, `/api/Roles`, `/api/Users/Current/Profile`, `/api/Hello` beside kebab `/api/admin/feedback`, `/api/agent-links`, `/api/identity/...`; `/Analytics/TrackEvent` and `/identity/sign-out` sit outside `/api`

- **Severity:** minor
- **Lens:** consistency
- **Pointer:** `/paths/~1Analytics~1TrackEvent/post`; `/paths/~1api~1Users~1Current~1Profile/get`;
  `/paths/~1api~1Feedback/get`; `/paths/~1api~1admin~1feedback/get`; `/paths/~1identity~1sign-out/post`
- **Evidence:** User-facing feedback is `/api/Feedback/{feedbackItemId}` while admin feedback is
  `/api/admin/feedback/{feedbackItemId}`; the user's profile is `/api/Users/Current/Profile` while the
  user's key is `/api/Users/Current/xai-api-key`; identity is `/api/identity/...` but sign-out is
  `/identity/sign-out`. `track-event-contracts.cs:6-8` says the analytics route is "the historical path
  without an api/ prefix — preserve exactly". `route-grammar.md` covers parameter tokens only; there is
  no route-casing rule, so nothing enforces a style. Redocly also flags `/api/Feedback/attachments/{attachmentId}`
  vs `/api/Feedback/{feedbackItemId}/replies` as ambiguous (`no-ambiguous-paths`); ASP.NET prefers the
  literal segment, so it is not a runtime bug, but it is a sign the user and admin trees grew separately.
- **Proposed fix:** Pick one grammar (kebab-case segments under `/api/`), write it in
  `tw-web-api-contracts/references/route-grammar.md`, and enforce it with an analyzer on `[ApiRoute]`
  (the `[ApiRoute]` string is the single wire source, so renames do not drift). The SPA and the ingress
  golden (`WebServerApiRoutePrefixes.g.cs`) regenerate from the same attributes. This is a breaking change
  for external callers of the template demo; the template repo's policy is to fix debt rather than carry
  it, but the rename should be its own task.
- **Owner:** contract (`[ApiRoute]` strings) and skill (`route-grammar.md`), analyzer for enforcement.

---

### F-015 — `servers[0].url` has a trailing slash

- **Severity:** minor
- **Lens:** validity
- **Pointer:** `/servers/0/url` (`"https://arch.timewarp.work/"`)
- **Evidence:** Redocly `no-server-trailing-slash` (error) and Spectral `oas3-server-trailing-slash`
  (warning). The URL is built in `public-request-origin*.cs` `BuildUrl` from the forwarded proto and host
  and is published by `open-api-public-server.cs:51`. Generators concatenate `servers[0].url + path`, giving
  `https://arch.timewarp.work//api/health`; most tolerate it, some do not.
- **Proposed fix:** `TrimEnd('/')` in `OpenApiPublicServer` (or `BuildUrl`), plus an assertion in the
  `OpenApiPublicServer_` tests.
- **Owner:** transformer (`open-api-public-server.cs`).

---

### F-016 — Internal names leak through the document: class-name operationIds, `PostgresDbContext`, `Web.Server`, `web-server/alive`

- **Severity:** nit
- **Lens:** security
- **Pointer:** `/info/title` (`TimeWarp.Architecture Web.Server API v1`); `/paths/~1api~1health/get/description`
  ("When the PostgresDbContext check is registered… Liveness is GET /web-server/alive");
  `/paths/~1api~1agent-chat~1completions/post/operationId`
- **Evidence:** None of this is a secret and the repo is public, so this is cosmetic for the template.
  For a generated downstream app it becomes a fingerprint of the stack. `/web-server/alive` and `/alive`
  both answer `200 Healthy` live; the health description names only the first.
- **Proposed fix:** Falls out of F-002/F-011 (operationIds) and F-010 (descriptions written for callers,
  not maintainers).
- **Owner:** generator; `open-api-discovery-coverage.cs:100-108` for the health text.

---

### F-017 — No root `tags` array, no `info.contact`, no `info.license`

- **Severity:** nit
- **Lens:** validity, consumability
- **Pointer:** `/info` (no `contact`, `license`); root (no `tags`) — see `/paths/~1api~1health/get/tags`
  for one of the 15 tag names used
- **Evidence:** Spectral `operation-tag-defined` ×79, `info-contact` ×1; Redocly `info-license` ×1. Tags in
  use: Analytics, Feedback, Hellos, Roles, Profiles, AgentChats, Principals, AgentLinks, BrowserLogs,
  MeteredCapability, Identity, Settings, Tip, Health. Scalar groups by tag already; a root `tags` list
  lets each group carry a description (for example which tags are admin, user, or agent surfaces, which
  would help F-003's consumers).
- **Proposed fix:** A document transformer that collects the tags in use and emits `tags` with a
  description per feature (source: a `[OpenApiTags]`/feature attribute or the namespace leaf). Set
  `info.contact.url` to the repo and `info.license` to the repo license.
- **Owner:** transformer (`common-server-module.cs` document options).

---

### F-018 — Two tip-jar operations publish identical but separately named response components; identical empty responses are 7 separate components

- **Severity:** nit
- **Lens:** contracts
- **Pointer:** `/components/schemas/TimeWarpArchitectureFeaturesTipSubmitTipPost_Response`;
  `/components/schemas/TimeWarpArchitectureFeaturesTipSubmitTip_Response`;
  `/components/schemas/TimeWarpArchitectureFeaturesAnalyticsTrackEvent_Response` (one of seven `{type: object}`)
- **Evidence:** 23 structural-duplicate groups (ignoring descriptions), e.g. `SetPrincipalAgentScopes_Command`
  = `SetRoleAgentScopes_Command`, `CreateRole_Command` = `UpdateRole_Command`, `GetRole_Response` =
  `GetRoles_RoleDto`, and seven empty `BaseResponse` subclasses. This is the contract-per-endpoint design
  and is defensible; it just inflates generated clients.
- **Proposed fix:** None required. If the empty `BaseResponse` noise bothers consumers, publish an empty
  200 (no content) for `Response : BaseResponse` with no members.
- **Owner:** contract (design choice), transformer if the empty-200 variant is wanted.

---

## Where the linters are wrong, or only half right

- **Redocly `security-defined` (58 errors).** 24 of these are on operations that are intentionally
  anonymous and say so in `description` ("Intentionally anonymous …", 23 generated) or that carry an
  explicit `security: []` (the 3 hand-added operations are *not* flagged). The remaining 34 are real
  (F-003). Fix both halves the same way: emit `security: []` on `[EndpointAllowAnonymous]` operations and
  a cookie requirement on session operations; then the rule is clean and meaningful.
- **Spectral `oas3-operation-security-defined` (17 warnings, "scope must be listed among scopes").**
  Wrong for an `http`/`bearer` scheme: OpenAPI defines `scopes` only for `oauth2` and `openIdConnect`.
  A non-empty scope array on an `http` requirement is legal in 3.1 and 3.2 (the spec says the array "MAY
  contain a list of role names which are required for the execution"). Spectral 6.17.1 is applying the
  oauth2 rule to a bearer scheme. No change needed; the scopes are correctly listed in prose on the scheme.
- **Redocly `no-ambiguous-paths` (1 warning).** Structurally true, not a runtime defect (F-014).
- **Redocly `operation-4xx-response` (1 warning, `/api/health`).** Health has `503`; the rule wants a
  4xx. Ignore.
- **Both tools on 3.2.0.** Neither reported a structural error. The document uses JSON Schema 2020-12
  idioms correctly (`type: ["null","string"]`, `oneOf` with `{type: null}`, `contentEncoding`/
  `contentMediaType`/`contentSchema` on the `PAYMENT-REQUIRED` header). No `nullable`, `example`,
  `x-enumNames`, or other 3.0-isms remain. The 3.2-specific keywords (`querystring`, `itemSchema`,
  `$self`) are not used, so nothing in the document actually requires 3.2 over 3.1.

## What looks right

- **Excluded-routes list vs reality.** All six `/.well-known/*` paths in `/x-timewarp-excluded-routes`
  return `200 application/json` live, and `info.description` carries the same six with reasons and links.
  `/openapi/v1.json` matches the snapshot; `/openapi/v1.yaml` is 404 (not advertised).
- **Health.** `GET /api/health` → `200 text/plain Healthy`, exactly as documented (`text/plain` string
  schema, 200/503). `/health`, `/alive`, `/web-server/alive` also answer.
- **402/503 rules.** The `info.description` paragraph, the `/api/tip` GET and POST, the `/api` aliases,
  and `/api/demo/metered-capability` agree: 402 with `PAYMENT-REQUIRED` only on those routes, 503 for
  disabled payment, never 402 elsewhere. The x402 header is modelled as base64 JSON with a `contentSchema`,
  which is the right 2020-12 construct. `/api/demo/metered-capability` carries `demo:invoke`. (Not probed,
  by the brief.)
- **OneOf publishing.** No `OneOfOf…` wrapper component appears anywhere; every generated operation has
  the success arm at 200 `application/json` and `SharedProblemDetails` at `default`
  `application/problem+json`. No dangling `$ref`s, no unreferenced components, 79 unique operationIds.
- **Bearer 401/403 documentation.** For the 18 bearer operations, 401 and 403 carry a problem body schema
  and a `WWW-Authenticate` header description that matches live behaviour: `GET /api/admin/feedback`,
  `/api/identity/credentials`, `/api/agent-links`, `/api/identity/agent/me` all returned `401
  application/problem+json` with `www-authenticate: Bearer` and the documented "A valid agent bearer token
  is required." detail.
- **Scopes.** The seven scopes used in operation requirements (`credential:manage`, `demo:invoke`,
  `feedback:note`, `feedback:read`, `feedback:severity`, `feedback:status`, `identity:read`) are exactly
  the seven listed as "Known scopes" on the scheme.
- **Anonymous reads behave as documented.** `/api/Hello?name=Reviewer` → `{"message":"Hello, Reviewer!"}`;
  `/api/identity/sign-in-methods` → `{"methods":["Passkey"]}` (matches the `SignInMethod` enum);
  `/api/identity/session` → the documented five-field shape with `principalId: null`;
  `/api/Users/Current/Profile` → the documented demo profile for an anonymous caller;
  `/api/settings/assistant` → name and avatar; `/api/identity/entra/choice` → `{"valid":false,"destination":"/"}`.
- **Enums** are string enums with PascalCase members, matching the `ContractSerializationDefaults` note in
  `common-server-module.cs`. `required` arrays are present on response DTOs.
- **DELETE operations** carry no request body (the `OpenApiDeleteRequestBody` transformer is doing its job).
