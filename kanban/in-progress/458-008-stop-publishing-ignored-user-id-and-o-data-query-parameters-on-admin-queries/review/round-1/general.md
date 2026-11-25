# Round 1 — general
**Date:** 2026-11-26
**Scope reviewed:** branch `task/458-008-stop-publishing-ignored-user-id-and-o-data-query` vs merge-base `2e90c246` (`master`), product commit `e25884ca4`, against the task requirements and `## Goal (implementer restatement)`.

## Summary

The change removes `IAuthApiRequest.UserId` and `AuthApiRequestValidator`, deletes `[AuthApiRequest]`, `[OpenDataQueryParameters]`, and `IOpenDataQueryParameters`, and makes every former mixin member a hand-declared contract property or nothing. Fifteen hosted contracts drop the unread caller id. `GetRoles` keeps `search` only. Handlers for `GetRoles`, `ListPrincipals`, and `GetCredentials` read the members that remain, and no hosted handler reads a request `UserId`. `GetCurrentUser.Query.UserId` stays on the client-only mock, as the Design region says. TWA0014 now matches the interface only. The new OpenAPI scan and the updated delete-body assertions match that published shape. Recorded gates cover the edited runfiles, analyzer and source-generator suites, web contracts, web-server integration (including the new scan), web SPA integration, and a clean `ganda repo audit`. The restatement matches the task. One skill sentence still names the removed attributes.

## Issues

### Issue 1 — Severity: suggestion
- File: skills/tw-web-api-contracts/SKILL.md:469
- Description: The canonical-examples paragraph still points at `get-roles/get-roles-contracts.cs` as "attribute auth + open-data" and at `get-role/get-role-contracts.cs` as "manual auth". Those were the two forms this change removed. The sections above already say `[ApiRoute]` is the only generated request attribute and that a hosted request does not name its caller. An agent that starts from the canonical-examples line is sent looking for `[AuthApiRequest]` and `[OpenDataQueryParameters]`.
- Suggestion: Rewrite that parenthetical to the living shape: `GetRoles` declares `Search` only, and `GetRole` is the route id plus `IAuthApiRequest` with no caller property.
- Status: open
