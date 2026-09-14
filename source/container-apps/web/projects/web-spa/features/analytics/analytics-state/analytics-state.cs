#region Purpose
// Holds the per-app-load analytics correlation id used when posting opted-in SPA actions.
#endregion

#region Design
// The client is intentionally dumb: event name + correlation id only — no payload, no vendor SDK.
// The SPA carries no other CorrelationId, so this Guid is minted once in Initialize (app load)
// and reused for every TrackEvent POST in that session. Mutation of telemetry happens only in
// the TrackEvent ActionSet handler (HTTP side effect); this state exists so the generated
// dispatcher and correlation id have a TimeWarp.State home.
#endregion

namespace TimeWarp.Architecture.Features.Analytics;

[StateAccess]
internal sealed partial class AnalyticsState : State<AnalyticsState>
{
  public Guid CorrelationId { get; private set; }

  public override void Initialize() => CorrelationId = Guid.NewGuid();
}
