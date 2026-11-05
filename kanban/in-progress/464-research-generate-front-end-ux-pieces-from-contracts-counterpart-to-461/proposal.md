# Proposal: generate the SPA pieces from contracts, or prove agreement with analyzers

Host task: 464. This is the front-end counterpart to 461/476, and it is input for Steve's decision.
It records evidence and options, and it does not pick one. Every path below was read in this
worktree on 2026-11-05 at `387a278df` (master plus the 464 claim). `SPA` means
`source/container-apps/web/projects/web-spa`, and `contracts` means
`source/container-apps/web/features/admin/roles`.

The older kitchen draft `contracts-frontend-example.md` was written against `f003919db`. Sections
2–4 of that draft are Option C below, and section 4a shows why C cannot be built as drafted.

## 1. What the SPA glue is

A server-bound action set is an `Action` (what a page, the palette, or an agent dispatches) plus a
`Handler : DefaultApiHandler<Action, TRequest, TResponse>` (`SPA/features/base/api-handler.cs`).
The handler has two abstract methods that matter:

- **`GetRequest`:** action to contract request. Stamps `UserId` (`IAuthApiRequest`).
- **`HandleSuccess`:** contract response to state. Stores a snapshot or sets a success flag.

`ConfirmAsync` (a virtual that runs before the per-state lock) and `HandleError` are the other
override points. Everything else in an action set is either local state (drafts) or a rule.

## 2. Inventory: Roles (`SPA/features/admin/roles/role-state/`)

| Action set | Contract | Action input | `GetRequest` | `HandleSuccess` | Local state or rule |
|---|---|---|---|---|---|
| CreateRole | `CreateRole.Command` | the Command | **mechanical:** copies `Name`, `Description`, stamps `UserId` | **mechanical:** `LastCreatedRoleId = response.RoleId` | `[CatalogAction]` copies the description and policy by hand (see 4c) |
| FetchRoles | `GetRoles.Query` | none (`Search` is not used) | **mechanical:** `new Query { UserId = … }` | **mechanical:** `RolesList = [.. response.Items]` | **seeds two drafts** (`DraftPermissionIds`, `DraftAgentScopes`) from each `RoleDto` |
| SetRolePermissions | `SetRolePermissions.Command` | `Guid roleId` | **not mechanical:** builds `PermissionIds` from the draft | **mechanical:** `LastSetRolePermissionsSucceeded = true` | resets the flag to false in `GetRequest` |
| SetRoleAgentScopes | `SetRoleAgentScopes.Command` | `Guid roleId` | **not mechanical:** builds `Scopes` from the draft | **mechanical:** `LastSetRoleAgentScopesSucceeded = true` | resets the flag to false in `GetRequest` |
| SetPermissionSelected, SetAgentScopeSelected | none | role id, item, bool | n/a (`BaseHandler`) | n/a | purely local draft toggles: outside every option |
| GetRole, UpdateRole, DeleteRole | contracts exist | **no action set, no screen** | — | — | `git grep` under `SPA` finds no use |

Roles totals: 4 server-bound handlers. 2 request builds are mechanical (Create, Fetch), and 2 read
local drafts. 4 success writes are mechanical: 1 snapshot and 3 flags. 3 hand-written pieces are
required and cannot be generated: the draft seeding in FetchRoles, and the two draft-to-request
builds. The mechanical code in Roles is about 12 lines of method body.

SPA-wide counts, so the Roles numbers can be read in context (from `git grep` in `SPA/features`):

- There are 41 `DefaultApiHandler` subclasses.
- 5 of them build the request from `action.Command`. Four return it as is (`UpdateProfile`,
  `UpdateAssistantName`, `UpdateSiteSettings`, `UpdateXaiByokMode`), and CreateRole copies its
  fields. The other 36 build the request from action fields or state. Reproduce with
  `git grep -l "action.Command" -- SPA/features`: it lists 6 files, and the sixth (chat-state's
  `ServerToClientMessageAction`) is not a `DefaultApiHandler`.
- 19 override `HandleError`, so their error behavior is not the default toast.
- 2 override `ConfirmAsync` (`ClearXaiApiKey`, `ClearUserXaiApiKey`).
- There are 9 hand-written `Last…Succeeded` or `Last…Id` members.

## 3. Already generated or checked today

| Piece | Where | Driven by |
|---|---|---|
| `RoleState.CreateRole(Command)` and the other dispatch methods | TimeWarp.State's ActionSet method generator (package `TimeWarp.State` 12.0.0-beta.16) | the hand-written `XxxActionSet.Action` constructor |
| handler registration | TimeWarp.Mediator's source generator (`AddWebSpaGeneratedMediator`, `SPA/program.cs:189`) | the hand-written `Handler` class |
| `[StateAccess]` accessors, `[Page]` routes and policy, `ActionRoster`, the mock-factory registry, the `[ReferenceableResource]` registry | `source/analyzers/timewarp-architecture-analyzers/generators/` | attributes and `GetMockResponseFactory()` |
| loading state | `Section LoadingActionType=` plus `[TrackAction]` | the Action |
| client validation | `RoleForm.razor` binds `IRoleDetails` and uses the contract's `RoleDetailsValidator` | the contract |
| TWA0022, TWA0026 | convention analyzers | components dispatch actions, never `Mediator.Send`, the API, navigation, or JS |

## 4. Facts that limit the options

### 4a. Roslyn generators cannot see each other's output

Every source generator runs on the same input compilation, so code one generator emits is not
input to another one. A generated `XxxActionSet` would therefore be missing three things:

1. **No dispatch method.** TimeWarp.State's method generator would not emit `RoleState.CreateRole(...)`,
   so pages would have nothing to call (TWA0022 forbids `Mediator.Send`).
2. **No handler registration.** TimeWarp.Mediator's generator would not register the `Handler`, so
   the dispatch would find no handler at run time.
3. **No palette entry.** Our `ActionRosterSourceGenerator` would not list the Action.

The same applies to a generated `[Page]` partial. `PageSourceGenerator` would not see it, so the
page would get no `@page` route and no `GetPageUrl`.

So a generator can only fill in **bodies of types a person declared**: a hand-written partial
`Handler` with its base type, or a hand-written partial page. Emitting whole action sets or pages
needs the generation to move into TimeWarp.State, which is another repository (see the
correction below).

**Correction (Steve and cockpit, 2026-11-05).** Point 2 is wrong. TimeWarp.Mediator's generator
reads referenced assemblies that carry `[assembly: MediatorAssembly]`
(`timewarp-mediator/source/timewarp-mediator-generators/referenced-assembly-candidates.cs`), so a
`Handler` in a project web-spa references does register. The same cross-project pattern already
runs five generators in this repo: an upstream compilation stamps a marker
(`TypedIdsEmbedded`, `ApiEndpointsEmbedded`), and the downstream compilation walks marked
references by metadata. Points 1 and 3 stand: TimeWarp.State's method generator, its catalog
generator, and our roster and page generators use syntax providers on the host compilation only.

Steve recalled "dual pass source gen" (Discord, 2026-01-15). The GitHub history shows that week
was Nuru generator work. Two days earlier, Nuru commit `8027071a` records the same wall:
"Source generators run in isolation and can't see each other's output … Solution: put fixed
types in core library." Nuru's "two-phase" pattern is a syntactic filter followed by a semantic
extract inside one generator, not one generator feeding another. No two-pass build target exists
in any TimeWarp repository.

So option C is not impossible; it needs a metadata walk in TimeWarp.State's generators, following
Mediator's pattern, so generated action sets in a referenced project get their dispatch method
and catalog entry. That is a timewarp-state task. The roster and page generators here would take
the same change.

### 4b. `[Destructive]` exists and no contract uses it

`TimeWarp.Contracts` 2.0.0-beta.31 ships `DestructiveAttribute`. The analyzers read it
(`timewarp-architecture-analyzers/models/endpoint-metadata.cs:75`), and the contracts generator
copies it to `OperationDestructive`, which feeds the transports' destructive hint
(`foundation-contracts-generators/contracts-emitter.cs:49`). `git grep "\[Destructive"` finds
**0** uses. The two handlers that do ask (`ClearXaiApiKey` and `ClearUserXaiApiKey`, which use
`ConfirmAsync` plus `IConfirmationService`) sit on contracts without the attribute. So the SPA's
confirm and the contract's destructive hint already disagree in both cases.

### 4c. `[CatalogAction]` copies contract metadata by hand

In `role-state.create-role.cs:22-27`:

```csharp
[CatalogAction
(
  Description = "Create a new authorization role.",     // the contract's [Operation] says "Create a role."
  Permissions = [PermissionIds.AdminRolesManage],       // the contract's [EndpointAuthorize] Policy, copied
  Visibility = ActionVisibility.Agent
)]
```

That is agreement by memory, and the description already differs. 24 SPA files carry
`[CatalogAction]`, and nothing checks them against the contract.

### 4d. Mocks fall through silently

`SPA/services/mocks/mock-web-api-service.cs:61-64`: when no factory is found, it logs at Debug and
calls the real API. Of the 74 `[ApiEndpoint]` web contracts, about 11 have no
`GetMockResponseFactory()`. Some of them are passkey or Entra ceremonies that cannot be mocked as
JSON. All 7 Roles contracts have one.

### 4e. Some contracts have no SPA surface on purpose

About 11 of the 74 web contracts are not used in the SPA. Three are the Roles gap (GetRole,
UpdateRole, DeleteRole). Others are agent or server-only by design, for example `AddAgentKey`,
`GetAgentIdentity`, `InvokeMeteredCapability`, and `CompleteAgentChat`. Any coverage rule needs an
opt-out with a reason, the way TWA0006 has `[ClientOnlyContract(reason)]`.

### 4f. The hazard case: an optional hook that loses data

This is the SPA counterpart to 476's `Activity` leak. In a generated FetchRoles, draft seeding would
be a hook. If that hook were an optional `partial void OnRolesLoaded(...)` and nobody wrote it,
everything would still compile, and then:

- `RoleDetailPage` shows every checkbox unchecked (`IsPermissionSelected` reads an empty draft).
- **Save** posts `PermissionIds = []`, and `SetRolePermissions` is a full replace. Every role
  except Administrator loses all its permissions. Administrator is saved by the protected-core
  409.
- Toggling one checkbox creates a one-item draft, so Save keeps only that permission.

No test covers this path today: `git grep` in `tests/` finds no reference to `DraftPermissionIds`,
`SaveRolePermissions`, or the seed. So in any generator option the seed hook has to be
**required** (`private partial void OnRolesLoaded(...)`, CS8795 when missing), not optional.

### 4g. A sticky success value already needs a workaround

`RoleForm.razor:23-30` saves `LastCreatedRoleId` before dispatch and navigates only when the value
changed, because the value is never cleared on failure. The two `Last…Succeeded` flags avoid this
by resetting to false in `GetRequest`. That reset is a convention that each handler remembers by
hand.

## 5. The options as code

The options are not exclusive. A, the set of analyzers, can ship alone or under B. D is
independent of B and C.

### Option A: analyzers alone

Nothing is generated, and the action sets stay hand-written. The build proves that the
hand-written SPA agrees with the contracts. Here are the candidate checks, with what each would
flag today. The ids are placeholders.

```csharp
// A1 SPA coverage (Warning). An [ApiEndpoint] contract in an assembly the SPA references has no
// SPA ApiHandler<_, TRequest, _> and no opt-out.
// Today: GetRole, UpdateRole, DeleteRole, plus the agent-only contracts until they opt out.
// Opt-out shape (the reason text is an example, not a claim about AddAgentKey):
[assembly: NoSpaSurface(typeof(AddAgentKey), "<why this contract has no browser surface>")]

// A2 Destructive agreement (Warning, both directions).
// (a) The contract has [Destructive] and its SPA handler does not override ConfirmAsync.
// (b) A handler overrides ConfirmAsync and its contract lacks [Destructive].
// Today: (b) flags ClearXaiApiKey and ClearUserXaiApiKey. Adding [Destructive] to those two
// contracts also gives the transports the destructive hint.
// DeleteRole gets [Destructive] when its screen is built, and then (a) makes ConfirmAsync required.

// A3 Catalog agreement (Warning). [CatalogAction].Permissions on an action whose handler sends
// contract X does not contain X's [EndpointAuthorize] Policy. Description: either warn when it
// differs from [Operation].Description, or drop it from [CatalogAction] and read it from the
// contract (a TimeWarp.State change).
// Today: CreateRole's description differs.

// A4 Mock coverage (Warning). An SPA ApiHandler sends a contract that has no
// GetMockResponseFactory(). Opt-out: the existing UseRealApi set in mock-web-api-service.cs.
// This replaces the silent Debug fallback with a build message.
```

Not covered by A: the draft-seed hazard (4f). That needs a test, whichever option is chosen.

### Option B: generate handler bodies into hand-written partial shells

This is the only generator shape that works with 4a. The developer declares the `Action` and the
`Handler` with its base type, so TimeWarp.State, TimeWarp.Mediator, and the roster still see them.
The generator writes only the bodies it can derive. Anything else becomes a required hook.

Hand-written, `role-state.create-role.cs`:

```csharp
partial class RoleState
{
  public static partial class CreateRoleActionSet          // now partial
  {
    [CatalogAction(Visibility = ActionVisibility.Agent)]   // Description and Permissions: see A3
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public CreateRole.Command Command { get; }
      public Action(CreateRole.Command command) { Command = command; }
    }

    [ContractHandler]   // the generator fills GetRequest and HandleSuccess for TRequest = CreateRole.Command
    internal sealed partial class Handler
    (
      IStore store,
      IWebServerApiService webServerApiService,
      ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher
    ) : DefaultApiHandler<Action, CreateRole.Command, CreateRole.Response>(store, webServerApiService, logger, publisher);
  }
}
```

Generated, `role-state.create-role.g.cs`:

```csharp
partial class RoleState
{
  public static partial class CreateRoleActionSet
  {
    internal sealed partial class Handler
    {
      // Rule: the Action has exactly one member of type TRequest. Its settable members are copied
      // into a new request, so the form's bound model is never changed. Same type, so copying by
      // name cannot pick the wrong member. IAuthApiRequest gets UserId stamped.
      protected override Task<CreateRole.Command?> GetRequest(Action action, CancellationToken cancellationToken) =>
        Task.FromResult<CreateRole.Command?>(new CreateRole.Command
        {
          UserId = Guid.NewGuid(),
          Name = action.Command.Name,
          Description = action.Command.Description,
        });

      // Needs Option D (a result slot). Without D there is nothing to derive, and this becomes a
      // required hook: private partial void OnSucceeded(CreateRole.Response response);
      protected override Task HandleSuccess(CreateRole.Response response, CancellationToken cancellationToken)
      {
        RoleState.CreateRoleResult = ContractResult.Succeeded(response);
        return Task.CompletedTask;
      }
    }
  }
}
```

The draft-based handlers keep their request build as a required hook. Hand-written,
`role-state.set-role-permissions.cs`:

```csharp
    [ContractHandler]
    internal sealed partial class Handler(/* same 4 dependencies */)
      : DefaultApiHandler<Action, SetRolePermissions.Command, SetRolePermissions.Response>(/* … */)
    {
      // The generator declares this with an access modifier, so a missing body is CS8795.
      private partial SetRolePermissions.Command BuildRequest(Action action) =>
        new() { RoleId = action.RoleId, PermissionIds = [.. RoleState.GetDraftPermissionIds(action.RoleId)] };
    }
```

Generated:

```csharp
      private partial SetRolePermissions.Command BuildRequest(Action action);

      protected override Task<SetRolePermissions.Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        SetRolePermissions.Command command = BuildRequest(action);
        command.UserId = Guid.NewGuid();
        return Task.FromResult<SetRolePermissions.Command?>(command);
      }

      // D replaces the hand-written flag reset. ApiHandler calls OnStarted only after GetRequest
      // returns a request (see Option D), so a skipped action never leaves the slot at Running.
      protected override void OnStarted(SetRolePermissions.Command request) =>
        RoleState.SetRolePermissionsResult = ContractResult<SetRolePermissions.Response>.Running;
```

FetchRoles under B. The request is `new GetRoles.Query()` plus the stamp, and the snapshot write is
generated. The seed hook is **required** because of 4f:

```csharp
      // Generated declaration. A missing body is CS8795, so the data-loss case in 4f cannot compile.
      private partial void OnRolesLoaded(GetRoles.Response response);
```

When the generator emits that required declaration needs a rule. There are two, and B as written
assumes the first:

- **Required on every generated fetch** (every `[ContractHandler]` whose request is an `IQuery<…>`).
  The generator cannot tell from symbols whether a snapshot feeds local state, so it always asks.
  Cost: a fetch that seeds nothing still needs a one-line empty body. SPA-wide that is up to 16
  handlers (the `DefaultApiHandler`s whose request is a `Query`). In Roles only FetchRoles is a
  fetch, and it needs the body anyway.
- **Opt-in** (for example `[ContractHandler(SeedsLocalState = true)]`). No empty bodies, but
  forgetting the flag compiles. That brings back 4f's data loss, so under this rule 4f is
  prevented only by the test in the section 7 notes.

The snapshot slot needs a name. Either the generator owns `RolesList` (named after the `ListResponse<T>`
item type), or the developer names it (`[ContractHandler(Snapshot = nameof(RolesList))]`).

What B saves in Roles: the 2 mechanical request builds and the 4 success writes, about 12 lines. What
B costs:

- a generator plus tests in `timewarp-architecture-analyzers`, which ships to every generated app
- `partial` on 4 action sets
- one attribute per handler
- one empty `On…Loaded` body per fetch that seeds nothing (up to 16 SPA-wide), under the
  required-on-every-fetch rule above
- rules for when `GetRequest` can be derived. SPA-wide, that is only 5 of 41 handlers (the
  Action carries the contract Command) plus the no-argument fetches.

### Option C: generate whole action sets from `[ContractState(...)]` (the old draft)

The input is one attribute on the state:

```csharp
[StateAccess]
[ContractState(typeof(GetRoles), typeof(GetRole), typeof(CreateRole), typeof(UpdateRole),
               typeof(DeleteRole), typeof(SetRolePermissions), typeof(SetRoleAgentScopes))]
public sealed partial class RoleState : State<RoleState>
```

The generator emits each `XxxActionSet` (Action and Handler) with the hooks from B. Because of 4a,
it also has to emit everything the other generators would have produced from a hand-written set:

```csharp
// Also generated, because TimeWarp.State's and TimeWarp.Mediator's generators cannot see the set:
partial class RoleState
{
  public Task CreateRole(CreateRole.Command command, CancellationToken externalCancellationToken = default) =>
    Sender.Send(new CreateRoleActionSet.Action(command), externalCancellationToken);   // copies TimeWarp.State's method rule
}
// + a handler registration for each generated Handler (copies TimeWarp.Mediator's registration)
// + the ActionRoster entries (our own generator; it could merge with this one)
```

The alternative is to move the generator into TimeWarp.State, which is another repository's work.

The action's input shape cannot be derived. SetRolePermissions is dispatched with a role id, and
the request is built from the draft. A generated `Action(Command)` would move the draft read into
the page, but the palette and agents dispatch the action, not the page. So C still needs a
hand-written action for every draft-based operation. That is 2 of the 4 server-bound sets in
Roles.

C also generates the 3 missing sets (GetRole, UpdateRole, DeleteRole), which nothing would call
until someone builds the screens. A1 surfaces the same gap without generating unused code.

### Option D: one result slot per contract call (separate from B and C)

This replaces the hand-written flags and the sticky id (4g).

```csharp
// Platform type. It is immutable, so a State clone can share it (the same rule as RoleDto.Clone).
public sealed record ContractResult<TResponse>
{
  public static ContractResult<TResponse> NotRun { get; } = new();
  public static ContractResult<TResponse> Running { get; } = new() { IsRunning = true };
  public bool IsRunning { get; private init; }
  public TResponse? Response { get; private init; }
  public SharedProblemDetails? Problem { get; private init; }
  public bool Succeeded => Response is not null;
  // Succeeded(response) and Failed(problem) factories
}
```

The page changes. `RoleForm.razor` today:

```csharp
Guid? previousCreatedRoleId = RoleState.LastCreatedRoleId;
await RoleState.CreateRole(Command);
if (RoleState.LastCreatedRoleId is Guid roleId && roleId != previousCreatedRoleId) { … }
```

With D:

```csharp
await RoleState.CreateRole(Command);
if (RoleState.CreateRoleResult is { Succeeded: true, Response: { } created })
{
  await NoSubRouteState.ChangeRoute(newRoute: RoleDetailPage.GetPageUrl(created.RoleId));
}
```

`Failed` cannot be generated into `HandleError`. 19 handlers override `HandleError` by hand
(section 6 keeps them hand-written), and a generator cannot add a statement to a method someone
else declared. `Running` has the same problem in the other direction. If `GetRequest` sets it, a
`GetRequest` that then returns `null` (the sanctioned skip) leaves it set. So D needs a base-class
change in the template SPA's own `SPA/features/base/api-handler.cs`. Today
`ApiHandler.Handle` calls `GetRequest` and sends the request. The private
`HandleApiResponseAsync` then matches the `OneOf` straight to `HandleError`. Exceptions other than
cancellation are logged and rethrown at `api-handler.cs:87-92`.

A slot can leave `Running` only through a hook the base class calls. Each path out of `Handle` is
listed here, with the hook that closes it:

| Path out of `Handle` (`api-handler.cs`) | Slot if `GetRequest` sets `Running` and only the problem arm calls `OnProblem` | Closed by |
|---|---|---|
| `GetRequest` returns `null` (`:71`) | stuck at `Running` if `GetRequest` set it | `Running` moves to `OnStarted`, which runs only for a non-null request |
| success body (`:96` → `HandleSuccess`) | `Succeeded` | `HandleSuccess` (unchanged) |
| problem body, including the synthetic 499 for cancellation (`:77-86`) | `Failed` | `OnProblem` before `HandleError` |
| transport failure: `HttpRequestException` from `ApiService.GetResponse`, rethrown at `:87-92` | stuck at `Running` | `OnProblem` with a synthetic problem, called in the catch before the rethrow |
| `ValidationException` from `ValidateRequestAsync` (`:123`), rethrown at `:87-92` | stuck at `Running` | the same catch |
| `GetRequest` itself throws (a hand-written build) | stuck at `Running` if the write came before the throw | the same catch. `OnStarted` has not run, and the slot goes to `Failed` |

D therefore adds two empty virtuals and one call in the existing catch:

```csharp
// ApiHandler<TAction, TRequest, TResponse> (proposed).

// Runs inside the lock after GetRequest returns a non-null request, before validation and the send.
protected virtual void OnStarted(TRequest request) { }

// Runs for every failure after OnStarted could have run: a problem body (before HandleError,
// after the lock is released), and any exception the catch below rethrows.
protected virtual void OnProblem(SharedProblemDetails problem) { }

// In Handle:
      TRequest? request = await GetRequest(action, cancellationToken);
      if (request is null) return;// Skip the action
      OnStarted(request);
      await ValidateRequestAsync(request);
      …
    catch (Exception ex)
    {
      Logger.LogError(ex, "Unexpected error occurred while handling {ActionType}", typeof(TAction).Name);
      // The slot leaves Running. HandleError is still not called, and the exception still
      // propagates, so the "validation failures throw" rule in the Design region is unchanged.
      OnProblem(new SharedProblemDetails { Title = "Unexpected error", Detail = ex.Message });
      throw;
    }

private async Task HandleApiResponseAsync(OneOf<TResponse, FileResponse, SharedProblemDetails> apiResponse, CancellationToken cancellationToken)
{
  await apiResponse.Match(
    response => HandleSuccess(response, cancellationToken),
    fileResponse => HandleFileResponse(fileResponse, cancellationToken),
    problemDetails => { OnProblem(problemDetails); return HandleError(problemDetails, cancellationToken); }
  );
}
```

The catch runs after the `using` has released the per-state lock, so `OnProblem` never runs under
the lock from that path either.

Other remedies were weighed and not taken:

- **Map unexpected exceptions to a synthetic problem through `HandleError`, as 499 is mapped.**
  This would stop validation failures from throwing. The Design region of `api-handler.cs` says
  they throw because they are client-side coding bugs, and every handler's `HandleError` would
  start toasting them.
- **Set `Running` in the base without a hook.** The base does not know which state member is the
  slot. It would need reflection or a generated registry, and the hook is smaller.

Two gaps remain, and D accepts them:

- `DefaultApiHandler.HandleFileResponse` throws `NotImplementedException` outside the `try`. A JSON
  endpoint that returns a file leaves the slot at `Running`. The handler is in the wrong base
  class, which is a coding bug, and the exception surfaces.
- A hand-written `HandleSuccess` (A only) that throws before its `Succeeded` write leaves the slot
  at `Running`. The exception surfaces in the same way. Generated `HandleSuccess` bodies (B, C)
  write the slot as the first statement.

Who sets the slot:

- **With B or C:** generated code overrides `OnStarted` to set `Running` and `OnProblem` to set
  `Failed`. It sets `Succeeded` as the first statement of `HandleSuccess`. A hand-written
  `GetRequest` build hook or `HandleError` (toast text, a field reset) is not touched.
- **With A only:** the slot is hand-written in all three places. The check flags a
  `DefaultApiHandler` whose state has a `ContractResult<TResponse>` slot for its response type when
  any of these is missing: an assignment of `Running` in an `OnStarted` override, of `Succeeded` in
  `HandleSuccess`, or of `Failed` in an `OnProblem` override. It also flags an assignment of
  `Running` in `GetRequest`, because the skip path would leave it set. Without the `OnProblem` leg,
  every failure path in the table would leave the slot at `Running` for good.

Scope: on `ApiHandler`, two empty virtuals (`OnStarted`, `OnProblem`), one `OnStarted` call after
the null-skip, and one `OnProblem` call in each of the problem arm and the existing catch. No
handler changes behavior from the base change alone. Then 3 slot members in Roles and 9 SPA-wide.
Every page that reads one of them changes. The 19 hand-written `HandleError` overrides stay as
they are.

### Forms and validation: nothing to generate

`RoleForm.razor` already binds the contract (`IRoleDetails`) and uses the contract's validator
(`RoleDetailsValidator`). An Update screen reuses the same form, because `GetRole.Response`
implements `IRoleDetails` (the Design region of `get-role-contracts.cs` says so). The old draft
proposed a `FormBinding` with `Fields` and `Required` flags. That needs the generator to read
validator bodies, and 476 kept every option to symbols only. A generated binding would also be a
second source for rules the validator already enforces at run time. No option is offered here.

### CRUD scaffolding and page stubs

A generated `[Page]` code-behind is invisible to `PageSourceGenerator` (4a), so build-time page
stubs have the same limit as C. What can be done at build time is A1: the missing Update and Delete
screens show up as warnings until they are built or opted out. Visuals stay hand-written in every
option.

## 6. What stays hand-written in every option

- all Razor markup, CSS, and copy
- local state and actions: the drafts and the two `Set…Selected` toggles
- draft seeding (required hook in B and C)
- the two draft-to-request builds (required hooks in B and C)
- confirm question text (`ConfirmAsync` body; A2 makes it required when the contract is `[Destructive]`)
- the 19 custom `HandleError` overrides
- page sequencing ("fetch again only after a successful save")
- the dispatch call shape of every action whose input is not the contract request

## 7. The question for Steve

Do we generate any SPA handler code, or does the SPA side stay hand-written with agreement
analyzers (A1–A4), the same way 461 ended for aggregates?

If any generation is wanted, the only shape that works with the other generators is B: bodies
inside hand-written partial handlers. In Roles that saves about 12 lines. C (the old draft's
whole-set generator) needs the TimeWarp.State and TimeWarp.Mediator rules copied, or the work moved
into TimeWarp.State.

D is a separate choice: should one result slot replace the 9 hand-written success flags and ids?

Notes for whichever way this goes (to file after the decision, not split here):

- `ClearXaiApiKey` and `ClearUserXaiApiKey` lack `[Destructive]` even though their SPA handlers
  ask before the call (4b).
- `CreateRole`'s `[CatalogAction]` description differs from its `[Operation]` description (4c).
- No test covers RoleDetailPage Save with drafts that were seeded from the fetch (4f).
