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
    protected bool IsAnyActive(params Type[] actions) => ActionTrackingState.IsAnyActive(actions);

    [Obsolete(message:"Prefer using ActionSet methods so the cancellation tokens are automatically used. Also more concise")]
    protected async Task Send(IRequest request) => await Mediator.Send(request);
}
