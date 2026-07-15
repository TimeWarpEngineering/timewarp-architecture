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
