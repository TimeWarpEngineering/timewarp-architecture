#nullable enable
namespace TimeWarp.Architecture.Features;

/// <summary>
/// Makes access to the State a little easier and by inheriting from
/// TimeWarpStateDevToolsComponent it allows for ReduxDevTools operation.
/// </summary>
/// <remarks>
/// In production one would NOT be required to use these base components
/// But would be required to properly implement the required interfaces.
/// one could conditionally inherit from BaseComponent for production build.
/// </remarks>
public abstract partial class BaseComponent : TimeWarpStateDevComponent, IAttributeComponent
{
  [Parameter]
  public string? Class { get; set; }

  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
  internal ActionTrackingState ActionTrackingState => GetState<ActionTrackingState>();
  internal ActionTrackingState NoSubActionTrackingState => GetState<ActionTrackingState>(placeSubscription: false);
  internal RouteState RouteState => GetState<RouteState>();
  internal RouteState NoSubRouteState => GetState<RouteState>(placeSubscription: false);
  protected string? BaseClass { get; set; }
  protected CssBuilder BaseCssBuilder { get; } = new();
  protected bool IsAnyActive(params Type[] actions) => ActionTrackingState.IsAnyActive(actions);

  [Obsolete(message:"Prefer using ActionSet methods so the cancellation tokens are automatically used. Also more concise")]
  protected async Task Send(IRequest request) => await Mediator.Send(request);

  protected override void OnParametersSet()
  {
    base.OnParametersSet();
    BaseCssBuilder.AddClass(BaseClass);
    if (string.IsNullOrWhiteSpace(Class)) return;

    string[] classesToAdd = Class.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    foreach (string classToAdd in classesToAdd)
    {
      BaseCssBuilder.AddClass(classToAdd);
    }
  }

  // Method to get the class attribute from the Attributes dictionary
  protected string? GetClassFromAttributes()
  {
    return Attributes.TryGetValue("class", out object? classValue) ?
      classValue as string :
      null;
  }
}
