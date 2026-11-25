# Stop publishing ignored userId and OData query parameters on admin queries

## Description

`GET /api/Roles` publishes required query `userId` (from `[AuthApiRequest]`), optional `top`, `skip`,
`filter`, `orderBy` (from `[OpenDataQueryParameters]`), and required `returnTotalCount`. The handler
orders roles by name and reads none of them. The same required `userId` appears on `GET` and
`DELETE /api/Roles/{roleId}`, `GET /api/admin/principals`, and `GET /api/identity/credentials`. A
generated client cannot call these without inventing a Guid, and `top` would not page.

## Requirements

- Sweep every contract with `[AuthApiRequest]` or `[OpenDataQueryParameters]`.
- Where the handler does not read a member, stop publishing it (remove the attribute or the member).
  Where paging is the intended contract, implement it with a test that `top` changes the row count.
- Decide whether `IAuthApiRequest.UserId` belongs on the wire at all now that authorization uses the
  session or bearer principal; record the decision in that interface's Design region.
- Add a check (test or analyzer) that a required query parameter is read by its handler, if that can
  be done without inference.

Owner: contracts (`get-roles-contracts.cs` and siblings); `source/foundation/foundation-contracts/base/i-auth-api-request.cs`.

## Goal (implementer restatement)

The published OpenAPI document lists query and body members that no handler reads: a required
`userId` on every `IAuthApiRequest` contract, and `top`/`skip`/`filter`/`orderBy`/`returnTotalCount`
on `GET` roles. A generated client must invent a Guid and gets no paging. Stop publishing them.

Decision: `UserId` does not belong on the wire. Every hosted handler takes the caller from the
session or bearer principal (`ICurrentPrincipalAccessor`); no hosted handler reads `UserId`. The
only reader is the client-only `GetCurrentUser` mock factory, which keeps its own property.

Acceptance:

- `IAuthApiRequest` has no `UserId`; `AuthApiRequestValidator` is gone; the interface's Design
  region records the decision. It stays as a member-less marker so TWA0014 still flags an
  anonymous endpoint on an auth request.
- `[AuthApiRequest]` and `[OpenDataQueryParameters]` (and `IOpenDataQueryParameters`) are removed
  from the contracts generator, so no attribute can publish members on a handler's behalf. Every
  query member is now declared by hand on the contract.
- `GetRoles`, `ListPrincipals`, `GetCredentials`, and every manual `IAuthApiRequest` contract drop
  `UserId`; SPA callers, mock factories, generators that stamped `UserId`, tests, and the
  `tw-web-api-contracts` skill follow.
- An OpenAPI test asserts no operation publishes a `userId` parameter or body property, and that
  `GET` roles publishes no OData paging parameters.
- A "handler reads every query member" check is not added: handlers pass the request to stores and
  services, so a read check would have to infer through calls. Removing the mixins removes the
  source of unread members instead (recorded below).
- `dev build` is clean; edited test runfiles pass; `ganda repo audit` passes.

## Checklist

- [x] Implement the requirements above
- [x] Tests or document checks named above pass

## Review

- Round 1: M1 fixed in the fix pass (skill canonical-examples line)
- Round 2: Done
## Notes

- Source: task 458 multi-model review of the published OpenAPI document
  (`https://arch.timewarp.work/openapi/v1.json`, snapshot 2026-11-24, OpenAPI 3.2.0). Merged finding
  M-09 (Grok G-008) in `kanban/.../458-multi-model-review-grok-47-fable-of-published-openapi-at-archtimewarpwork/review/findings.md`
  (lands with task 458); reviewer files are `review/round-1/grok-4.7.md` and `review/round-1/fable.md`.
- Prefer generating the document from contracts or adding a build check over hand-written doc text
  (AGENTS.md: "When two things must agree, generate one from the other or add a build check").
- OpenAPI tests live in `tests/container-apps/web/web-server-integration-tests/open-api-*-tests.cs`.
- Skill: `tw-web-api-contracts`.
- Post-deploy verification: re-fetch the live OpenAPI document after deploy and confirm the finding is gone.
- Grooming note: removing `IAuthApiRequest.UserId` from the wire affects `open-api-delete-request-body-tests.cs` (which asserts `userId` on `DELETE`/`GET /api/Roles/{roleId}`), up to 16 `[AuthApiRequest]` contracts, SPA `role-state` callers, `EndpointAuthPostureAnalyzer` (`TWA0014`), and `skills/tw-web-api-contracts/SKILL.md`.

## Results

### What changed

- **Decision (recorded in `i-auth-api-request.cs` Design):** `UserId` does not belong on the wire.
  No hosted handler read it; every handler takes the caller from the session or bearer principal.
  `IAuthApiRequest` is now a member-less marker (kept for TWA0014(b)); `AuthApiRequestValidator`
  is deleted. The only reader was the client-only `GetCurrentUser` mock factory, which keeps its
  own `UserId` property.
- **Generator:** `[AuthApiRequest]` and `[OpenDataQueryParameters]` are removed from
  `ContractsGenerator` (collector, emitter, post-init attributes); `IOpenDataQueryParameters` is
  deleted. `[ApiRoute]` is the only generated request attribute, so no attribute publishes members
  on a handler's behalf.
- **Contracts swept (15):** `GetRoles` (now publishes `search` only), `ListPrincipals`,
  `GetCredentials`, `GetRole` and `DeleteRole` (route id only; `IQueryStringRouteProvider`
  dropped), `CreateRole`, `UpdateRole`, `SetRolePermissions`, `SetRoleAgentScopes`,
  `SetPrincipalRoles`, `SetPrincipalAgentScopes`, `AddPasskey`, `AddAgentKey`,
  `RenameCredential`, `RevokeCredential`. Paging was not implemented: the role catalog is small and
  the handler returns every match, so the OData members were dropped, not wired.
- **Followers:** SPA handlers stop stamping `UserId` (`AuthenticationStateProviderExtensions.GetUserIdAsync`
  had no other caller and is deleted); `ContractStateReader`/emitter and
  `ResourceReferenceRegistryGenerator`/`ReferenceableResourceShape` no longer stamp `UserId`;
  `catalog-agent-schema` and `form-prefill.values` drop their `UserId` skips; TWA0014 detects the
  interface only (attribute form gone); `tw-web-api-contracts` skill (SKILL.md, examples,
  referenceable-resources, neutral-operations) states "a request never names its caller" and
  "declare only members the handler reads".
- **Check:** new `open-api-unread-request-members-tests.cs` scans the whole web-server OpenAPI
  document: no operation publishes a `userId` parameter or request-body property, none publishes
  `top`/`skip`/`filter`/`orderBy`/`returnTotalCount`, and `GET /api/admin/roles` publishes exactly
  `search`. `open-api-delete-request-body-tests.cs` now asserts `GET`/`DELETE /api/admin/roles/{roleId}`
  publish only `path:roleId`. Generator tests assert the removed attributes and members are not emitted.
- **Not added — a "handler reads every query member" analyzer.** Handlers pass the request to
  stores and services (and mock factories read it too), so proving a member is read would mean
  inferring through calls. Removing the two mixins removes the source of unread members; the
  OpenAPI scan above guards the two known shapes.

### Gates

- `./bin/dev build` — passed, 0 warnings, 0 errors (after fixing RCS1251 on the now-empty `RevokeCredential.Command` body).
- `dotnet run --file source/container-apps/web/features/admin/principals/list-principals/list-principals-tests.cs` — passed (13).
- `dotnet run --file source/container-apps/web/features/admin/principals/set-principal-agent-scopes/set-principal-agent-scopes-tests.cs` — passed (3).
- `dotnet run --file source/container-apps/web/features/admin/principals/set-principal-roles/set-principal-roles-tests.cs` — passed (4).
- `dotnet run --file source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs` — passed (6).
- `dotnet run --file source/container-apps/web/features/admin/roles/delete-role/delete-role-validator-tests.cs` — passed (2).
- `dotnet run --file source/container-apps/web/features/admin/roles/set-role-agent-scopes/set-role-agent-scopes-tests.cs` — passed (3).
- `dotnet run --file source/container-apps/web/features/admin/roles/set-role-permissions/set-role-permissions-tests.cs` — passed (15).
- `dotnet run --file source/container-apps/web/features/admin/roles/update-role/update-role-validator-tests.cs` — passed (5).
- `dotnet run --file source/container-apps/web/features/identity/credential-last-used-tests.cs` — passed (4).
- `dotnet run --file source/container-apps/web/features/identity/rename-credential/rename-credential-tests.cs` — passed (9).
- `dotnet run --file source/container-apps/web/platform/agent-discovery/markdown-content-negotiation-tests.cs` (covers `GetRoles`, not edited) — passed (6).
- csproj suites (not standalone runfiles; the edited files there are `tests/analyzers/**`, `tests/container-apps/web/{web-server-integration-tests,web-contracts-tests,web-spa-integration-tests}/**`), run per project:
  - `./bin/dev test --no-build --project timewarp-architecture-analyzers-tests --project timewarp-architecture-sourcegenerator-tests` — passed (313 + 174).
  - `./bin/dev test --no-build --project web-contracts-tests` — passed.
  - `./bin/dev test --no-build --project web-server-integration-tests` — passed (445; includes `OpenApiUnreadRequestMembers_` 3/3 and `OpenApiDeleteRequestBody_` 5/5).
  - `./bin/dev test --no-build --project web-spa-integration-tests` — passed (654).
- `tests/container-apps/aspire/aspire-tests/ingress-smoke-tests.cs` (dropped `?UserId=` from the roles probe) — not run locally (Aspire host); CI runs it.
- `./bin/dev check-version` — source 2.0.0-beta.32 is ahead of NuGet 2.0.0-beta.31; no bump needed.
- `dev template-smoke` — not run locally; CI's PR gate runs the default variant.
- `ganda repo audit` — failed first on two checks: `kanban-landed` (task 527's live kitchen
  differed from origin-home; its session was dead) and `vscode-window-icon` (warning).
  `ganda repo audit --fix` landed the 527 kitchen and set the local, git-ignored
  `.vscode/settings.json` window title. Rerun: `ganda repo audit` — passed, all checks.

Fix pass (review round 1, skill text only):

- `./bin/dev build` — passed, build completed successfully.
- No test file covers skill prose; `./bin/dev lint-skills` (skills/ changed) — passed, 11/11 skills and `tw-web-api-contracts/evals/eval.yaml` valid.
- `ganda repo audit` — passed, all checks.

### How to validate

- **Smoke:** `./bin/dev test --no-build --project web-server-integration-tests --filter-class OpenApiUnreadRequestMembers_`
- **Expect:** 3 passed — no `userId` parameter or body property and no OData paging parameter on
  any operation, and `GET /api/admin/roles` lists only `search`.
- **Post-deploy:** fetch `https://arch.timewarp.work/openapi/v1.json`; `/paths/~1api~1admin~1roles/get/parameters`
  holds only `search`, and no operation carries `userId`.

### Review

Round 1 (effort 3, roster general): one suggestion, M1, in `review/round-1/merged.md`: the skill canonical-examples line still called GetRoles "attribute auth + open-data" and GetRole "manual auth". Fixed in `22e842c7e`: `skills/tw-web-api-contracts/SKILL.md` Canonical examples now says GetRoles' `Search` is the only query parameter and GetRole's route id is the whole request (`IAuthApiRequest`, no caller property, `I*Details` Response).

Round 2 (effort 1, roster general) re-reviewed that delta against `get-roles-contracts.cs` and `get-role-contracts.cs`. M1 stays fixed. No new findings. Disposition: clean. Final counts: bug 0 open / 0 fixed / 0 wontfix; suggestion 0 open / 1 fixed / 0 wontfix; nit 0 open / 0 fixed / 0 wontfix. Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`. Gates were not re-run; the fix-pass record above covers the skill-text change.

## Learnings

- No misread. The task text still names `GET /api/Roles` and `GET`/`DELETE /api/Roles/{roleId}` from the 2026-11-24 OpenAPI snapshot. Those routes already live at `/api/admin/roles` and `/api/admin/roles/{roleId}`. The restatement follows the living routes and the same unread `userId` and OData members.
- The handler-read check was skipped because a sound check has to follow the request into stores and services. That matches the requirement's clause "if that can be done without inference". The OpenAPI name scan is the substitute recorded in Results, not a silent drop of the requirement.

## Session

- Created: 2227132 (2026-11-24)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session a55ae55e-9096-452b-a518-4425f1fe25cd, max-turns 500, Done — 2026-11-25T18:24:38Z
- Reviewer: grok session 01a213f4-0f56-78d2-90e2-6f5e9bcb0d5c (2026-11-26), effort 3, roster general, round 1 open (M1)
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a213f4-0f56-78d2-90e2-6f5e9bcb0d5c, max-turns 200, artifacts general.md, merged.md — 2026-11-25T18:32:18Z
- Fix pass (review round 1): implementer-claude (claude-opus-5-5), M1 fixed in `skills/tw-web-api-contracts/SKILL.md`; build, lint-skills, audit passed — 2026-11-26
- Review round 2: reviewer-grok (grok, vendor xai, model grok-4.7), session 01a213fd-b606-77e3-9a18-62226b47c023, effort 1, roster general, max-turns 80, disposition clean — 2026-11-26
