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

## Checklist

- [ ] Implement the requirements above
- [ ] Tests or document checks named above pass

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
