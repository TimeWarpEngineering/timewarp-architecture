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

## Results

- `source/foundation/foundation-server/open-api-delete-request-body.cs` (new): an OpenAPI operation transformer for DELETE operations. It moves leftover scalar properties (not route parameters) to query parameters and removes the request body. If a leftover property is an object or collection, the body stays, with `required: false`. FastEndpoints promotes these properties only for GET and HEAD. Contracts cannot use `[QueryParam]`, because web-contracts does not reference FastEndpoints.
- `common-server-module.cs`: registers the transformer in `ConfigureOpenApi`, which runs after the FastEndpoints operation transformer.
- `delete-role-contracts.cs`: the Design region now says why no body is published.
- `tests/container-apps/web/web-server-integration-tests/open-api-delete-request-body-tests.cs` (new, tag `OpenApi`). It covers DELETE Roles, which has no `requestBody`, a required `roleId` path parameter, and a required `userId` query parameter (uuid). It also checks that GET Roles is unchanged and that DELETE xai-api-key stays without a body.
- Gates: `dev build` has 0 warnings and 0 errors. `DeleteRoles_Given_` passed 3/3. All tests in the web-server-integration-tests project passed (376/376). `dev template-smoke` (default variant) succeeded. `ganda repo audit` passes. Its one advisory is a `kanban-landed` warning that other tasks' claims have no worktree, which is outside this task.

### How to validate

- **Smoke:** `cd tests/container-apps/web/web-server-integration-tests && dotnet run -c Release -- --filter-class DeleteRoles_Given_`. You can also run the app and fetch `/openapi/v1.json`.
- **Expect:** 3/3 pass. In the document, `paths["/api/Roles/{roleId}"].delete` has no `requestBody`. Its `parameters` are `roleId` (path, required) and `userId` (query, required, `format: uuid`).

## Notes

Evidence: https://arch.timewarp.work/openapi/v1.json, 2026-10-28.
