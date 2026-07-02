# Implement server-side CreateRole endpoint + backend validation (roles contract)

## Description

The `admin/roles` contract is the **clean, exemplary** contract (`IRoleDetails` +
`RoleDetailsValidator`, `CreateRole`/`UpdateRole`/`GetRole` commands) but has **contracts only —
no server-side implementation**. Task 078 built the client half (`RoleForm` → dispatches a valid
`CreateRole.Command`); on Save the request reaches the **real web-server**, which has no
`POST api/Roles` handler, so it returns **405 Method Not Allowed** and the client surfaces the
generic error toast. This is expected and correct until the server half exists.

Build the **server-side `CreateRole` slice** so the whole contract use case works end-to-end,
including **backend validation** (the same `RoleDetailsValidator` / `AbstractValidator<IRoleDetails>`
shape enforced server-side, not just in the browser — never trust the client).

## Evidence / current state
- Client dispatches `POST api/Roles` (`create-role.cs`: `[RouteMixin("api/Roles", HttpVerb.Post)]`).
- Aspire console log (web-server) confirms: `POST .../api/Roles - 405` — route answers GET only, no POST.
- No server-side role files exist under the web-server / web-application (contracts-only feature).
- `CreateRole.GetMockResponseFactory()` + mock registration already exist (078) — the client works
  under `MOCK_WEB_API`; this task makes it work against the **real** server.

## Checklist
- [x] Server `CreateRole` Endpoint + Handler — **note:** web-server uses the hand-written
      MVC-style `BaseEndpoint<Command, Response>` pattern (mirrored `TrackEventEndpoint`), not
      FastEndpoints (that's api-server's path). Files:
      `web-server/features/admin/roles/{feature-annotations,create-role-endpoint}.cs`,
      `web-application/features/admin/roles/create-role-handler.cs`. No mapper needed — the
      Command *is* the pipeline message.
- [x] Backend validation: **zero new code** — `FluentValidationBehavior` (already in the mediator
      pipeline) + `AddValidatorsFromAssemblyContaining<Web.Contracts.IAssemblyMarker>` run the
      contract's composed Validator (shared `RoleDetailsValidator` + `AuthApiRequestValidator`)
      server-side. Integration tests prove both rejection paths (empty Name, empty UserId → 400
      problem details naming the property).
- [x] Persistence: **deliberate in-memory stub, documented** in the handler's Design region
      (static `ConcurrentDictionary`) — roles are a template demo feature; swap for a repository
      when real, contract/endpoint unchanged.
- [x] End-to-end verification via **web-server integration tests** (real host, real pipeline):
      `POST api/Roles` → 200 + non-empty `RoleId`; 18 passed (was 11). Manual RoleForm-in-browser
      check pending healthy local Docker (Aspire) — the integration tests cover the same seam.
- [x] 078's Save-round-trip checkbox flipped (kanban/done/078, with resolution note).

## Results — beyond the checklist

- **Unblocked by prerequisite fix:** `BaseEndpoint`/`BaseFastEndpoint` constrained
  `TResponse : BaseResponse`, which would have forced `CreateRole.Response` back into the
  `BaseResponse` shape that RFC Decision 5 rejected. Loosened to `where TResponse : class`
  (`Send` uses no `BaseResponse` member; both bases kept aligned).
- **All roles route warts fixed** (was: blocks 078 Edit/View): `get-role.cs`
  `{RoleId:min(1)}`→`{RoleId:guid}`; `update-role.cs` `api/Role/{RoleId:int}`→
  `api/Roles/{RoleId:guid}` **+ removed stray `Guid Guid` property**; `delete-role.cs` RPC-style
  `api/DeleteRole`→`api/Roles/{RoleId:guid}` **+ removed hand-declared `required int RoleId`**
  (generator emits the Guid from the route). Serialization test updated (RoleId now `Guid`) —
  the 083 tests caught the contract change, as intended by the tests-first sequencing.
- Verified: `dev build` 0/0; web-server integration 18 passed; contracts round-trips 7/7;
  analyzer 21/21; sourcegen 14/14.

## Notes
- **Contract warts to resolve first (or alongside):** `update-role.cs` /`get-role.cs` route uses
  `api/Role/{RoleId:int}` — `RoleId` typed **`int`** in the route but **`Guid`** everywhere else, and
  **singular** `api/Role` vs plural `api/Roles`. This blocks 078's Edit/View modes and should be fixed
  as part of the contracts cleanup. See [[contract-conventions-rfc]] /
  [[077-contracts-compliance-01-nullability-validator-agreement]].
- Ties into the open FastEndpoints source-gen rename work (task 053-002: `[RouteMixin]` →`[Route]`);
  keep the new endpoint consistent with whatever attribute naming lands.
- Follow-up to [[078-add-roleform-demonstrating-idetails-binding-and-shared-validation]] — that task
  proved the client binding + validation; this one completes the round-trip.
