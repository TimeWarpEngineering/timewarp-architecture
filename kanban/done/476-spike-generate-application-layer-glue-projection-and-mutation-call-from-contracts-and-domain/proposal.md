# Proposal: generate the application-layer glue, or keep the analyzer alone

Host task: 476. This is input for Steve's decision. It records evidence and options, and it does
not pick one. Every path and line below was read in this worktree on 2026-11-05, at the 475 merge
(`33efe049b`).

## 1. What the glue is

A handler between a contract and an aggregate does two mechanical things:

- **Projection:** entity to response, for example `new Response(role.Id.Value, role.Name, ...)`.
- **Mutation call:** command to domain method, for example `role.SetPermissions(command.PermissionIds)`.

Everything else in a handler is either plumbing (load by id, 404, save) or an application rule.

## 2. Inventory: Roles (7 handlers, `features/admin/roles/*/…-handler-application.cs`)

| Handler | Mechanical projection | Mechanical mutation call | Plumbing | Application rule |
|---|---|---|---|---|
| CreateRole | `Response(role.Id.Value)`: 1 member, key rule `RoleId ← Id` | `Role.Create(command.Name, command.Description)`: the details members become factory arguments | `AddAsync` | none |
| UpdateRole | none (empty response) | `role.UpdateDetails(command.Name, command.Description)` | find, 404, `UpdateAsync` | none (product roles may be renamed) |
| DeleteRole | none | none (`[NotAggregate]`) | find, 404, `RemoveAsync` | product role cannot be deleted → 409 (domain rule `IsProductRole` turned into a problem value) |
| GetRole | 5 members: `RoleId ← Id.Value`, `Name`, `Description`, `Permissions ← Permissions[].PermissionId`, `AgentScopes ← AgentScopes[].Scope` | none | find, 404 | agent scopes are put in `AgentScopes.All` order (`AgentScopeOrder.OrderKnown`) |
| GetRoles | the same 5 members on `RoleDto`, a second copy of GetRole's projection | none | `ListAsync` | search on name (trimmed, ignore case), order by name, `TotalCount` = matches |
| SetRolePermissions | `Response.PermissionIds ← role.Permissions[].PermissionId`. **The names differ** (`PermissionIds` against `Permissions`), so this does not match by name | `role.SetPermissions(requested)` | find, 404, `UpdateAsync` | **protected core**: Administrator keeps the admin permissions (`AdminLockoutGuards.ProtectedCoreConflict`, 409) before the write |
| SetRoleAgentScopes | `Response.Scopes ← role.AgentScopes[].Scope`. **The names differ** (`Scopes` against `AgentScopes`) | `role.SetAgentScopes(OrderKnown(command.Scopes))`. **The names differ** here too, and the input is reordered first | find, 404, `UpdateAsync` | agent-scope order on input and output |

"Last admin" is not in Roles. It is in `features/admin/principals/set-principal-roles` and was out
of this inventory's scope.

Roles totals: 11 projected member assignments, 10 of them in two copies of one projection; 3
mutation calls that are purely mechanical (Create, UpdateDetails, SetPermissions).

## 3. Inventory: Feedback (13 handlers, `features/feedback/*/…-handler-application.cs`)

The shared projection code is already hand-centralised in `feedback-contract-map-application.cs`
(`FeedbackContractMap`).

| Handler | Mechanical projection | Mechanical mutation call | Application rule |
|---|---|---|---|
| SubmitFeedback | none that matches: the response echoes `request.Kind`, plus `[Computed]`-style values (permalink, email sent, attachment ids) | none. `FeedbackItem.File(owner, kind, title, body, now)` takes the principal and the clock, not only command members | signed in; kind is a defined enum value; at most `MaxPerItem` attachments, no duplicates, each owned and unlinked; link all or roll back; email copy |
| GetFeedback | 6 members (`FeedbackItemId ← Id`, Kind, Title, Body, FiledAt, Status) | none | signed in; **owner only** (not found otherwise); `Activity` is the **filer thread**: internal notes and severity changes removed, author ids redacted to null |
| ListMyFeedback | `Item`: 5 members (Id, Kind, Title, FiledAt, Status) | none | signed in; owner's items; title search |
| ListFeedback | `Item`: 7 members (Id, OwnerPrincipalId, Kind, Status, Title, FiledAt, Severity) | none | status, kind, and severity translation; "open only" unless `IncludeClosed`; page and size clamps; sort; search; owner names fetched in one batch |
| GetFeedbackForAdmin | `FeedbackAdminView`: 8 members, and `Activity` as the full admin thread (9 members per entry, with 4 nullable enum maps) | none | none beyond 404 |
| ChangeFeedbackStatus | `FeedbackAdminView` (shared) | none. `ChangeStatus(status, principal, now)` | signed in; status maps to the domain and is not `New`; **transition allowed** (`CanChangeStatusTo`); optimistic concurrency (`UpdateAsync` false → 409 "changed"); filer is emailed when `NotifiesFiler` |
| SetFeedbackSeverity | `FeedbackAdminView` | none. `SetSeverity(severity, principal, now)` | severity is not `Unset`; `CanSetSeverityTo`; concurrency |
| ReplyToFeedback | `FeedbackAdminView` | none. `AddAdminReply(principal, body, now)` | message validity; concurrency; reply email |
| AddFeedbackNote | `FeedbackAdminView` | none. `AddInternalNote(principal, body, now)` | message validity; concurrency |
| ReplyAsFiler | 2 members (Id, Status) plus the filer thread | none. `AddFilerReply(principal, body, now)` | owner only; message validity; concurrency |
| Upload / Remove / Download attachment | Upload's response maps an attachment (5 members). That is a second copy of `FeedbackContractMap.AttachmentsAsync` | none | `FeedbackAttachment` is its own entity (`[NotAggregate]`): file name and media rules, size, pending lifetime, owner checks, cross-owner read permission, blob store compensation |

Feedback totals: about 40 projected member assignments, including the enum maps and the activity
entry. There are **0** mechanical mutation calls. Every `FeedbackItem` mutation takes the principal
and the clock, and most of them sit behind a `Can…` check and a concurrency check.

Enum mapping today is `FeedbackContractMap.ByName`, which uses `Enum.TryParse` at runtime and
throws on a missing name. Since 475, TWA0041 proves that the wire and domain enums have the same
member names, so that throw cannot fire. A generated `switch` would make the same guarantee in
the compiler instead of at runtime.

## 4. What the analyzer already covers, and what it does not

TWA0039–TWA0043 prove that a projection *can* be written: each query-response member has a root
member with a compatible type. They do not check the handler's actual expression. These gaps are
real in today's code:

1. **Positional arguments can be swapped.** `new GetRole.Response(role.Id.Value, role.Description, role.Name, …)`
   compiles, because both are `string`, and TWA0041 is still clean.
2. **Optional constructor parameters can be dropped silently.** `GetRole.Response(…, permissions = null, agentScopes = null)`
   and `FeedbackAdminView(…, severity = FeedbackSeverity.Unset)` both accept a call that leaves the
   argument out.
3. **Duplicate projections.** GetRole and GetRoles have two copies of the same 5-member projection.
   Upload and `AttachmentsAsync` have two copies of the attachment projection.
4. **Command responses are outside TWA0041.** `SetRolePermissions.Response.PermissionIds` and
   `SetRoleAgentScopes.Response.Scopes` do not match their root members by name.

One case shows why name agreement is not enough. **`GetFeedback.Response.Activity` matches
`FeedbackItem.Activity` by name, and TWA0041 accepts it.** The correct value is the filer thread,
filtered and redacted. A projection made by name would send internal notes and admin identities to
the filer. Today the tests catch that (`feedback-admin-tests.cs:263` and `:324` assert redaction on the handler's filer view, and
`feedback-triage-tests.cs:177` asserts the root marks internal notes hidden),
but only because those tests exist. Any generator that projects by name by default has this
failure mode. The member has to opt out explicitly, and leaving the opt-out off compiles.

## 5. Facts that bear on the build options

- **`Riok.Mapperly` 4.3.1 is already a package reference of `web-application`**
  (`web-application.csproj:4`, pin `Directory.Packages.props:90`). Nothing uses it: `git grep` finds
  no `[Mapper]`. Mapperly is the compile-time AutoMapper. It maps by name to constructors, maps enums
  by name, reports unmapped target members as diagnostics (which fail the build here, because warnings
  are errors), and supports extra method parameters for values the source does not hold. The 4.3.1
  generator carries the message "The additional mapping method parameter {0} of the method {1} is not
  mapped", which confirms that last point.
- The repo's generators live in `timewarp-architecture-analyzers`. **`web-application` does not
  reference that assembly** today; it gets only the analyzer-only convention analyzers
  (`source/Directory.Build.props`, `EnableConventionAnalyzers`). A home-grown generator in
  web-application needs that wiring, or emission inside the convention-analyzer assembly.
- The Feedback application classes are `public sealed class AddFeedbackNote` and so on, not
  `partial`. Generating into them needs `partial`. The Roles ones are already `partial`.
- No option reads validator bodies. Every option below uses symbols only: names, types, `[Computed]`,
  `[NotAggregate]`, and `[Operation(Group)]`.

## 6. The options as code

### Option A: keep the analyzer alone

Nothing is generated. The glue stays hand-written, and the compiler checks the mutation calls: a
renamed domain method breaks the handler with CS1061. The gaps in section 4 stay open unless they
are closed by hand. Closing them by hand looks like this:

```csharp
// role-projection-application.cs (hand-written, one copy shared by GetRole and GetRoles)
internal static class RoleProjection
{
  public static GetRole.Response ToResponse(Role role) =>
    new(
      roleId: role.Id.Value,
      name: role.Name,
      description: role.Description,
      permissions: PermissionIds(role),
      agentScopes: AgentScopeIds(role));
  // Named arguments make a swap of Name and Description a visible diff instead of a silent bug.
  // They do not stop an optional argument from being left out.
}
```

### Option B: generate projections only, with a home-grown generator that reuses the TWA0041 rules

The developer declares each projection. The generator writes the body with the same compatibility
rules TWA0041 uses: `<Root>Id → Id`, a value wrapper through `.Value`, a one-property child through
its value, mirrored enums by name, and owned children walked member by member. A response member the
generator cannot fill is a build error. Mutation calls stay hand-written.

Hand-written declaration:

```csharp
namespace TimeWarp.Architecture.Features.Admin.Roles.Application;

internal static partial class RoleProjections
{
  // The generator implements every partial method whose first parameter is an aggregate root and
  // whose return type is a contract response of that root's operation group.
  public static partial GetRole.Response ToGetRoleResponse(Role role);
  public static partial GetRoles.RoleDto ToRoleDto(Role role);
}

internal static partial class FeedbackProjections
{
  // Each [Computed] member of the response must be a parameter with the member's name. The handler
  // fetches it (async, from another store) and passes it in, so the generated code never awaits.
  // A [Computed] member with no parameter is a generator error.
  [ProjectWith(nameof(GetFeedback.Response.Activity))]   // the filer thread, not every entry
  public static partial GetFeedback.Response ToGetFeedbackResponse(
    FeedbackItem item,
    string permalink,
    IReadOnlyList<GetFeedback.Attachment> attachments);
}
```

Generated (`RoleProjections.g.cs`, `FeedbackProjections.g.cs`):

```csharp
internal static partial class RoleProjections
{
  public static partial GetRole.Response ToGetRoleResponse(Role role) =>
    new(
      roleId: role.Id.Value,
      name: role.Name,
      description: role.Description,
      permissions: [.. role.Permissions.Select(static child => child.PermissionId)],
      agentScopes: [.. role.AgentScopes.Select(static child => child.Scope)]);
}

internal static partial class FeedbackProjections
{
  // [ProjectWith] makes the generator declare this hook with an access modifier and call it.
  // If nobody writes the body, the build fails with CS8795. It is not projected by name.
  private static partial IReadOnlyList<FeedbackActivityEntry> ProjectActivity(FeedbackItem item);

  public static partial GetFeedback.Response ToGetFeedbackResponse(
    FeedbackItem item,
    string permalink,
    IReadOnlyList<GetFeedback.Attachment> attachments) =>
    new(
      feedbackItemId: item.Id.Value,
      permalink: permalink,
      kind: item.Kind switch
      {
        Domain.FeedbackKind.BugReport => FeedbackKind.BugReport,
        // one arm per member. TWA0041 proves the name sets are equal, so there is no default arm.
      },
      title: item.Title,
      body: item.Body,
      filedAt: item.FiledAt,
      attachments: attachments,
      status: /* same switch shape */,
      activity: ProjectActivity(item));
}
```

What a handler becomes, using GetRole:

```csharp
Role? role = await RoleStore.FindAsync(RoleId.From(query.RoleId), cancellationToken).ConfigureAwait(false);
return role is null ? RoleNotFound(query.RoleId) : RoleProjections.ToGetRoleResponse(role);
```

Open points in B:

- **Agent-scope order.** The generated GetRole body drops `AgentScopeOrder.OrderKnown`. Either the
  member gets `[ProjectWith]` and a hook, or the ordering moves into the domain (`Role.AgentScopes`
  returns `AgentScopes.All` order). That domain change is cleaner whatever the decision is.
- **The opt-out problem from section 4.** Leaving `[ProjectWith]` off `Activity` compiles and leaks.
  B reduces the bugs in section 4 but adds this one, and only a test catches it.
- **Command responses** (`SetRolePermissions`, `SetRoleAgentScopes`) do not match by name. They stay
  hand-written, or the contracts are renamed to match the root (`Permissions`, `AgentScopes`). That
  rename changes the wire keys.
- **Cost.** It is a new generator plus tests in an assembly web-application does not reference yet,
  and it ships to every generated app. Its rules must stay identical to TWA0041's. The way to keep
  them identical is one shared rule class (`ContractAgreementTypes`) used by both.

### Option C: generate projections with Mapperly, which is already referenced

The shape is the same as B, but the engine is off the shelf:

```csharp
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target, EnumMappingStrategy = EnumMappingStrategy.ByName)]
internal static partial class RoleMapper
{
  [MapProperty(nameof(Role.Id), nameof(GetRole.Response.RoleId))]
  [MapProperty(nameof(Role.AgentScopes), nameof(GetRole.Response.AgentScopes), Use = nameof(OrderedScopes))]
  public static partial GetRole.Response ToGetRoleResponse(Role role);

  private static Guid ToGuid(RoleId id) => id.Value;
  private static string ToPermissionId(PermissionGrant grant) => grant.PermissionId;
  private static List<string> OrderedScopes(IReadOnlyList<AgentScopeGrant> scopes) =>
    AgentScopeOrder.OrderKnown([.. scopes.Select(static scope => scope.Scope)]);
}
```

Trade-offs against B:

- No generator to build or ship. The package is already restored, and generated apps already get it.
- **Two rule sets.** Mapperly's name matching is not TWA0041's. The key rule `<Root>Id → Id` and
  the child-value unwrapping become per-mapping configuration (`[MapProperty]`, `Use =` methods),
  which is the agreement-by-memory the analyzer was built to remove.
- Computed values work through extra method parameters, as in B.
- The `Activity` opt-out risk is the same: by default, Mapperly maps it by name.
- Hooks are Mapperly's `Use =` methods, not CS8795 partials. A missing `Use =` is not an error,
  because the default mapping by name succeeds.

### Option D (shown to rule it in or out): generate the whole handler, with required hooks

This shape answers the requirement "what a handler becomes, where required hooks go". It is shown on
the one handler where it fits best.

Hand-written:

```csharp
public sealed partial class SetRolePermissions
{
  public sealed partial class Handler
  {
    // The generator declares these with an access modifier, so a missing body is CS8795.
    private partial SharedProblemDetails? BeforeSetPermissions(Role role, Command command) =>
      AdminLockoutGuards.ProtectedCoreConflict(command.RoleId, command.PermissionIds);

    private partial Response Respond(Role role) =>   // PermissionIds ≠ Permissions by name
      new([.. role.Permissions.Select(static permission => permission.PermissionId)]);
  }
}
```

Generated:

```csharp
public sealed partial class SetRolePermissions
{
  public sealed partial class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IRoleStore RoleStore;
    public Handler(IRoleStore roleStore) => RoleStore = roleStore;

    private partial SharedProblemDetails? BeforeSetPermissions(Role role, Command command);
    private partial Response Respond(Role role);

    public async ValueTask<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      Role? role = await RoleStore.FindAsync(RoleId.From(command.RoleId), cancellationToken).ConfigureAwait(false);
      if (role is null) return GetRole.Handler.RoleNotFound(command.RoleId);
      if (BeforeSetPermissions(role, command) is { } problem) return problem;
      role.SetPermissions(command.PermissionIds);
      await RoleStore.UpdateAsync(role, cancellationToken).ConfigureAwait(false);
      return Respond(role);
    }
  }
}
```

Why it stops at Roles: ChangeFeedbackStatus needs, in order, the principal, the enum translation
and the `New` exclusion, `CanChangeStatusTo`, `ChangeStatus(status, principal, now)`, a store whose
`UpdateAsync` returns false on a concurrency conflict, mail after the save, and an admin view that
needs three more services. Each of those is a hook. What is left to generate is the four-line
find/404/call/save skeleton, and that skeleton differs between slices (`UpdateAsync` returns
`Task` in Roles and `Task<bool>` in Feedback). The mutation call it generates is one line that the
compiler already checks. Across both slices, only 3 calls are mechanical, all in Roles.

## 7. What stays hand-written in every option

Every rule in the inventory: protected core, product-role delete refusal, agent-scope order (unless
it moves into the domain), sign-in and owner checks, status and severity transitions, concurrency,
mail, the filer-thread redaction, attachment limits and compensation, list filters, paging, search
and sort, and the 404 problem shapes. Also hand-written: the store implementations, the three
attachment handlers, and the command responses that do not match by name.

## 8. The question for Steve

Should the projections (entity to response) be generated? If yes, which engine: a home-grown
generator on the TWA0041 rules (B), or the Mapperly package that is already referenced (C)?

The evidence for both sides is in sections 2–6. Mutation-call generation (D) is in the proposal
because the task asked for the handler shape. Section 6-D has the counts for it: 3 mechanical calls,
all in Roles.

Follow-ups that come out of this spike no matter which option is chosen, to file after the decision:

- The unused `Riok.Mapperly` reference is either adopted (C) or removed.
- Agent-scope ordering might move into `Role.AgentScopes`. Then GetRole, GetRoles, and
  SetRoleAgentScopes stop ordering by hand.
