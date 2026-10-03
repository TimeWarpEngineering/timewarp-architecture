# Page attribute (source generator)

Used in place of the `@page` directive. Must be on a C# partial class in a `.cs` file
(not in a `.razor` file). Implemented by `PageSourceGenerator` (`TimeWarp.Architecture.Generators`).

## Usage

```csharp
// Public page — Policy omitted → Policies.Anonymous
[Page("/todoitems/{TodoItemId:Guid}")]
public partial class TodoItemPage : BaseComponent;

// Gated page — Policy must be a product const field reference (pit of success)
[Page("/settings", Policy = Policies.SettingsEdit)]
public partial class SettingsPage : BaseComponent;
```

**Multiple routes (multi-tab pages):** one `[Page]`, primary route first, aliases after it.

```csharp
[Page("/clients", "/clients/revenue", "/clients/me-close", Policy = Policies.ClientsRead, Navigable = true)]
public partial class ClientsPage : BaseComponent;

[Page("/clients/{ClientId:string}", "/clients/{ClientId}/revenue")]
public partial class ClientDetailPage : BaseComponent;
```

| Concern | Uses |
|---------|------|
| `GetPageUrl`, `IStaticRoute`, `PageRegistry` row, `Navigable` / TWE009 | primary route only |
| `[Route]` | one per route (primary + every alias) |
| `Policy` | once per page (all routes) |
| alias token without a type | inherits the type an earlier route gave that name |

Errors (no page surface generated): **TWE010** — two routes of the page are the same Blazor route
(case or token name only differ; includes a hand-written `[Route]` that repeats one). **TWE011** —
stacked `[Page]`, a non-literal alias, or an alias token typed differently from an earlier route of the page.

**Policy rules (TWE005):**

| Form | Result |
|------|--------|
| omitted | `Policies.Anonymous` |
| `Policy = Policies.X` (const) | emit that expression |
| `Policy = "…"` / `nameof(...)` | **error TWE005** |

Do not pass claim string literals. Define `public const string X = "…"` (or `nameof(X)`) on a
product `Policies` type and reference the field.

## Generated code

```csharp
namespace TimeWarp.Architecture.Features.ToDo
{
  using Microsoft.AspNetCore.Components;
  using TimeWarp.Architecture;

  [Route("/todoitems/{TodoItemId:guid}")]
  partial class TodoItemPage : INavigableComponent
  {
    public static string GetPageUrl(Guid TodoItemId) => FormattableString.Invariant($"/todoitems/{TodoItemId}");
    public static string Policy { get; } = Policies.Anonymous;
    [Parameter] public Guid TodoItemId { get; set; }
  }
}
```
