#region Purpose
// Round-trip tests for TrackEvent.Command after CorrelationId was added.
#endregion

#region Design
// EventName + optional CorrelationId are auto-properties, but CorrelationId is Guid? and must
// survive camelCase JSON as a UUID string (present) or omit/null (absent). That is the seam
// the SPA POST uses.
#endregion

namespace TrackEventContracts_;

using TimeWarp.Architecture.Features.Analytics;
using TimeWarp.Architecture.Web.Contracts.Tests;

public class TrackEvent_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TrackEvent_Command_Should>();

  public static Task SerializeAndDeserialize_With_CorrelationId()
  {
    Guid correlationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    TrackEvent.Command command = new()
    {
      EventName = "IncrementCounterActionSet.Action",
      CorrelationId = correlationId
    };

    TrackEvent.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.EventName.ShouldBe(command.EventName);
    parsed.CorrelationId.ShouldBe(correlationId);
    return Task.CompletedTask;
  }

  public static Task SerializeAndDeserialize_Without_CorrelationId()
  {
    TrackEvent.Command command = new() { EventName = "SomeEvent" };

    TrackEvent.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.EventName.ShouldBe("SomeEvent");
    parsed.CorrelationId.ShouldBeNull();
    return Task.CompletedTask;
  }
}
