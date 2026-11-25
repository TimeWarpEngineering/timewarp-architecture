# OpenAPI operationId summary and descriptions from the Operation attribute

## Description

76 of 79 operationIds are endpoint class names (`TimeWarpArchitectureFeaturesAdminRolesGetRolesEndpoint`);
the three hand-added ones are camelCase (`getApiHealth`). The contract's `[Operation]` id
(`roles.list`) and description do not reach the document. `JWTBearerAuth`'s description tells agents
to call `start-agent-key-registration` and three other kebab ids that are not operationIds. 38
operations have no `summary`; some summaries are a command's XML summary ("The new key. It is not
returned.") or a route string ("POST api/Feedback/attachments"). 0 of 52 parameters and 58 of 462
schema properties have a description (for example the required `userId` and the OData parameters).

## Requirements

- `operationId` is the contract's `[Operation]` id (sanitized if a generator needs it); the hand-added
  operations follow the same style.
- `summary` is `[Operation].Description`; `description` carries the longer XML summary or remarks.
- The bearer scheme's reviewer guide reads the operation ids from the contracts, so it cannot drift.
- Parameters and schema properties carry the XML `<summary>` of their members; shared parameter sets
  get fixed descriptions from their shared source.
- Optional: schema reference ids drop the `TimeWarpArchitectureFeatures` prefix.
- Document checks: every operation has a `summary`; every operationId quoted in any description
  resolves; consider making `[Operation]` with a description mandatory (TWA0035 severity).

Owner: generator (`source/analyzers/timewarp-architecture-analyzers/generators/` fast-endpoint emitter, `endpoint-documentation-resolver.cs`); transformer `open-api-agent-reviewer-guide-server.cs`.

## Checklist

- [ ] Implement the requirements above
- [ ] Tests or document checks named above pass
- [ ] Re-fetch the live document after deploy and confirm the finding is gone

## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-06 and M-16 (Grok G-006, Fable F-002, F-010, F-011, F-016) in `kanban/.../458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`
  (lands with task 458); reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.

## Session

- Created: 2227132 (2026-11-24)
