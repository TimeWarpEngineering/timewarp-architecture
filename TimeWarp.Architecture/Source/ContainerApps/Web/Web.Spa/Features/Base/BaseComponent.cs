namespace TimeWarp.Architecture.Features;

/// <summary>
/// Makes access to the State a little easier and by inheriting from
/// BlazorStateDevToolsComponent it allows for ReduxDevTools operation.
/// </summary>
/// <remarks>
/// In production one would NOT be required to use these base components
/// But would be required to properly implement the required interfaces.
/// one could conditionally inherit from BaseComponent for production build.
/// </remarks>
public abstract partial class BaseComponent : BlazorStateDevToolsComponent, IAttributeComponent
{
  [Parameter]
  public string? Class { get; set; }

  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
  internal ActionTrackingState ActionTrackingState => GetState<ActionTrackingState>();
  internal RouteState RouteState => GetState<RouteState>();
  protected string? BaseClass { get; set; }
  protected CssBuilder BaseCssBuilder { get; private set; }
  protected bool IsAnyActive(params Type[] aActions) => ActionTrackingState.IsAnyActive(aActions);
  protected async Task Send(IRequest aRequest) => await Mediator.Send(aRequest);

  protected override void OnParametersSet()
  {
    base.OnParametersSet();
    BaseCssBuilder = new CssBuilder(BaseClass);
    if (string.IsNullOrWhiteSpace(Class)) return;

    string[] classesToAdd = Class.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    foreach (string classToAdd in classesToAdd)
    {
      BaseCssBuilder = BaseCssBuilder.AddClass(classToAdd);
    }
  }
}
