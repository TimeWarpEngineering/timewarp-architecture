# Publish required request members from validator rules in OpenAPI

## Description

33 of 34 `*_Command` schemas have properties and no `required` array. `CreateRole_Command`
publishes `name` as optional; the xAI key command publishes `apiKey` as optional while its
description says it is required. The only command with `required` is `RequestAgentHumanLink_Command`,
whose `[Required]` exists only to mark OpenAPI. FluentValidation `NotEmpty`/`NotNull` rules do not
reach the document, so generated clients treat names, signatures, challenges, and keys as optional.

## Requirements

- Emit `required` from validator `NotEmpty`/`NotNull` rules (and non-nullable members where that is
  the contract), from the same validator the handler pipeline runs.
- Remove the OpenAPI-only `[Required]` backfill once the generated rule covers it.
- Add a document check that each member with such a rule is listed in `required`.
- Write the rule in `skills/tw-web-api-contracts/` (the validator is the source of `required`).

Owner: schema pipeline / transformer in foundation-server; skill `tw-web-api-contracts`.

## Goal (implementer restatement)

Generated clients must see which request members the server rejects when they are missing. Today
the OpenAPI document leaves `required` off almost every command, because FluentValidation rules
never reach schema export, and the one exception uses a hand-written `[Required]` that only exists
for the document.

Acceptance:

- The OpenAPI pipeline in foundation-server reads the same `IValidator<TRequest>` instances
  `FluentValidationBehavior` runs and lists a member in `required` (body schema, or a required
  query parameter) when an unconditional `NotEmpty`/`NotNull` rejects the value an omitted
  member keeps. Rules through `RuleFor(x => x).SetValidator(...)` and `Include(...)` count;
  `When`/`Unless`, `RuleForEach`, named rule sets, nested paths, and an initialized `NotNull`
  member (`= []`) do not.
- `RequestAgentHumanLink.Command.HumanPrincipalId` loses `[Required]` and stays required.
- A document check fails `/openapi/{doc}.json` when a member with such a rule is not required in
  the final document.
- Tests show `CreateRole` (name, description), `SetXaiApiKey` (apiKey), `SubmitFeedback` (title,
  body; not attachmentIds, pagePath, emailCopy), the agent link id, the full web-server document
  building with every contract validator, and the check failing when `required` is stripped.
- `skills/tw-web-api-contracts/` states that the validator is the source of `required`.

## Checklist

- [x] Implement the requirements above
- [x] Tests or document checks named above pass

## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-07 (Grok G-005) in `kanban/.../458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`
  (lands with task 458); reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.
- Post-deploy verification: re-fetch the live OpenAPI document after deploy and confirm the finding is gone.

## Session

- Created: 2227132 (2026-11-24)
- Implemented 2026-11-26 (implement oracle, ganda task work). FastEndpoints sets FluentValidation's
  global `PropertyNameResolver` (camelCase), so the member name comes from the rule's lambda, not
  `IValidationRule.PropertyName`.

## Results

New `OpenApiValidatorRequired` (`source/foundation/foundation-server/open-api-validator-required.cs`):
a schema transformer adds `required` to validated mediator request schemas, an operation
transformer marks matching query/header parameters required and records the names in the
operation's (unserialized) Metadata, and a document check registered last in
`CommonServerModule.AddOpenApi` throws when a recorded member is not required in the final
document. `[Required]` removed from `RequestAgentHumanLink.Command`. Skill rule added to
`skills/tw-web-api-contracts/SKILL.md` §8 and Common pitfalls.

### How to validate

Smoke: `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-required-request-members-tests.cs`
Expect: 6 tests pass across `Document_Given_` and `RequiredStripped_Given_`. After deploy, re-fetch
`https://arch.timewarp.work/openapi/v1.json`: `CreateRole_Command` lists `required: [name, description]`,
the xAI key command lists `apiKey`, and `RequestAgentHumanLink_Command` lists `humanPrincipalId`.

### Gates

- `./bin/dev build` — passed (0 warnings, 0 errors).
- `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-required-request-members-tests.cs` — passed 6/6 (not a standalone runfile: suite project, no `#if !JARIBU_MULTI` guard; run via dev test).
- `./bin/dev test tests/container-apps/web/web-server-integration-tests/open-api-request-agent-human-link-tests.cs` — passed 2/2 (suite project, run via dev test).
- Covering tests for the `CommonServerModule.AddOpenApi` change (suite project, via dev test): `open-api-payment-responses-tests.cs`, `open-api-binary-content-tests.cs`, `open-api-bearer-security-tests.cs`, `open-api-problem-details-tests.cs`, `open-api-browser-navigation-tests.cs`, `open-api-delete-request-body-tests.cs` — 6/6 files passed, 0 failures.
- `./bin/dev lint-skills` (skills/ changed) — passed, 11 skills.

- `./bin/dev check-version` — source 2.0.0-beta.32 is ahead of NuGet 2.0.0-beta.31; no bump needed for the TimeWarp.Foundation.Server change.
- `ganda repo audit` — passed with 1 advisory warning (`vscode-window-icon`: local `.vscode/settings.json` missing). `ganda repo audit --fix` wrote that gitignored local file and the check passes; nothing to commit from it.
