# IOpenDataQueryParametersMixin

## Usage

```csharp
namespace TimeWarp.Architecture.Features.TodoItems.Commands;

public static partial class GetRoles
{
  [OpenDataQueryParametersMixin]
  public sealed partial class Query {}
}
```

## Generated code

```csharp
namespace TimeWarp.Architecture.Features.TodoItems.Commands;

public partial class GetRoles : IOpenDataQueryParameters
{
  partial class Query
  {
    public int? Top { get; set; }
    public int? Skip { get; set; }
    public string? Filter { get; set; }
    public string? OrderBy { get; set; }
    public bool ReturnTotalCount { get; set; }
  }
}
```
