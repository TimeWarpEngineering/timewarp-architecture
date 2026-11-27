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

## Goal (implementer restatement)

Make the published OpenAPI document name and describe every operation from the contract's
`[Operation]` attribute, and make that attribute mandatory so the document cannot regress.

Acceptance criteria:

- Every hosted `[ApiEndpoint]` contract carries `[Operation("<resource>.<action>", Description = "…")]`.
- `TWA0035` is a `Warning` (a build error under warnings-as-errors) and also fires when
  `Description` is missing or whitespace. Analyzer tests, `AnalyzerReleases.Unshipped.md`, and the
  `tw-web-api-contracts` skill say so.
- Each OpenAPI `operationId` is the contract's `[Operation]` name (`roles.list`); the hand-added
  health and `/api` alias operations use the same `<resource>.<action>` style.
- `summary` is the `[Operation]` description. `description` is the XML `<summary>` and `<remarks>`,
  without repeating the summary text.
- The `JWTBearerAuth` reviewer guide names the four ceremony operations from their generated
  `OperationName` constants, and its test reads the same constants.
- Request parameters and schema properties carry their members' XML `<summary>`.
- Schema component ids keep the `TimeWarpArchitectureFeatures` prefix.
- A document check (and a test) fails when an operation has no `summary` or when an operation id
  quoted in a description does not resolve to an operation in the document.

## Checklist

- [x] Backfill `[Operation("<resource>.<action>", Description = "…")]` on all hosted contracts that lack it
- [x] Promote `TWA0035` to `Warning` and require non-whitespace `Description`; update analyzer tests and `tw-web-api-contracts` skill docs
- [x] Emit OpenAPI `operationId` from `[Operation]` id, `summary` from `[Operation].Description`, `description` from XML docs, and parameter/property XML `<summary>` descriptions
- [x] Wire `open-api-agent-reviewer-guide-server.cs` to read contract `OperationName` constants and update `open-api-agent-reviewer-guide-tests.cs`
- [x] Add OpenAPI document tests asserting every operation has a `summary` and every quoted `operationId` resolves
- [x] Verify `./bin/dev build`, targeted `./bin/dev test` (analyzer, generator, and OpenAPI suites), `./bin/dev lint-skills`, and `ganda repo audit`

## Results

What changed:

- **Contracts.** Every hosted contract now has `[Operation("<resource>.<action>", Description = "…")]`
  (about 47 added: identity, settings, agent-chat, agent-links, admin/principals, analytics,
  browser-logs, hello, metered-capability, tip, and the api-server weather and agent-bearer
  samples). The four ceremony ids are `agent-key-registration.start`, `agent-key-registration.complete`,
  `agent-token-issuance.start`, and `agent-token-issuance.complete`. Request, response, DTO, and
  `I*Details` members got one-line XML `<summary>` text. The agent-links operations carry no
  `Group`: a `Group` links them to the `AgentHumanLink` aggregate under TWA0041/TWA0042, and those
  contracts do not project from that root. `api-contracts` gained `global using TimeWarp.Contracts;`.
- **TWA0035** is `Warning` and also fires on a missing or whitespace `Description`
  (`operation-attribute-analyzer.cs`, `AnalyzerReleases.Unshipped.md`, two new analyzer tests).
- **Generator.** `EndpointDocumentationResolver` puts `[Operation].Description` first for the
  summary. The description is the XML `<summary>` plus `<remarks>`, without a part equal to the
  summary. The contracts generator writes an XML `<summary>` on each generated route member
  ("The RoleId value in the route api/admin/roles/{RoleId:guid}."), so every path parameter has a
  description.
- **Foundation (`OpenApiOperationNames`, `OpenApiMemberDocumentation`, `OpenApiOperationName`).**
  `operationId` is the request's generated `OperationName` and `summary` is its
  `OperationDescription`. Hand-written `EndpointWithoutRequest` endpoints (Entra challenge,
  sign-out, sign-out antiforgery token) pass `OpenApiOperationName` metadata. A parameter or
  schema property that FastEndpoints left undescribed gets its member's XML summary, or the
  summary on the same-named `I*Details` interface property. The document check (registered last)
  fails on a repeated operationId and on a backtick-quoted operation id that resolves to nothing.
  The summary and `<resource>.<action>` rules are a test on the real web-server document, not
  the runtime check, because other test hosts publish probe endpoints with no contract (the
  stricter runtime check failed 20 probe tests in other suites).
- **Hand-added operations.** `getApiHealth` is now `health.get`. The `/api` aliases are the tip
  operation id plus `-alias` (`tip.get-alias`, `tip.submit-alias`).
- **Reviewer guide.** `JWTBearerAuth` quotes the four ceremony ids from `OperationName`
  constants in backticks. The ceremony remarks quote their partner operation the same way.
- **Skills.** `tw-web-api-contracts` SKILL.md and `references/neutral-operations.md` say TWA0035
  is a warning that requires `Description`, and describe the OpenAPI projection.
- Schema component ids keep the `TimeWarpArchitectureFeatures` prefix (as decided).

Gates:

- `./bin/dev build`: passed, 0 warnings, 0 errors.
- `./bin/dev test --no-build tests/analyzers/timewarp-architecture-analyzers-tests/operation-attribute-analyzer-tests.cs`: passed 9/9.
- `./bin/dev test --no-build tests/analyzers/timewarp-architecture-sourcegenerator-tests/fast-endpoint-source-generator-summary-tests.cs`: passed 11/11.
- `./bin/dev test --no-build tests/analyzers/timewarp-architecture-sourcegenerator-tests/contracts-generator-core-tests.cs`: passed 15/15 (covers the contracts emitter change).
- `./bin/dev test --no-build tests/analyzers/timewarp-architecture-sourcegenerator-tests/contracts-generator-route-tests.cs`: passed 1/1 (covers the contracts emitter change).
- `./bin/dev test --no-build tests/container-apps/web/web-server-integration-tests/open-api-operation-names-tests.cs` (new): passed, Document_Given_ 9/9 and StaleQuote_Given_ 1/1.
- `./bin/dev test --no-build tests/container-apps/web/web-server-integration-tests/open-api-agent-reviewer-guide-tests.cs` (edited, full web host): passed.
- `./bin/dev test --no-build tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs` (every OpenAPI suite, which covers the foundation transformer change): 23 classes passed, 0 failed.
- These test files have no `#if !JARIBU_MULTI` guard, so they are not standalone runfiles. Each ran through `./bin/dev test <file>`.
- `./bin/dev lint-skills`: passed, 11/11 skills.
- `./bin/dev check-version`: source 2.0.0-beta.32 is ahead of NuGet 2.0.0-beta.31, so no bump is needed.
- `dev template-smoke` was not run. CI runs the default variant on the PR.
- `ganda repo audit` (after the last product edit): passed, 43 checks, 0 failed. The 1 warning is `vscode-window-icon` (VS Code window-icon configuration), which this change does not touch.

### How to validate

Smoke: `./bin/dev build`, then `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-operation-names-tests.cs tests/container-apps/web/web-server-integration-tests/open-api-agent-reviewer-guide-tests.cs`.
After deploy, fetch `https://arch.timewarp.work/openapi/v1.json`.

Expect: the build is clean. Both test files pass. In the document every `operationId` is
`<resource>.<action>` (`roles.list`, `agent-key-registration.start`, `health.get`). Every
operation has a `summary` equal to its `[Operation]` description. Every parameter has a
`description`. The `JWTBearerAuth` description quotes the four ceremony ids in backticks, and each
one is an operation. Removing a `Description` from any hosted contract fails the build with TWA0035.

Follow-up: response-model and shared-library members outside the feature folders (for example
`EntraAcceptedTenant` in `libraries/timewarp-identity`, and enum types) still have no XML
summaries, so some schema properties stay undescribed. Nothing checks schema property
descriptions yet.

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
- 2026-11-27 implement: backfilled `[Operation]` and member docs (three parallel helpers for the contract files), raised TWA0035, flipped the summary order, added `OpenApiOperationNames` and its document check and tests, and wired the reviewer guide to the `OperationName` constants.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 3bbfb902-945d-4636-9476-e47faea8e15e, max-turns 500, Done — 2026-11-27T04:27:37Z
