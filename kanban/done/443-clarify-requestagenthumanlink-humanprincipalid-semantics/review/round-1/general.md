# Round 1 — general
**Date:** 2026-10-29
**Scope reviewed:** commit d0f0dc2e4 (task 443) vs its parent

## Summary

The change does what the task asks. Runtime is untouched, the contract carries `[Required]`, a summary and an example, auth.md and llms.txt describe the step, and the new test follows the sibling OpenAPI test conventions. Nearly every behavioral claim in the prose checks out against the handler, validator, policies and routes. Two small documentation defects remain: a paragraph in auth.md is now orphaned under the new heading, and the "403 for a non-agent caller" sentence glosses over the 401 a human session actually gets.

## Verified claims

1. Handler and Validator: verified.
   - Non-agent caller is 403: `request-agent-human-link-handler-application.cs:105-109` (`caller.Kind != PrincipalKind.Agent` returns `AgentLinkProblems.Forbidden`, status 403 at `agent-link-problems-application.cs:24`). See Issue 2 for the caveat on which callers can reach it.
   - Non-human or missing target is 404: handler lines 111-116, `HumanNotFound()` is status 404 (`agent-link-problems-application.cs:38`).
   - Existing pending-or-approved pair is 409: handler lines 118-122 and the `InvalidOperationException` race path at 129-132. `AlreadyLinked()` is 409 (`agent-link-problems-application.cs:45`).
   - A denied link can be requested again: handler Design region lines 63-64 ("An open (Pending/Approved) pair is 409 so a denied link can be requested again"), and `deny-agent-human-link-contracts.cs:6`.
   - No self-link or default-human path: the handler only uses `command.HumanPrincipalId` (line 111). Nothing falls back to `callerId`.
   - Validator has `RuleFor(command => command.HumanPrincipalId).NotEmpty()` at `request-agent-human-link-contracts.cs:42`. It is covered by the existing test `agent-human-link-tests.cs:79-82`.
2. Routes table: verified.
   - GET `/api/agent-links` is `list-agent-human-links-contracts.cs:20`. Schemes are IdentitySession, MockIdentitySession and AgentToken (line 16). The handler scopes by `ICurrentPrincipalAccessor` (Design region, lines 6-7).
   - POST `/api/agent-links/{LinkId:guid}/approve` is `approve-agent-human-link-contracts.cs:19`, and `/deny` is `deny-agent-human-link-contracts.cs:19`. Both use only IdentitySession and MockIdentitySession, so "Human session only" holds.
   - GET `/api/agent-links/{LinkId:guid}/human-ux` is `get-human-ux-contracts.cs:21`. It is AgentToken only (line 17). The handler returns `NotApproved()` (409) unless `Status == Approved` (`get-human-ux-handler-application.cs:58-60`).
   - Scope is `identity:read`: `agent-scope-permission-seed-application.cs:36-40` maps `AgentScopes.IdentityRead` to `IdentityRead` plus `AgentLinkManageSelf`. All five contracts use `PermissionIds.AgentLinkManageSelf`.
3. The `new()` constraint: verified. `agent-link-onboarding-prompt.cs:209` has `HttpMethod<TRequest>() where TRequest : IApiRequest, new()`, and line 92 calls `HttpMethod<RequestAgentHumanLink.Command>()`. A C# `required` member would make `new()` unusable for it (CS9040 on the generic instantiation).
4. DataAnnotations at runtime: verified none. `git grep` for `DataAnnotations`, `ValidateDataAnnotations`, `EnableDataAnnotations` and `AddValidation` over `source/` and `tests/` finds only the new attribute. `web-server/program.cs:29` and `344-345` say FluentValidationBehavior owns validation, so `[Required]` only affects the OpenAPI document.
5. Conflicting docs: no conflict found. Searching `source/` and `skills/` for "agent-links", "humanPrincipalId" and agent-link prose finds only the new text plus the onboarding prompt (`agent-link-onboarding-prompt.cs:107,166`). That prompt sends `humanPrincipalId` in every call and already says the Agent links page supplies "My account id" (line 118). `AgentLinksPage.razor:66` has a "Your account id" section, so "the Agent links page shows it" is accurate. Related doc defect: Issue 1.
6. Test conventions: verified, with one hedge.
   - It matches `open-api-delete-request-body-tests.cs` and `open-api-one-of-tests.cs`: namespace `OpenApi<Topic>_`, class `*_Given_`, `[TestTag("OpenApi")]`, `ModuleInitializer` calling `RegisterTests<>`, `SetupOnce`/`CleanUpOnce`, the same host setup, and Purpose and Design regions.
   - Filtering by the type name `RequestAgentHumanLinkEndpoint` matches the sibling `GetRoleEndpoint` / `DeleteRoleEndpoint` convention.
   - `Command.RouteTemplate` is a `const` emitted by the contracts generator (`contracts-generator.cs:468`).
   - XML docs flow: `Directory.Build.props:87` sets `GenerateDocumentationFile` repo-wide, and `web-server.csproj:22` sets it explicitly. The existing `get-weather-forecasts-contracts.cs:10` states that `<example>` tags reach the OpenAPI description. I did not build, so the end-to-end flow rests on the implementer's before/after test report (task.md Results).
   - `description.ShouldContain("account id")` against "Account id of the human…": the repo uses Shouldly 4.3.0 (`Directory.Packages.props:266`), and I believe its `string.ShouldContain` defaults to `Case.Insensitive`. I could not confirm that from the package XML, which lists only the `Case` parameter. The implementer reports the test passes, so this is low risk.
7. Repo conventions: verified. The new test has a Purpose region (`open-api-request-agent-human-link-tests.cs:1-3`) and a Design region. The file name is kebab-case. It uses Shouldly and Jaribu only. I could not read `skills/tw-csharp/SKILL.md`: `skills/` has no `tw-csharp` directory in this worktree. The XML summary uses plain prose and no tags beyond `<summary>` and `<example>`.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/wwwroot/auth.md:290-314
- Description: The new `### Optional human link` subsection was inserted between the "Onboarding tools" subsection and the paragraph "Credential management (list/add/revoke) uses `/api/identity/credentials*` …". That paragraph used to close "Onboarding tools". It now sits under "Optional human link", where it reads as part of the link section even though it is about credential management.
- Suggestion: Move the new subsection after the credential-management paragraph, so it sits just before the `---` that precedes "Payment". Alternatively, give the credential paragraph its own short heading.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/wwwroot/auth.md:305
- Description: "A caller that is not an agent gets **403**" is only partly right. `RequestAgentHumanLink` is restricted to `AuthenticationSchemeNames.AgentToken` (`request-agent-human-link-contracts.cs:21`). A human session cookie therefore fails authentication and gets 401 (`agent-token-authentication-handler-server.cs:99-101`). The handler's 403 (`request-agent-human-link-handler-application.cs:106-108`) is a defense-in-depth branch for a bearer whose principal is not of kind Agent.
- Suggestion: Reword to something like "A bearer whose principal is not an agent gets **403**; a request with no valid agent bearer gets 401". The Design region of the contracts file could use the same wording.
- Status: open
