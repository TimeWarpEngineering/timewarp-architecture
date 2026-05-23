namespace TimeWarp.Architecture.Features;

using TimeWarp.Features.Theme;

partial class BaseComponent
{
    internal ActionTrackingState ActionTrackingState => GetState<ActionTrackingState>();
    internal ActionTrackingState NoSubActionTrackingState => GetState<ActionTrackingState>(placeSubscription: false);
    internal RouteState RouteState => GetState<RouteState>();
    internal RouteState NoSubRouteState => GetState<RouteState>(placeSubscription: false);
    internal ThemeState ThemeState => GetState<ThemeState>();
    internal ThemeState NoSubThemeState => GetState<ThemeState>(placeSubscription: false);
}
