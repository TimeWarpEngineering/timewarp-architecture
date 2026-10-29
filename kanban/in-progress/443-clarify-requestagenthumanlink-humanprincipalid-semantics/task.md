# Clarify RequestAgentHumanLink humanPrincipalId semantics

## Description

`RequestAgentHumanLink_Command.humanPrincipalId` is optional (non-required
uuid). Callers cannot tell from the spec alone what omitting it means
(self-link? error?) vs the successful humanPrincipalId-pinned flow used on
2026-10-28.

## Goal (implementer restatement)

The OpenAPI document shows `humanPrincipalId` on `POST /api/agent-links` as an optional uuid.
In code it is not optional: the property is a non-nullable `Guid`, the Validator rejects an
empty value, and the handler links the calling agent only to the human it names. There is no
self-link or default human. The published contract should say that.

Acceptance:

- In `/openapi/v1.json`, the `RequestAgentHumanLink` request schema lists `humanPrincipalId` in
  `required`, with a description of what it is and what omitting it does, and a uuid example.
- Runtime behavior does not change. An omitted or empty id is still a 400 from the Validator.
- `auth.md` and `llms.txt` describe the optional human link step and say the field is required.
- A test fails if the schema loses `required`, the description, or the example.

## Checklist

- [x] Document omitting vs providing humanPrincipalId
- [x] Mark required if pinning is the contract
- [x] Add description + example; align auth.md/llms.txt wording

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)
- Implemented under ganda task work (implement oracle), 2026-10-29.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session c5708c32-27fc-453a-b85b-abc0d8c7985c, max-turns 500, Done — 2026-10-29T15:37:50Z

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 2026-10-28.

## Results

Pinning is the contract. The handler links the calling agent only to the human the body names.
An omitted `humanPrincipalId` binds to `Guid.Empty` and the Validator's `NotEmpty` rule returns
400. There was no self-link path to document. Only the OpenAPI document was wrong: a
non-nullable value type without a required marker comes out optional.

- `request-agent-human-link-contracts.cs`: `HumanPrincipalId` now has
  `[System.ComponentModel.DataAnnotations.Required]`, an XML `<summary>` (the human's account id
  from the Agent links page; no default, no self-link; omitted or empty is 400), and an
  `<example>` uuid. The Design region records why. `[Required]` only affects the document,
  because nothing runs DataAnnotations validation at runtime and the Validator stays the rule.
  The C# `required` keyword was not used because it breaks the `new()` constraint that
  `AgentLinkOnboardingPrompt.HttpMethod<TRequest>` relies on.
- `wwwroot/auth.md`: new "Optional human link" subsection under Agents. It has a routes table
  (request, list, approve/deny, human-ux), the `humanPrincipalId` rules, the 400/403/404/409
  outcomes, and a body example.
- `wwwroot/llms.txt`: an agent-auth bullet for `POST /api/agent-links` that says the field is
  required with no default, and links to auth.md.
- New test `tests/container-apps/web/web-server-integration-tests/open-api-request-agent-human-link-tests.cs`
  asserts the request schema lists `humanPrincipalId` in `required`, and that the field is a
  `uuid` with a description and a non-empty uuid example. Both tests failed before the
  contract change and pass after it.

Gates run in this worktree: `ganda repo audit` passes. Its one advisory warning is
`kanban-landed`, about other tasks' claims with no worktree, which is out of scope here.
`dev build` passed with 0 warnings. `dev test -g fast` passed 14/14 projects and
`dev test -g integration` passed 8/8. `dev template-smoke` (default variant) succeeded.
`dev check-version` reports source 2.0.0-beta.29 is ahead of NuGet. The playwright group was
not run: it only checks that auth.md returns 200, and this change edits content only.

### How to validate

Smoke:

```bash
dotnet build tests/container-apps/web/web-server-integration-tests/web-server-integration-tests.csproj
(cd tests/container-apps/web/web-server-integration-tests/bin/Debug/net11.0 && ./web-server-integration-tests --filter-class OpenApiRequestAgentHumanLink)
grep -n "humanPrincipalId" source/container-apps/web/projects/web-spa/wwwroot/auth.md source/container-apps/web/projects/web-spa/wwwroot/llms.txt
```

Expect: 2/2 tests pass. In `/openapi/v1.json`, the `POST /api/agent-links` request schema has
`"required": ["humanPrincipalId"]`, and the property has `format: uuid`, a description that
says there is no default or self-link and that an omitted id is 400, and an example uuid. Both
auth.md and llms.txt say `humanPrincipalId` is required. A deployed host shows the same thing at
`<origin>/openapi/v1.json` after this ships.
