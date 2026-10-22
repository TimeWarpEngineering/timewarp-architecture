# TimeWarp.Contracts

Transport-neutral operation contracts shared by HTTP, CLI, and MCP hosts. One request class
is the operation. Each host projects it: this template's contracts generator and FastEndpoint
pipeline for HTTP, TimeWarp.Nuru for CLI and MCP. There is no second schema.

The package publishes from `source/foundation/` in TimeWarp.Architecture as its own package
(`TimeWarp.Contracts`), versioned with the repository. It targets `net10.0` and `net11.0`.
A standalone repository is the home before a stable Nuru release takes the dependency.
Dependencies are `TimeWarp.Mediator.Contracts` and `OneOf`.

## `[Operation]`

Apply `[Operation(name, Description = ..., Group = ..., Title = ...)]` to the request class (`Query` or `Command`).

- `name` is the canonical operation name. It is not an HTTP route, a CLI pattern, or an MCP tool name.
- `Description` is the operation summary hosts show in `--capabilities`, OpenAPI, and MCP `tools/list`.
- `Group` is the operation's purpose group. Segments are separated by `.` or space. Null, empty, and
  whitespace mean ungrouped. CLI, MCP, and HTTP hosts read the same value from the catalog. The group
  does not prefix the route pattern or the command name: the CLI command stays the operation name
  (`get_symbol`, not `symbols get_symbol`).
- `Title` is the human-readable name hosts project to MCP `title`. Null, empty, and whitespace mean
  no title. It is not the canonical name and not the summary.
- Kind is not an attribute argument. Generators read it from the mediator interface on the request.

## Tool hints

`readOnlyHint` and `idempotentHint` come from the kind. The contract states the three hints the kind
cannot supply. A generator reads them by metadata name.

The hints belong on a request that has `[Operation]` with a name that is not blank. The contracts
generator and the endpoint emit model ignore `[Destructive]` and `[OpenWorld]` without that name.
A reflection reader still sees the attributes.

| Hint | Declaration | Catalog field | Unset |
| --- | --- | --- | --- |
| Title | `Title` on `[Operation]` | `OperationDescriptor.Title` | Null or whitespace |
| Destructive | `[Destructive]`, `[Destructive(false)]` | `DestructiveHint` | Attribute absent. That is not false. MCP clients treat an omitted `destructiveHint` as true, and they read it only when the tool is not read-only |
| Open world | `[OpenWorld]`, `[OpenWorld(false)]` | `OpenWorldHint` | Attribute absent. MCP clients treat an omitted `openWorldHint` as true |

`[Destructive]` and `[OpenWorld]` with no argument are true. `[Destructive(true)]` is the same claim
as a bare `[Destructive]`. `[Destructive(false)]` states that the operation only adds data: it does
not delete or overwrite. `[OpenWorld(false)]` states that it only touches local state. `destructiveHint`
does not apply to an `IQuery`, because that kind is read-only.

```csharp
[Operation("notes.append", Description = "Append a note to the log.", Title = "Append note")]
[Destructive(false)]
[OpenWorld(false)]
public sealed class AppendNoteCommand : ICommand<string> { }
```

Metadata names: `TimeWarp.Contracts.OperationAttribute` (named argument `Title`),
`TimeWarp.Contracts.DestructiveAttribute`, and `TimeWarp.Contracts.OpenWorldAttribute`.
The constructor parameters are `isDestructive` and `isOpenWorld`.

TimeWarp.Nuru has its own `[Destructive]` and `[OpenWorld]` with the same constructors. A file that
imports both `TimeWarp.Contracts` and `TimeWarp.Nuru` gets an ambiguous reference (CS0104) on the
bare name. Put the contracts attributes on the contract, and qualify the name
(`[TimeWarp.Contracts.Destructive]`) where both namespaces are in scope.

## Kind

| Request implements | `OperationKind` |
| --- | --- |
| `IQuery<TResponse>` | `Query` |
| `IIdempotentCommand` or `IIdempotentCommand<TResponse>` | `IdempotentCommand` |
| `ICommand` or `ICommand<TResponse>` | `Command` |

Test `IIdempotentCommand` before `ICommand`. The idempotent interfaces extend the command
interfaces, and the more specific kind wins. `IQuery<TResponse>` stands on its own.
A request that implements none of these is omitted from the catalog.
The HTTP verb lives on the HTTP binding, supplied by the host that owns `[ApiRoute]`.

## Member derivation

Generators derive member metadata. This package does not reflect over request types.

| Field | Rule |
| --- | --- |
| Name | The property's declared identifier, as written on the request. |
| Required | True when the property has the `required` modifier. Otherwise true when the type is non-nullable and the property has no initializer. A nullable annotation (`T?`) is not required. `required` wins over a nullable annotation and over an initializer. |
| Default | The property initializer rendered as source text. Omitted when the property has no initializer. Generators do not invent `0`, `false`, `null`, or `default(T)`. |
| Allowed values | When the member type is an enum, the enum's named constants in declaration order. Omitted for every other type. |
| Description | `System.ComponentModel.DescriptionAttribute.Description` on the property. Omitted when the attribute is absent. XML documentation is not this field; HTTP OpenAPI keeps using the request's XML docs. |

## Catalog

`OperationCatalog` is the document. `--capabilities`, OpenAPI, and MCP `tools/list` are
projections of it, not separate schemas.

- `OperationDescriptor.ReturnType` is the success type's display name. Hosts that return
  `OneOf<TSuccess, SharedProblemDetails>` record `TSuccess`. The problem type is shared and
  is not repeated on each entry.
- `OperationBindings` holds the transports a host actually binds: HTTP route and verb, CLI
  pattern, MCP tool name. A missing binding means that transport does not expose the operation.
- `OperationDescriptor.Group` is the purpose group from `[Operation]`. Null when the operation is
  ungrouped. A host reads it as metadata (CLI `groupPath`, MCP tool filtering). This package does
  not prefix a route or a command name with it.
- `OperationDescriptor.Title`, `DestructiveHint`, and `OpenWorldHint` are the tool hints above.
  Null means the request did not state that hint. A host omits null and still writes an explicit
  false. The hints are operation facts, not `McpOperationBinding` fields, so a CLI catalog can
  project them when MCP is unbound. They do not change the tool name.

## `IResponseRenderer<TResponse>`

A CLI or MCP host calls `Render` when a renderer is registered for the response type, and
writes that text. HTTP hosts do not consult it. With no renderer, the host writes JSON.

## `SharedProblemDetails`

The RFC 7807 problem type lives in this assembly, in namespace `TimeWarp.Foundation.Types`.
The namespace stays so `TypeForwardedTo` in `TimeWarp.Foundation.Contracts` preserves the
identity already compiled into callers. It has no ASP.NET Core dependency.
