# Neutral operations: one contract, three bindings

`TimeWarp.Contracts` holds the part of a contract that does not depend on the transport. A
request class in this skill's shape is the operation. The HTTP, CLI, and MCP hosts each bind
the same class. Nobody writes a second schema.

## The neutral layer

| Fact | Where it lives |
|------|----------------|
| Canonical name | `[Operation("roles.get")]` on the nested `Query`/`Command` |
| Summary | `Description = "…"` on `[Operation]` |
| Purpose group | `Group = "admin.roles"` on `[Operation]` (optional; `.` or space separates segments) |
| Title | `Title = "Rename symbol"` on `[Operation]` (optional). Null or whitespace means no title |
| Destructive | `[Destructive]` or `[Destructive(false)]`. Absent is unset, not false. MCP treats an omitted `destructiveHint` as true and reads it only when the tool is not read-only |
| Open world | `[OpenWorld]` or `[OpenWorld(false)]`. Absent is unset, not false |
| Kind | The mediator interface: `IQuery<T>`, `ICommand<T>`, or `IIdempotentCommand<T>` |
| Members | The request's properties. Name, required, default, enum values, and `[Description]` are derived (see the `TimeWarp.Contracts` package readme) |
| Result | `OneOf<Response, SharedProblemDetails>`. The catalog records `Response`. |

Kind is never an attribute argument, because the mediator interface already says it. The name is
not a route, a CLI pattern, or an MCP tool name. Each host derives those. The group is metadata
only and never prefixes a route or a command name.

Name operations `<resource>.<action>` in lower case (`roles.list`, `roles.get`, `roles.create`,
`roles.update`, `roles.delete`, `roles.set-permissions`). A whitespace name counts as no
operation.

## Choosing the kind

| The request | Interface |
|-------------|-----------|
| Reads only; repeating it changes nothing | `IQuery<OneOf<Response, SharedProblemDetails>>` |
| Writes, and a retry leaves the same state (full replace, delete) | `IIdempotentCommand<OneOf<Response, SharedProblemDetails>>` |
| Writes, and a retry repeats the effect (create, append, charge) | `ICommand<OneOf<Response, SharedProblemDetails>>` |
| Fits none of these (a GET that takes a payment or debits a ledger) | Bare `IRequest<OneOf<…>>`. Say why in the Design region. |

All three extend `IRequest<T>`. Handlers stay `IRequestHandler<TRequest, TResponse>`, and
dispatch is the same. Choosing a kind changes no runtime behavior. It is a declared promise
that hosts and catalogs read. `IIdempotentCommand` means a retry is safe. It does not add an
idempotency key or deduplication.

**The kind does not pick the HTTP verb.** `[ApiRoute]` keeps the verb, and the verb is not
derived from the kind. The usual pairing is GET with `IQuery`, POST with `ICommand`, and PUT or
DELETE with `IIdempotentCommand`. It is not a rule. A search sent as a POST body can still be
an `IQuery`. A PUT whose handler appends history is an `ICommand`. Use `ICommand` when retry
safety is uncertain: it is never wrong, and `IIdempotentCommand` is a promise to keep.

## The three bindings

| Binding | Source of truth | Who projects it |
|---------|-----------------|-----------------|
| HTTP | `[ApiRoute(template, verb)]` on the request and `[ApiEndpoint]` on the outer class | The contracts generator emits route members, plus `OperationName`, `OperationDescription`, and `OperationGroup` when `[Operation]` is present. A set title or hint adds `OperationTitle`, `OperationDestructive`, or `OperationOpenWorld`. An unset hint is omitted. The FastEndpoint generator hosts the route and does not read the hints. |
| CLI | The operation name, members, and kind | TimeWarp.Nuru derives a route pattern from the contract and dispatches through the mediator. The command is the operation name. |
| MCP | The operation name, description, and members | The TimeWarp.Nuru MCP host derives the tool name and input schema and dispatches through the mediator. |

A host that publishes an `OperationCatalog` fills one `OperationBindings` entry (`Http`, `Cli`,
`Mcp`) per transport it exposes. A missing binding means the transport does not expose that
operation. `--capabilities`, OpenAPI, and MCP `tools/list` are projections of one catalog. The
template's HTTP host does not publish a catalog. It carries the operation constants on the
generated partial.

An `IResponseRenderer<TResponse>` registered for a response type gives CLI and MCP a text
rendering. HTTP ignores it and returns JSON.

## Tool hints

`readOnlyHint` and `idempotentHint` come from the kind (`IQuery` is read-only;
`IIdempotentCommand` is idempotent). The request states the three hints the kind cannot supply.
`OperationDescriptor` carries them. `McpOperationBinding` stays the tool name.

The hints belong on a request that has `[Operation]` with a name that is not blank. A whitespace
name counts as no operation. The contracts generator and the endpoint emit model ignore
`[Destructive]` and `[OpenWorld]` without that name, so they emit no hint members. A reflection
reader sees the attributes either way.

| Hint | Declaration | Catalog | Unset |
|------|-------------|---------|-------|
| Title | `Title = "…"` on `[Operation]` | `Title` | Null or whitespace. Not the operation name |
| Destructive | `[Destructive]` or `[Destructive(false)]` | `DestructiveHint` | Attribute absent. A bare `[Destructive]` is true. MCP clients treat an omitted `destructiveHint` as true, and they read it only when the tool is not read-only |
| Open world | `[OpenWorld]` or `[OpenWorld(false)]` | `OpenWorldHint` | Attribute absent. MCP clients treat an omitted `openWorldHint` as true |

`[Destructive(false)]` means the operation only adds data. A delete or an overwrite is `[Destructive]`.
`destructiveHint` does not apply to an `IQuery`, because that kind is read-only.

A generator matches `TimeWarp.Contracts.OperationAttribute` (named argument `Title`),
`TimeWarp.Contracts.DestructiveAttribute`, and `TimeWarp.Contracts.OpenWorldAttribute`.
The constructor parameters are `isDestructive` and `isOpenWorld`. No argument means true.
The contracts generator copies a set hint onto the request partial and leaves the route alone.

```csharp
[Operation("roles.delete", Description = "Delete a role.", Group = "admin.roles", Title = "Delete role")]
[Destructive]
[OpenWorld(false)]
public sealed partial class Command
  : IIdempotentCommand<OneOf<Response, SharedProblemDetails>>
{
}
```

## Enforcement

**TWA0035** (info) reports a hosted contract whose request has no `[Operation]`. Hosted means
`[ApiEndpoint]`, a nested `Query`/`Command` with `[ApiRoute]`, and no `[ClientOnlyContract]`.
`[Operation]` is matched by its full name, `TimeWarp.Contracts.OperationAttribute`. The
diagnostic is a suggestion, so the build stays clean while contracts adopt the attribute.

## Example

```csharp
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.AdminRolesManage,
  AuthenticationSchemes = AuthenticationSchemeNames.IdentitySession + "," + AuthenticationSchemeNames.MockIdentitySession
)]
public static partial class UpdateRole
{
  [ApiRoute("api/Roles/{RoleId:guid}", HttpVerb.Put)]
  [Operation("roles.update", Description = "Replace a role's name and description.", Group = "admin.roles")]
  public sealed partial class Command
    : IAuthApiRequest, IRoleDetails, IIdempotentCommand<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
  }
  // Validator, Response, GetMockResponseFactory as usual
}
```

The living example is the template's roles slice (`web/features/admin/roles/`). All six
operations carry `[Operation]` and a kind interface.
