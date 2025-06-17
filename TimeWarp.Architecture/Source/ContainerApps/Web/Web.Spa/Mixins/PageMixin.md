# Page Mixin

This is used in place of the @page directive and must be put on a C# class in a `.cs` file.

> It Can NOT be used in a `.razor` file

## Usage

[Page("/todoitems/{TodoItemId:Guid}")]
public partial class TodoItemPage: BaseComponent;

## Generated code

```csharp
namespace TimeWarp.Architecture.Pages
{
  using Microsoft.AspNetCore.Components;
  [Route("/todoitems/{TodoItemId:guid}")]
  partial class TodoItemPage
  {
    public static string GetPageUrl(Guid TodoItemId) => FormattableString.Invariant($"/todoitems/{TodoItemId}");
    [Parameter] public Guid TodoItemId { get; set; }

  }
}
```
