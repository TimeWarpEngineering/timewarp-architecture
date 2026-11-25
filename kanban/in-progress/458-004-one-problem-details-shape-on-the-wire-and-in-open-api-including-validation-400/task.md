# One problem-details shape on the wire and in OpenAPI including validation 400

## Description

Live `GET /api/Hello` without `name` returns `400` with `content-type: application/json` and a body
with an `errors` map. Every generated operation documents `default` as `application/problem+json`
with `TimeWarpFoundationTypesSharedProblemDetails` (`type,title,status,detail,instance`, no
`errors`). The document also has `MicrosoftAspNetCoreMvcProblemDetails` (used by the Entra
challenge), and bearer 401/403 and payment 402/503 use their own inline problem objects.

## Requirements

- FastEndpoints validation failures are written as `application/problem+json`.
- The one problem schema declares `errors` (map of string arrays) and allows extension members
  (`traceId`).
- Operations with a validator document `400` with that schema.
- Every problem response (default, 400, bearer 401/403, payment 402/503, Entra 400/403) references
  that one component; `MicrosoftAspNetCoreMvcProblemDetails` is gone from the document.
- Test: `GET /api/Hello` without `name` is `application/problem+json` and the published schema lists
  `errors`.

Owner: host config (`UseFastEndpoints` error builder in `source/container-apps/web/projects/web-server/program.cs` and `source/container-apps/api/projects/api-server/program.cs`, or a shared helper in `common-server-module.cs`); transformers `open-api-one-of.cs`, `open-api-bearer-security-server.cs`, `open-api-payment-responses-server.cs`; `challenge-entra-endpoint-server.cs`.

## Goal (implementer restatement)

Problem responses have one shape on the wire and one schema in the OpenAPI document.

- A validation failure (mediator FluentValidationBehavior → SharedProblemDetails) goes out as
  `application/problem+json`, not `application/json`. The cause is `WriteAsJsonAsync` without a
  content type, which overwrites the header set before it.
- The `TimeWarpFoundationTypesSharedProblemDetails` component declares `errors` (map of string
  arrays) and allows extension members (`additionalProperties`).
- An operation whose request type has a registered validator documents `400` with that schema.
- Every problem+json response (default, validation 400, bearer 401/403, payment 402/503, Entra
  400/403) references that component, and `MicrosoftAspNetCoreMvcProblemDetails` is gone.
  A document check fails `/openapi/{doc}.json` if a problem+json response names another schema.

Acceptance: `GET /api/hello` with no name returns 400 `application/problem+json` with an `errors`
object; the published document's problem component lists `errors`, the Hello 400 references it,
and no other `*ProblemDetails` component remains. Existing OpenAPI tests updated and passing.

## Checklist

- [x] Implement the requirements above
- [x] Tests or document checks named above pass

## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-05 (Grok G-004, Fable F-009, F-013 part) in `kanban/.../458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`
  (lands with task 458); reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.
- Post-deploy verification: re-fetch the live OpenAPI document after deploy and confirm the finding is gone.

## Results

What changed:

- Wire: `HttpResponse.WriteAsJsonAsync` without a content type writes `application/json` and
  replaces a Content-Type set before it, so every SharedProblemDetails body (validation 400
  included) went out as `application/json`. New `SharedProblemDetailsHttp.WriteAsync`
  (`source/foundation/foundation-server/base/shared-problem-details-http.cs`) passes
  `application/problem+json; charset=utf-8` to that call and sets the problem's status. Used by
  `BaseFastEndpoint`, the feedback attachment download and upload (413) endpoints, and
  `EntraTicketHttp.WriteProblemAsync`. Validation runs in the mediator (FluentValidationBehavior),
  not the FastEndpoints validator, so the FastEndpoints error builder was not the path to change.
- OpenAPI: new `OpenApiProblemDetails` (`source/foundation/foundation-server/open-api-problem-details.cs`),
  registered in `CommonServerModule.AddOpenApi`:
  - schema transformer: the `TimeWarpFoundationTypesSharedProblemDetails` component declares
    `errors` (object of string arrays) and `additionalProperties: {}` for extension members
    (traceId and others). Registered before `OpenApiOneOf`, whose transformer removes the union's
    members before a later transformer could reach the problem arm.
  - operation transformer: an operation whose request type has a registered `IValidator<T>`
    documents `400` with the component.
  - `ReferenceAsync`: the `$ref` helper other transformers use; reads the component id from the
    document service (`x-schema-id`), so no transformer spells the id.
  - document check (last, after the host callback): throws when any problem+json response does not
    reference the component (directly or as the first allOf member), naming operation and status;
    removes MVC `ProblemDetails` / `HttpValidationProblemDetails` components nothing references.
- Bearer 401/403 (`open-api-bearer-security-server.cs`, `open-api-operation-security-server.cs`):
  `$ref` to the component instead of an inline object.
- Payment 402/503 (`open-api-payment-responses-server.cs`): `allOf: [$ref component, {payment,
  reason | error, ok, tip}]`, so the extension members the payment gate writes stay documented.
- Entra challenge (`challenge-entra-endpoint-server.cs`): `Produces<SharedProblemDetails>(400|403,
  application/problem+json)` instead of `ProducesProblem`, which is what put
  `MicrosoftAspNetCoreMvcProblemDetails` in the document.

Tests:

- New `tests/container-apps/web/web-server-integration-tests/open-api-problem-details-tests.cs`:
  component declares errors and allows extensions; no other `*ProblemDetails` component (a
  `ProducesProblem(422)` probe proves the MVC component is removed); every problem+json response
  over web-server endpoints plus probes is a `$ref` to the component (scan covers default, 400,
  401, 403, 422); a validated probe request publishes 400; a request without a validator does not.
- `source/container-apps/web/features/hello/hello/hello-tests.cs` (real web host): `GET /api/hello`
  without name is 400 `application/problem+json` with an `errors` object; the published document's
  component lists `errors`, Hello's 400 references it, no other `*ProblemDetails` component.
- `open-api-bearer-security-tests.cs` and `open-api-payment-responses-tests.cs` assert the `$ref` /
  allOf form instead of inline properties.

Gates:

- `./bin/dev build` — passed (0 warnings, 0 errors).
- `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-problem-details-tests.cs` — passed 5/5.
- `dotnet run --file source/container-apps/web/features/hello/hello/hello-tests.cs` — passed 4/4.
- `./bin/dev test` on every `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`
  (13 files, including the edited `open-api-bearer-security-tests.cs` and
  `open-api-payment-responses-tests.cs`, and the new file) plus
  `tests/container-apps/api/api-server-integration-tests/features/open-api/open-api-document-tests.cs`
  — 14 of 14 projects passed (they cover `CommonServerModule.AddOpenApi`, which every one boots).
- Covering tests for the changed problem writers: `./bin/dev test` on
  `features/feedback/upload-feedback-attachment-endpoint-tests.cs`,
  `features/identity/entra-oidc-handler-round-trip-tests.cs`, `features/identity/local-return-url-tests.cs`,
  `features/identity/entra-public-origin-tests.cs` — 4 of 4 projects passed;
  `dotnet run --file source/container-apps/web/features/feedback/feedback-attachment-tests.cs` — passed 14/14.
- `ganda repo audit` — failed `kanban-landed` (task 530's kitchen not landed on origin-home; not this
  task) and `vscode-window-icon` (local `.vscode/settings.json` missing, git-ignored). Boy-scout fix:
  `ganda repo audit --fix --checks kanban-landed,vscode-window-icon` landed 530's kitchen and wrote
  the local VS Code setting; the same run's audit passed (44 passed, 0 failed).

### How to validate

Smoke: run `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-problem-details-tests.cs`
and `dotnet run --file source/container-apps/web/features/hello/hello/hello-tests.cs`. Against a running
web host, `curl -i '<host>/api/hello'` and `curl -s '<host>/openapi/v1.json'`.

Expect: both test runs pass. The curl returns `400` with `content-type: application/problem+json`
and an `errors` object. In the document, `components.schemas.TimeWarpFoundationTypesSharedProblemDetails.properties.errors`
exists, `paths./api/hello.get.responses.400` references that component, no
`MicrosoftAspNetCoreMvcProblemDetails` component exists, and every `application/problem+json`
response is a `$ref` to it (payment 402/503 as the first `allOf` member). After deploy, re-fetch
`https://arch.timewarp.work/openapi/v1.json` and confirm the same.

## Session

- Created: 2227132 (2026-11-24)
- 2026-11-26 implementer: wire fix (SharedProblemDetailsHttp), OpenApiProblemDetails transformer + document check, bearer/payment/Entra point at the one component; gates in ## Results.
