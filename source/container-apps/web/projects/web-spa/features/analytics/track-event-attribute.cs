#region Purpose
// Opt-in marker that tells TrackEventBehavior to POST this action's type name after it succeeds.
#endregion

#region Design
// The client is intentionally dumb: event name + correlation id only — no payload, no vendor SDK.
// The server decides the sink. Actions opt in via this attribute rather than tracking every
// action by default, so a slice can be deleted without a hidden telemetry tax and demos stay
// explicit. Applied to the nested Action type (not the ActionSet) because that is the TRequest
// the pipeline sees.
// Features substrate (bare …Features namespace): any product slice can tag an action without
// [CrossSliceReference]. TrackEventBehavior and AnalyticsState stay in Features.Analytics.
#endregion

namespace TimeWarp.Architecture.Features;

/// <summary>
/// Marks a TimeWarp.State action so the analytics TrackEvent pipeline behavior posts
/// the action's type name to the analytics endpoint after the action succeeds.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class TrackEventAttribute : Attribute;
