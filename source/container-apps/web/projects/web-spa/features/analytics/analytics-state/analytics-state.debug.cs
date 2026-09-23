#region Purpose
// Test-only seeding of AnalyticsState's correlation id.
#endregion

#region Design
// CorrelationId is otherwise minted once in Initialize(); tests that assert a specific id
// need a bypass gated by TestCaller.Ensure so production code stays on app-load init.
// State's ThrowIfNotTestAssembly rejects kebab *-tests names (timewarp-state#607).
#endregion

namespace TimeWarp.Architecture.Features.Analytics;

partial class AnalyticsState
{
  /// <summary>
  /// Use in Tests ONLY, to initialize the State
  /// </summary>
  public void Initialize(Guid correlationId)
  {
    TimeWarp.Architecture.TestCaller.Ensure(Assembly.GetCallingAssembly());
    CorrelationId = correlationId;
  }
}
