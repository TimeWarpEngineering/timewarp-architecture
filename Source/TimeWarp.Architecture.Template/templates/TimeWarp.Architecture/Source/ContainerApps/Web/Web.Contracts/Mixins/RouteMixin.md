# RouteMixin

Tying to support [route constraints](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-8.0#route-constraints) 


## Usage

```csharp
namespace TimeWarp.Architecture.Features.TodoItems.Commands;

public partial class ParentClass
{
  [RouteMixin("api/TodoItems/{TodoItemId:guid}", HttpVerb.Post)]
  public partial class RouteMixinNestedTestClass
  {
     public string CanISeeRouteTemplate => RouteTemplate;
  }
}

```

## Generated code

```csharp
// Generated at 2024-04-14 01:32:01 UTC

namespace TimeWarp.Architecture.Features.TodoItems.Commands;

public partial class ParentClass
{
  partial class RouteMixinTestClass
  {
    public const string RouteTemplate = "api/TodoItems/{TodoItemId:guid}";
    public HttpVerb GetHttpVerb() => HttpVerb.Post;
    public string GetRoute(Guid TodoItemId) => FormattableString.Invariant($"api/TodoItems/{TodoItemId}");
    public Guid TodoItemId { get; set; }
  }
}

```
## Reference
https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing?view=aspnetcore-8.0#route-constraints
