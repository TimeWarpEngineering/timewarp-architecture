# IOpenDataQueryParametersMixin

## Usage

```csharp
namespace TimeWarp.Architecture.Features.TodoItems.Commands;

public static partial class GetRoles
{
  [IAuthApiRequestMixin]
  public sealed partial class Query {}
}
```

## Generated code

```csharp
namespace TimeWarp.Architecture.Features.TodoItems.Commands;

public partial class GetRoles : IAuthApiRequest
{
  partial class Query
  {
    public Guid UserId { get; set; }
  }
}
```
