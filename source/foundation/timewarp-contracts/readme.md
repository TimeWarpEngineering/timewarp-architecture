# TimeWarp.Contracts

Transport-neutral operation contracts shared by HTTP, CLI, and MCP hosts. One request class
is the operation. Each host projects it: this template's contracts generator and FastEndpoint
pipeline for HTTP, TimeWarp.Nuru for CLI and MCP. There is no second schema.

The package publishes from `source/foundation/` in TimeWarp.Architecture as its own package
(`TimeWarp.Contracts`, `1.0.0-beta` line). A standalone repository is the home before a stable
Nuru release takes the dependency. Dependencies are `TimeWarp.Mediator.Contracts` and `OneOf`.

## `[Operation]`

Apply `[Operation(name, Description = ...)]` to the request class (`Query` or `Command`).

- `name` is the canonical operation name. It is not an HTTP route, a CLI pattern, or an MCP tool name.
- `Description` is the operation summary hosts show in `--capabilities`, OpenAPI, and MCP `tools/list`.
- Kind is not an attribute argument. Generators read it from the mediator interface on the request.

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

## `IResponseRenderer<TResponse>`

A CLI or MCP host calls `Render` when a renderer is registered for the response type, and
writes that text. HTTP hosts do not consult it. With no renderer, the host writes JSON.

## `SharedProblemDetails`

The RFC 7807 problem type lives in this assembly, in namespace `TimeWarp.Foundation.Types`.
The namespace stays so `TypeForwardedTo` in `TimeWarp.Foundation.Contracts` preserves the
identity already compiled into callers. It has no ASP.NET Core dependency.
