namespace TimeWarp.Architecture.Features;

partial class BaseComponent
{
    internal ActionTrackingState ActionTrackingState => GetState<ActionTrackingState>();
    internal ActionTrackingState NoSubActionTrackingState => GetState<ActionTrackingState>(placeSubscription: false);
    internal RouteState RouteState => GetState<RouteState>();
    internal RouteState NoSubRouteState => GetState<RouteState>(placeSubscription: false);
}
