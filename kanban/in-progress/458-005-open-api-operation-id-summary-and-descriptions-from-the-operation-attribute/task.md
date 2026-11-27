# OpenAPI operationId summary and descriptions from the Operation attribute

## Description

76 of 79 operationIds are endpoint class names (`TimeWarpArchitectureFeaturesAdminRolesGetRolesEndpoint`);
the three hand-added ones are camelCase (`getApiHealth`). The contract's `[Operation]` id
(`roles.list`) and description do not reach the document. `JWTBearerAuth`'s description tells agents
to call `start-agent-key-registration` and three other kebab ids that are not operationIds. 38
operations have no `summary`; some summaries are a command's XML summary ("The new key. It is not
returned.") or a route string ("POST api/Feedback/attachments"). 0 of 52 parameters and 58 of 462
schema properties have a description.

Only 29 of ~79 hosted contracts currently carry `[Operation]`, because `TWA0035` is `Info` and does
not require `Description`.

## Requirements

- **Backfill `[Operation]` on all hosted contracts:** Add `[Operation("<resource>.<action>", Description = "…")]`
  to every hosted `[ApiEndpoint]` contract that lacks one (~50 contracts, including all four agent
  ceremony contracts: `StartAgentKeyRegistration`, `CompleteAgentKeyRegistration`,
  `StartAgentTokenIssuance`, `CompleteAgentTokenIssuance`, plus `settings/*`, `todo-items/*`,
  `agent-links/*`, `hello/*`, `tip/*`, `metered-capability/*`, `analytics/*`, `browser-logs/*`,
  `identity/*`, and `admin/principals/*`).
- **Promote `TWA0035` to `Warning` (= build error) and require `Description`:** Update
  `OperationAttributeAnalyzer` (`source/analyzers/timewarp-architecture-convention-analyzers/operation-attribute-analyzer.cs`)
  and `AnalyzerReleases.Unshipped.md` so missing `[Operation]` or missing/whitespace `Description`
  on a hosted contract is a `Warning`. Update `skills/tw-web-api-contracts/SKILL.md` and
  `skills/tw-web-api-contracts/references/neutral-operations.md` to reflect `Warning` + required
  `Description` (and update analyzer tests).
- **`operationId`:** Emit `operationId` from the contract's `[Operation]` id (sanitized if a
  generator needs it); hand-added operations follow the same style.
- **`summary` and `description`:** Flip `EndpointDocumentationResolver` priority so `summary` comes
  from `[Operation].Description`, and `description` carries the longer XML `<summary>` / `<remarks>`
  (without duplicating `[Operation].Description`).
- **Bearer reviewer guide:** `open-api-agent-reviewer-guide-server.cs` reads the four agent ceremony
  operation ids from the contracts' generated `OperationName` constants so it cannot drift; update
  `open-api-agent-reviewer-guide-tests.cs` to match.
- **Parameter & schema property descriptions:** Parameters and schema properties carry the XML
  `<summary>` of their members; shared parameter sets get fixed descriptions from their shared source.
- **Schema reference IDs (decided: keep as-is):** Do **not** strip the
  `TimeWarpArchitectureFeatures` prefix from component schema IDs (keeps template `sourceName`
  renames safe and avoids cross-slice schema-name collisions).
- **Document checks:** Every operation has a `summary`; every `operationId` quoted in any
  description resolves to a real operation in the document.

Owner:
- Contracts (~50 hosted `*-contracts.cs` files missing `[Operation]`)
- Convention analyzer (`source/analyzers/timewarp-architecture-convention-analyzers/operation-attribute-analyzer.cs`, `AnalyzerReleases.Unshipped.md`, analyzer tests)
- FastEndpoint generator (`source/analyzers/timewarp-architecture-analyzers/generators/` fast-endpoint emitter, `models/endpoint-metadata.cs`, `models/endpoint-documentation-resolver.cs`)
- OpenAPI transformers & tests (`source/container-apps/web/platform/identity-host/open-api-agent-reviewer-guide-server.cs`, `source/foundation/foundation-server/`, `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`)
- Skill docs (`skills/tw-web-api-contracts/SKILL.md`, `skills/tw-web-api-contracts/references/neutral-operations.md`)

## Checklist

- [ ] Backfill `[Operation("<resource>.<action>", Description = "…")]` on all hosted contracts that lack it
- [ ] Promote `TWA0035` to `Warning` and require non-whitespace `Description`; update analyzer tests and `tw-web-api-contracts` skill docs
- [ ] Emit OpenAPI `operationId` from `[Operation]` id, `summary` from `[Operation].Description`, `description` from XML docs, and parameter/property XML `<summary>` descriptions
- [ ] Wire `open-api-agent-reviewer-guide-server.cs` to read contract `OperationName` constants and update `open-api-agent-reviewer-guide-tests.cs`
- [ ] Add OpenAPI document tests asserting every operation has a `summary` and every quoted `operationId` resolves
- [ ] Verify `./bin/dev build`, targeted `./bin/dev test` (analyzer, generator, and OpenAPI suites), `./bin/dev lint-skills`, and `ganda repo audit`

## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-06 and M-16 (Grok G-006, Fable F-002, F-010, F-011, F-016) in `kanban/in-progress/458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`;
  reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Decisions settled (2026-11-27, Steven): backfill all ~50 missing contracts in this task, promote
  `TWA0035` to `Warning` requiring `Description`, and skip stripping the
  `TimeWarpArchitectureFeatures` schema-ID prefix.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.
- Post-deploy verification: re-fetch the live OpenAPI document after deploy and confirm the finding is gone.

## Session

- Created: 2227132 (2026-11-24)
