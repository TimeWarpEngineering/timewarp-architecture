#region Purpose
// BaseComponent partial: typed accessors for commonly used states, with and without subscription.
#endregion

#region Design
// The NoSub variants read state without registering a re-render subscription — use them in event
// handlers and one-shot reads where subscribing would cause components to re-render on every
// change of a state they only sampled once.
// Centralizing the accessors keeps GetState<T>() calls (and the subscription decision) out of
// individual components.
#endregion

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
