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

  [Parameter(CaptureUnmatchedValues = true)]
  public IReadOnlyDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

  protected Task<TResponse> Send<TResponse>(IRequest<TResponse> aRequest) => Send(aRequest);

  protected bool IsProcessingAny(params string[] aActions) => ApplicationState.IsProcessingAny(aActions);
  protected async Task Send(IRequest aRequest) => await Mediator.Send(aRequest).ConfigureAwait(false);
}
