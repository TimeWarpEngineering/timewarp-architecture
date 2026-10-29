# Fix DELETE Roles roleId required requestBody

## Description

`DELETE /api/Roles/{roleId}` declares `requestBody.required: true` with `*/*`
+ `application/json` DeleteRole_Command. DELETE-with-required-body is
unusual, breaks many clients/codegen, and no other DELETE in the spec does
this.

## Goal (implementer restatement)

`DELETE /api/Roles/{roleId}` is published with a required JSON body (`DeleteRole.Command`). That body is not how the operation works. The role id is the path parameter. `UserId` is the mock-mode caller id, already sent on the query string, and the handler never reads a body. FastEndpoints turns leftover properties into query parameters only for GET and HEAD, so this DELETE is the one that advertises `requestBody.required: true`. The document should drop that body. `roleId` stays a path parameter. `userId` stays visible as a query parameter, because the validator still rejects an empty caller id.

## Checklist

- [x] Confirm whether the command body is actually required (it is not: the handler reads RoleId; UserId is on the query string)
- [x] If not, make it optional or drop it (roleId path param suffices)
- [x] If required, document why and give an example body (not applicable: the body was dropped)

## Session

- Created: ses_e7c126e60ffeQQKOK3tf1mfEiJ (2026-10-28)
- 2026-10-29 implement oracle: picked up uncommitted work from the earlier session. Fixed RCS1146 and CA1307 build errors in the new transformer. Ran a red check: with the transformer unregistered, the DELETE test fails on `requestBody`. Build, tests, template smoke, and audit are green.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session c7ee9cf8-9465-4eab-a189-2cba4c678127, max-turns 500, Done — 2026-10-29T07:12:44Z
- 2026-10-29 review oracle (claude-fable-5-1, effort 2, roster general via sonnet subagent aa4b403fdc754d4bc): round 1 written under `review/round-1/`. Gates re-run: `dev build` 0/0, `DeleteRoles_Given_` 3/3, `dev check-version` safe (source already ahead), `ganda repo audit` passes. 0 bugs; 1 suggestion (M1, untested Design-region branches) and 1 nit (M2, `SchemaFor` fallback formats) open. Fix pass requested on this id; next review is `review/round-2/`.
- Review oracle: review by reviewer-claude-fable (claude, model claude-fable-5-1), session 518eadd6-b300-4678-9d6e-6a5d43c6b413, max-turns 120 — 2026-10-29T07:23:00Z
- 2026-10-29 implement-fix-2 oracle (claude-opus-5-5): fixed M1 and M2 from round 1. Added two probe DELETE endpoints to the test file, one with nullable scalars and one with a collection property. Fixed the `SchemaFor` fallback types and documented the enum fallback in the Design region. Red check: when the code forced `required: true` on the leftover query parameters and the kept body, both new tests failed. The source was then restored.

## Results

- `source/foundation/foundation-server/open-api-delete-request-body.cs` (new): an OpenAPI operation transformer for DELETE operations. It moves leftover scalar properties (not route parameters) to query parameters and removes the request body. If a leftover property is an object or collection, the body stays, with `required: false`. FastEndpoints promotes these properties only for GET and HEAD. Contracts cannot use `[QueryParam]`, because web-contracts does not reference FastEndpoints.
- `common-server-module.cs`: registers the transformer in `ConfigureOpenApi`, which runs after the FastEndpoints operation transformer.
- `delete-role-contracts.cs`: the Design region now says why no body is published.
- `tests/container-apps/web/web-server-integration-tests/open-api-delete-request-body-tests.cs` (new, tag `OpenApi`). It covers DELETE Roles, which has no `requestBody`, a required `roleId` path parameter, and a required `userId` query parameter (uuid). It also checks that GET Roles is unchanged and that DELETE xai-api-key stays without a body.
- Gates: `dev build` has 0 warnings and 0 errors. `DeleteRoles_Given_` passed 3/3. All tests in the web-server-integration-tests project passed (376/376). `dev template-smoke` (default variant) succeeded. `ganda repo audit` passes. Its one advisory is a `kanban-landed` warning that other tasks' claims have no worktree, which is outside this task.
- Review (round 1, effort 2, roster general): 0 bugs, 1 suggestion, 1 nit. Framework `review/review-framework.md`; findings `review/round-1/general.md`; ledger `review/round-1/merged.md`.
- Fix pass (round 1 → round 2):
  - M1 fixed. `open-api-delete-request-body-tests.cs` adds `DeleteProbeOptionalEndpoint` (route `id`, `int? Limit`, `string? Note`) and `DeleteProbePayloadEndpoint` (route `id`, `IReadOnlyList<Guid> Items`). The FastEndpoints filter includes them only from the test namespace. The new tests check that the optional scalars become query parameters without `required`, and that the collection variant keeps `requestBody` with `required: false`.
  - M2 fixed. In the `SchemaFor` fallback, `DateOnly` is now `date`, `TimeOnly` is now `time`, and `sbyte`, `ushort`, `uint`, and `ulong` are now integers. The Design region says the body property schema is the primary source and that the fallback publishes an enum as a plain string, because the app's JSON options decide the enum wire format.
  - Gates: `dev build` has 0 warnings and 0 errors. `DeleteRoles_Given_` passed 5/5. In the web-server-integration-tests project, 401 tests passed and 1 was skipped, out of 402.

### How to validate

- **Smoke:** `cd tests/container-apps/web/web-server-integration-tests && dotnet run -c Release -- --filter-class DeleteRoles_Given_`. You can also run the app and fetch `/openapi/v1.json`.
- **Expect:** 5/5 pass. In the document, `paths["/api/Roles/{roleId}"].delete` has no `requestBody`. Its `parameters` are `roleId` (path, required) and `userId` (query, required, `format: uuid`). The probe `/probe/delete-optional/{id}` has no body, and its `limit` and `note` query parameters are not required. The probe `/probe/delete-payload/{id}` keeps its `requestBody` with `required: false`. The probes exist only in the test host.

## Review

- Round 1: Blocked — open M1, M2

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 2026-10-28.
