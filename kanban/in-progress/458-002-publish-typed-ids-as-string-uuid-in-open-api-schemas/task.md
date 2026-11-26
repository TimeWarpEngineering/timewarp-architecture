# Publish typed ids as string uuid in OpenAPI schemas

## Description

`/components/schemas/TimeWarpIdentityPrincipalId` and `TimeWarpIdentityCredentialId` have only a
`description`, and `SystemNullableOfTimeWarpIdentityPrincipalId` is `{}`. The session response's
`principalId` is `oneOf [null, $ref {}]`. On the wire the `[TypedId]` converter writes a Guid string.
Kiota and NSwag emit `object`/`any` for these properties, including the token-issuance response an
agent must parse.

## Requirements

- A schema transformer maps every `[TypedId]` type to `type: string, format: uuid` (keep the
  description) and `Nullable<T>` of one to `type: ["null","string"], format: uuid`.
- The set of typed ids comes from the typed-id generator or the type shape, not a hand list.
- Add a document test: no component schema is `{}`, and none lacks `type`, `oneOf`, `anyOf`, `allOf`,
  `$ref`, or `enum`.

Owner: transformer in `source/foundation/foundation-server/`; typed-id generator as the source.

## Goal (implementer restatement)

Every `[TypedId]` struct serializes as a Guid string, but the OpenAPI document describes it as
an untyped schema, so generated clients see `object`/`any`. Fix the document, not the wire.

Acceptance:

- A schema transformer in foundation-server publishes every `[TypedId]` type as
  `type: string, format: uuid`, keeping any description, and `Nullable<TypedId>` as
  `type: ["null","string"], format: uuid`.
- Typed ids are recognized from what the typed-id generator stamps on the type (the
  `TypedIdAttribute` it injects, matched by simple name like the generator does, plus the
  generated `Guid Value`), never from a list of type names.
- A document test over the web-server endpoints fails when any component schema is `{}` or has
  none of `type`, `oneOf`, `anyOf`, `allOf`, `$ref`, `enum`; it also pins the PrincipalId,
  CredentialId, and nullable PrincipalId components to the string-uuid shape.

## Checklist

- [x] Implement the requirements above
- [x] Tests or document checks named above pass

## Review

- Round 1: Done

## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-03 (Grok G-002, Fable F-008) in `kanban/.../458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`
  (lands with task 458); reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.
- Post-deploy verification: re-fetch the live OpenAPI document after deploy and confirm the finding is gone.

## Session

- Created: 2227132 (2026-11-24)
- Implement (2026-11-26): `OpenApiTypedId` schema transformer added; root cause of the first
  failing run was registration order (see Results).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 3ea3591e-7a5a-4e0f-875f-06ddb9996f43, max-turns 500, Done — 2026-11-26T00:58:22Z
- Review (2026-11-26): round 1, effort 2, roster general. Reviewer: grok session 01a2155c-7760-7680-b020-1670544592ee. Disposition clean, 0 open findings.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a2155c-7760-7680-b020-1670544592ee, max-turns 120, artifacts general.md, merged.md, disposition.md — 2026-11-26T01:09:24Z

## Results

- `source/foundation/foundation-server/open-api-typed-id.cs`: `OpenApiTypedId` schema transformer.
  A typed id is a value type carrying the generator-injected `TypedIdAttribute` (matched by simple
  name, as the generator does, since each assembly has its own internal copy) and a `Guid Value`
  property. It gets `type: string, format: uuid`; `Nullable<T>` of one gets
  `type: [null, string], format: uuid`. Description and component names are unchanged.
- `common-server-module.cs`: registered **before** `OpenApiOneOf`. ASP.NET OpenApi 11 runs each
  schema transformer as its own parent-first walk; the OneOf transformer replaces the union's
  members with `oneOf`, which the walk does not follow. Registered after it, the transformer
  never saw a response DTO (first test run: all 4 tests failed, components still `{}`). The
  existing ordering comment now names this for every schema transformer.
- `tests/container-apps/web/web-server-integration-tests/open-api-typed-id-tests.cs`
  (`OpenApiTypedId_.Document_Given_`): no component schema lacks `type`/`oneOf`/`anyOf`/`allOf`/
  `$ref`/`enum` (covers `{}`); PrincipalId and CredentialId components are string uuid with a
  description; `SystemNullableOfTimeWarpIdentityPrincipalId` is `[null, string]` uuid; every
  component's `principalId` property resolves to string uuid (10 holders, including the
  token-issuance and current-session responses).

### Gates

- `./bin/dev build`: passed, 0 warnings, 0 errors.
- `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-typed-id-tests.cs --no-build`: 4/4 passed.
- `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-one-of-tests.cs --no-build`: 9/9 passed (registration order changed).
- `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-problem-details-tests.cs --no-build`: 5/5 passed (registration order changed).
- The test files are in the `web-server-integration-tests` csproj, not standalone runfiles (no
  `#if !JARIBU_MULTI` guard); each was run alone through `dev test <file>`.
- `ganda repo audit`: passed. One advisory warning, `vscode-window-icon` (`.vscode/settings.json` missing). That file is gitignored machine-local config, not repo content, so nothing to commit.

### How to validate

Smoke: `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-typed-id-tests.cs`,
or run the app and fetch `/openapi/v1.json`.

Expect: 4/4 pass. In the document, `components.schemas.TimeWarpIdentityPrincipalId` and
`TimeWarpIdentityCredentialId` are `{ "type": "string", "format": "uuid", "description": … }`,
`SystemNullableOfTimeWarpIdentityPrincipalId` is `{ "type": ["null","string"], "format": "uuid" }`,
and no component schema is `{}`. Post-deploy: re-fetch `https://arch.timewarp.work/openapi/v1.json`
and confirm the same.

### Review

- Round 1, effort 2, roster general (`review/round-1/general.md`).
- Counts: bug 0, suggestion 0, nit 0; open 0, fixed 0, wontfix 0.
- Disposition: clean (`review/disposition.md`). No wontfix and no escalation.
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.
- The implementer restatement matches the task. No misread.
