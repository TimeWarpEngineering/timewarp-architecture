# IQueryStringRouteProviderMixin

This mixin will iterate over all the properties of the GetRouteWithQueryString method which builds a NameValueCollection of ParamerterName and Value. It will then return the route with the query string. 

## Usage

```csharp
namespace TimeWarp.Architecture.Features.TodoItems.Commands;

public static partial class GetRoles
{
  [RouteMixin("api/Roles", HttpVerb.Get)]
  [IOpenDataQueryParametersMixin]
  [IAuthApiRequestMixin]
  [IQueryStringRouteProviderMixin]
  public sealed partial class Query 
  {
    public string SomeProperty { get; set; }  
  }
}
```

## Generated code

```csharp
namespace TimeWarp.Architecture.Features.TodoItems.Commands;

public partial class GetRoles : IQueryStringRouteProvider
{
  partial class Query
  {
    public string GetRouteWithQueryString()
    {
      var collection = new NameValueCollection
      {
        { nameof(SomeProperty), SomeProperty },
        // From IAuthApiRequestMixin
        { nameof(UserId), UserId.ToString() }, 
        // From IOpenDataQueryParametersMixin
        { nameof(Top), Top.ToString() },
        { nameof(Skip), Skip.ToString() },
        { nameof(OrderBy), OrderBy },
        { nameof(Filter), Filter },
        { nameof(ReturnTotalCount), ReturnTotalCount.ToString() }
      };

      return $"{GetRoute()}?{this.GetQueryString(collection)}";
    }
  }
}
```
