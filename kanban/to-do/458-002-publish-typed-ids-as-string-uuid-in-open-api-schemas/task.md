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

## Checklist

- [ ] Implement the requirements above
- [ ] Tests or document checks named above pass

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
