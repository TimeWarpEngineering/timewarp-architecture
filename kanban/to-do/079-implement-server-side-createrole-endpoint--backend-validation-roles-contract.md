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
- [ ] Server `CreateRole` Endpoint + Handler + Mapper (FastEndpoint pattern; mirror an existing
      web-server command slice, e.g. weather-forecast/todo-item write paths).
- [ ] Backend validation wired: enforce the shared `IRoleDetails` rules server-side (do not
      re-declare — reuse `RoleDetailsValidator`).
- [ ] Persistence/store for roles (or a deliberate in-memory stub if roles aren't a real feature yet
      — decide and document which).
- [ ] Verify end-to-end from `RoleForm` Save: `POST api/Roles` → 200 + `Response(RoleId)`, no toast.
- [ ] Then flip 078's manual-verification checkbox for the real (non-mock) path.

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
