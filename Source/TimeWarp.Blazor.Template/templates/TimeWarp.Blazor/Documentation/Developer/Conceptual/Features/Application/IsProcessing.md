Title: Clean up your hard drive with WizTree
Published: 12/11/2021
Tags: 
  - dev-tools
Author: Steven T. Cramer
---

# ApplicationState IsProcessing

In the [`Client`](\Source\Client\TimeWarp.Blazor.Client.csproj) project [`ApplicationState`](\Source\Client\Features\Application\ApplicationState.cs) maintains a list of actions that are currently being processed by the pipeline. If this list contains any items then the `IsProcessing` attribute is set to true.

The primary purpose is to allow the UI to reflect when we are waiting for tasks to complete.  "Loading..."

The BaseComponent exposes the following method:

`protected bool IsProcessingAny(params string[] aActions)`

And would be used in a component similar as follows:

```csharp
    protected bool IsProcessing => IsProcessingAny
    (
      nameof(ChangeRouteAction),
      nameof(FetchSomethingAction),
      nameof(FetchSomethingElseAction)
    );
```    

The above `IsProcessing` then could be used to toggle various UI features in the Component

```razor
@if (IsProcessing)
{
  <LoadingSpinIcon />
}
else
{
  ...
}
```
