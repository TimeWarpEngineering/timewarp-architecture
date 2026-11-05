# Front-end (UX) pieces from contracts: worked example (Roles), the counterpart to task 461

Sources: TimeWarpEngineering/timewarp-architecture origin/HEAD `f003919db` (read-only). The ActionSet
method generator is TimeWarp.State's own. Paths are relative to the repo root, and `SPA` =
`source/container-apps/web/projects/web-spa`.
Legend: **[REAL]** is quoted verbatim or summarized with a path. **[PROPOSED]** does not exist.
**[HOOK]** marks a hand-written piece the generator must surface: a `partial` hook, or a stub that
fails until filled in (same rule as task 461).

## 0. Boundary (what the generator must NOT know about)

- **Visuals and layout stay hand-written.** That covers Razor markup, `FormSection`/`FormGrid`, tables,
  chips, CSS, icons, and copy. The generator emits no `.razor` markup.
- **Purely local state and actions stay hand-written and invisible to the generator.** These are
  anything that never reaches the server. Example [REAL]: `SetPermissionSelectedActionSet` (draft
  checkbox toggles in `DraftPermissionIds`). The generator only emits pieces keyed to a contract
  request type. Local actions live in their own hand-written partial files and nothing generated
  references them.

## 1. What exists today [REAL]

### 1a. Generated or derived today

| Piece | Where | Driven by |
|---|---|---|
| `RoleState.CreateRole(Command)` / `RoleState.FetchRoles()` dispatch methods | TimeWarp.State ActionSet method generator | the hand-written `XxxActionSet.Action` ctor |
| typed state accessors on `BaseComponent`/`BaseHandler` (`RoleState` property) | `state-access-source-generator.cs` | `[StateAccess]` on `RoleState` |
| `[Route]`, `GetPageUrl(...)`, policy, `PageRegistry` | `page-source-generator.cs` | `[Page("/Admin/Roles", Policy = …)]` |
| `ActionRoster` (Ctrl-K palette) | `action-roster-source-generator.cs` | every `*ActionSet.Action` |
| mock registry for `MockWebApiService` | `mock-response-factory-registry-generator.cs` | `GetMockResponseFactory()` on the contract |
| "@role" reference type | `resource-reference-registry-generator.cs` | `[ReferenceableResource]` on `GetRoles` |
| TWA0022: components must call generated ActionSet methods, not `Mediator.Send` | `spa-mediator-send-analyzer.cs` | rule |
| TWA0026: components must dispatch actions, not call API/nav/JS directly | `component-side-effect-analyzer.cs` | rule |

### 1b. Hand-written today: the action set over `DefaultApiHandler`

`SPA/features/admin/roles/role-state/role-state.set-role-permissions.cs` (server-bound part, verbatim):

```csharp
  public static class SetRolePermissionsActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Guid RoleId { get; }

      public Action(Guid roleId)
      {
        RoleId = roleId;
      }
    }

    internal class Handler : DefaultApiHandler<Action, Command, Response>
    {
      public Handler(
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher)
        : base(store, webServerApiService, logger, publisher)
      {
      }

      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        RoleState.LastSetRolePermissionsSucceeded = false;
        IReadOnlyCollection<string> draft = RoleState.GetDraftPermissionIds(action.RoleId);
        return Task.FromResult<Command?>(new Command
        {
          UserId = Guid.NewGuid(),
          RoleId = action.RoleId,
          PermissionIds = [.. draft]
        });
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        _ = response;
        _ = cancellationToken;
        RoleState.LastSetRolePermissionsSucceeded = true;
        return Task.CompletedTask;
      }
    }
  }
```

`SPA/features/admin/roles/role-state/role-state.fetch-roles.cs` (verbatim):

```csharp
  public static class FetchRolesActionSet
  {
    [TrackAction]
    [CommandPaletteOptOut("Page/startup data load; the owning page or shell runs it, a person never needs to.")]
    public sealed class Action : IBaseAction;

    internal class Handler
    (
      IStore store,
      IWebServerApiService webServerApiService,
      ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher
    ) : DefaultApiHandler<Action, Query, Response>(store, webServerApiService, logger, publisher)
    {
      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken) =>
        Task.FromResult<Query?>(new Query { UserId = Guid.NewGuid() });

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        RoleState.RolesList = [.. response.Items];
        RoleState.DraftPermissionIds = response.Items.ToDictionary(
          static dto => dto.RoleId,
          static dto => new HashSet<string>(dto.PermissionIds ?? [], StringComparer.Ordinal));
        return Task.CompletedTask;
      }
    }
  }
```

Base class (`SPA/features/base/api-handler.cs`, [REAL]): `GetRequest` and `HandleSuccess` are abstract.
`ConfirmAsync` is a virtual that runs before the semaphore (line 181). `DefaultApiHandler` publishes
`ProblemDetailsNotification` on error.

### 1c. State, form and screens [REAL]

- `SPA/features/admin/roles/role-state/role-state.cs`:
  - `[StateAccess] public sealed partial class RoleState : State<RoleState>`
  - `private List<RoleDto>? RolesList`, where null means "no snapshot yet".
  - `LastCreatedRoleId` and `LastSetRolePermissionsSucceeded` (hand-written success flags).
  - Its Design note says: *"In-flight fetch is [TrackAction] on FetchRoles — pages use IsAnyActive, not null."*
- `SPA/features/admin/roles/components/RoleForm.razor`:
  - `EditForm Model=@RoleDetails` binds the contract's `IRoleDetails`.
  - `<FluentValidator Validator="@RoleValidator" />` uses the contract's `RoleDetailsValidator`.
  - So client validation already reuses the contract validator. There's no view model.
- `RolesListPage.razor`: `OnInitializedAsync() => await RoleState.FetchRoles();` and
  `<Section Title="Roles" LoadingActionType="typeof(RoleState.FetchRolesActionSet.Action)">`. The loading
  state is derived from `[TrackAction]`.
- Mocks: `SPA/services/mocks/mock-web-api-service.cs`. *"Mock data lives with each contract
  (GetMockResponseFactory); the request-type -> factory registry is SOURCE-GENERATED … Requests still
  run through their FluentValidation validators."*
- Confirm before destructive calls: a hand-written `ConfirmAsync` override, e.g.
  `SPA/features/admin/ai/xai-api-key-state/xai-api-key-state.clear-xai-api-key.cs`.

### 1d. Gaps today (contracts with no front end)

`UpdateRole`, `DeleteRole` and `GetRole` contracts exist, but no SPA action set references them
(`git grep "DeleteRole\|UpdateRole"` under `SPA` finds nothing). A generator would close this kind of
drift by construction.

## 2. What could be generated [PROPOSED]

The input is the same contract set as 461 (roles.list/get/create/update/delete/set-permissions) plus one
opt-in on the state:

```csharp
// SPA/features/admin/roles/role-state/role-state.cs   [PROPOSED addition to REAL file]
[StateAccess]
[ContractState(typeof(GetRoles), typeof(GetRole), typeof(CreateRole), typeof(UpdateRole),
               typeof(DeleteRole), typeof(SetRolePermissions))]
public sealed partial class RoleState : State<RoleState>
```

### 2a. Typed API surface screens call: generated action sets

One generated file per operation, `role-state.<operation>.g.cs`. The handler bodies are mechanical,
because the contract already says which request, which response, and that `UserId` is stamped
(`IAuthApiRequest`).

```csharp
// role-state.set-role-permissions.g.cs   [PROPOSED, generated]
namespace TimeWarp.Architecture.Features.Admin.Roles;

using static SetRolePermissions;

partial class RoleState
{
  public static partial class SetRolePermissionsActionSet
  {
    [TrackAction]
    [CatalogAction(Description = "Replace a role's permission ids.",          // [Operation].Description
                   Permissions = [PermissionIds.AdminRolesManage])]           // [EndpointAuthorize].Policy
    public sealed class Action : IBaseAction
    {
      public Command Command { get; }
      public Action(Command command) { Command = command; }
    }

    internal sealed partial class Handler
    (
      IStore store,
      IWebServerApiService webServerApiService,
      ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher
    ) : DefaultApiHandler<Action, Command, Response>(store, webServerApiService, logger, publisher)
    {
      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        action.Command.UserId = Guid.NewGuid();   // IAuthApiRequest stamp, as every REAL handler does
        Command command = action.Command;
        OnBuildingRequest(action, ref command);   // [HOOK] optional
        return Task.FromResult<Command?>(command);
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        RoleState.SetRolePermissionsResult = ContractResult.Succeeded(response);   // replaces LastSetRolePermissionsSucceeded
        OnSucceeded(response);                                                      // [HOOK] optional
        return Task.CompletedTask;
      }

      partial void OnBuildingRequest(Action action, ref Command command);
      partial void OnSucceeded(Response response);
    }
  }
}
```

TimeWarp.State's ActionSet method generator then emits `RoleState.SetRolePermissions(Command)` as it
does today. TWA0022 already forces screens to call that method. To keep today's draft-based call
shape, a hand-written overload `RoleState.SetRolePermissions(Guid roleId)` can build the Command from
`DraftPermissionIds`. That overload is a [HOOK]: local draft state stays outside the generator.

### 2b. DTOs consumed, and fetchers for server-bound state

For each `IQuery` in the set the generator emits a fetcher and its snapshot slot. Lists use
`ListResponse<T>`; single items are keyed by the route id.

```csharp
// role-state.fetch-roles.g.cs   [PROPOSED, generated]
partial class RoleState
{
  private List<GetRoles.RoleDto>? RolesList { get; set; }                 // null = not loaded (REAL convention)
  public IReadOnlyList<GetRoles.RoleDto>? Roles => RolesList?.AsReadOnly();

  public static partial class FetchRolesActionSet
  {
    [TrackAction]
    [CommandPaletteOptOut("Page/startup data load; the owning page or shell runs it, a person never needs to.")]
    public sealed class Action : IBaseAction
    {
      public string? Search { get; }                                      // from GetRoles.Query members
      public Action(string? search = null) { Search = search; }
    }

    internal sealed partial class Handler(/* same 4 deps */) : DefaultApiHandler<Action, GetRoles.Query, GetRoles.Response>(…)
    {
      protected override Task<GetRoles.Query?> GetRequest(Action action, CancellationToken ct) =>
        Task.FromResult<GetRoles.Query?>(new GetRoles.Query { UserId = Guid.NewGuid(), Search = action.Search });

      protected override Task HandleSuccess(GetRoles.Response response, CancellationToken ct)
      {
        RoleState.RolesList = [.. response.Items];
        OnRolesLoaded(response);          // [HOOK] REAL code seeds DraftPermissionIds here (local state)
        return Task.CompletedTask;
      }

      partial void OnRolesLoaded(GetRoles.Response response);
    }
  }
}
```

`GetRole` produces `FetchRoleActionSet` plus `Dictionary<Guid, GetRole.Response> RoleById` and `GetRoleById(Guid)`.
Mutations can declare which fetchers they invalidate, `[ContractState(..., Refetch = …)]`. The default is none,
because the REAL code deliberately sequences refetch on the page (RoleState's Design note: "failed 409 must not re-seed").

### 2c. Screen states: loading, error, success

- **Loading:** already derived from `[TrackAction]`. Generated actions all carry it, so
  `Section LoadingActionType=` keeps working unchanged.
- **Error:** generated handlers keep `DefaultApiHandler`'s `ProblemDetailsNotification` toast. Proposed:
  also record a `ContractResult<TResponse>` slot per mutation (`NotRun | Running | Succeeded(resp) |
  Failed(SharedProblemDetails)`). That replaces the hand-written `LastCreatedRoleId` and
  `LastSetRolePermissionsSucceeded` flags. Pages read `RoleState.CreateRoleResult`.
- **Destructive confirm:** when the contract carries `[Destructive]` (or the kind is a non-idempotent
  delete), the generated handler overrides `ConfirmAsync` and calls a hook:

```csharp
// role-state.delete-role.g.cs   [PROPOSED, generated] (contract would add [Destructive])
      protected override Task<bool> ConfirmAsync(Action action, CancellationToken cancellationToken) =>
        ConfirmDeleteRoleAsync(action, cancellationToken);

      // [HOOK] required: generated stub fails until a person writes the question text.
      private partial Task<bool> ConfirmDeleteRoleAsync(Action action, CancellationToken cancellationToken);
```

  It's a C# partial method with no implementation, so the build fails until it's filled in. The same
  attribute also adds `[CommandPaletteOptOut]`, which is hand-written today in the xAI key example.
- **Mocks:** unchanged mechanism. A contract without `GetMockResponseFactory()` gets a proposed analyzer
  warning (TWA00xx: "contract in a [ContractState] set has no mock factory"), not a fabricated default.

### 2d. Forms and validation

Today the REAL form binds the contract interface and reuses the contract validator, so no generated
form model is needed. Proposed: a generated form-binding helper per Command, with no markup:

```csharp
// role-form-binding.g.cs   [PROPOSED, generated]
public sealed partial class CreateRoleFormBinding
{
  public CreateRole.Command Model { get; } = new();
  public IValidator Validator { get; } = new RoleDetailsValidator();   // from Validator's SetValidator chain
  public static IReadOnlyList<FormFieldDescriptor> Fields { get; } =
  [
    new(nameof(IRoleDetails.Name), Required: true),          // NotEmpty
    new(nameof(IRoleDetails.Description), Required: true),
  ];                                                         // UserId/RoleId omitted: IAuthApiRequest / route
}
```

The `.razor` file (`RoleForm.razor`) stays hand-written. That's the visual boundary.

### 2e. Full CRUD scaffolding

From the set {list, get, create, update, delete, set-permissions}, the generator emits:
- all six action sets
- the snapshot slots
- `ContractResult` slots
- the confirm hook for delete

It also emits failing **[HOOK] page stubs** only as code-behind partials, so routing and policy come from
the contract:

```csharp
// RoleEditPage.razor.g.cs   [PROPOSED, generated]
[Page("/Admin/Roles/{RoleId:guid}/Edit", Policy = PermissionIds.AdminRolesManage)]   // route id + [EndpointAuthorize] of UpdateRole
[Authorize(Policy = PermissionIds.AdminRolesManage)]
partial class RoleEditPage;
// No RoleEditPage.razor exists → analyzer error (proposed TWA00xx "generated page has no markup")
// so the missing screen is explicit, not silently absent.
```

This would surface today's gap (1d): Update and Delete have contracts but no screens.

### 2f. TypeScript client (optional)

The `.ts` features (`SPA/source/features/*.ts`) and the explainer repo could get typed clients from the
same `[Operation]` set. That's out of the Blazor path and listed for completeness.

## 3. Hand-written hooks summary (same rule as 461: none implicit)

| Hook | Kind | Fails until filled? |
|---|---|---|
| `ConfirmXxxAsync` for `[Destructive]` operations | required partial method | yes (build error) |
| page markup for generated page stubs | `.razor` file | yes (proposed analyzer error) |
| missing `GetMockResponseFactory()` | analyzer warning | warns |
| `OnBuildingRequest` / `OnSucceeded` / `OnXxxLoaded` | optional `partial void` | no (optional by design) |
| draft-based overloads (e.g. `SetRolePermissions(Guid)` from `DraftPermissionIds`) | hand-written, outside generator | n/a: local-state boundary |

## 4. Effort notes

- **Low:** generated action sets and fetchers. Today's hand-written files already follow one shape
  (`GetRequest` + `HandleSuccess`), and TimeWarp.State's method generator already works off them.
- **Medium:** `ContractResult` slots (they change page conditions), the confirm hook, and the page stubs plus analyzer.
- **Migration:** existing hand-written action sets for the same contract conflict with generated ones.
  Generate only when the hand-written file is absent, or require removal. Decide in the research task.
