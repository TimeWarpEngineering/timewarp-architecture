namespace TrackEventHandler_;

using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using TimeWarp.Foundation.Configuration;
using TrackEventApplication = TimeWarp.Architecture.Features.Analytics.Application.TrackEvent;
using static TimeWarp.Architecture.Features.Analytics.TrackEvent;

public class Handle_Returns
{

  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Handle_Returns>();

  public static async Task SetupOnce()
  {
#if(api)
    Graph = await HostGraphFactory.CreateWebWithApiAsync();
#else
    Graph = await HostGraphFactory.CreateWebAsync();
#endif
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  private static Command CreateValidCommand() => new() { EventName = "SomeEvent" };

  public static async Task Ok_Given_Valid_Request()
  {

    Command command = CreateValidCommand();

    OneOf<Response, SharedProblemDetails> result = await Web.Send(command);

    ValidateResult(result);
  }

  private static void ValidateResult(OneOf<Response, SharedProblemDetails> result)
  {
    result.Switch(
        response => response.ShouldNotBeNull(),
        problemDetails =>
        {
          // This should not happen in a successful case
          problemDetails.ShouldBeNull("The SignIn handler returned SharedProblemDetails instead of a successful response.");
        }
    );
  }

}

public class Handle_Emits
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Handle_Emits>();

  public static async Task Log_Record_With_Event_Name_And_Correlation_Id()
  {
    FakeLogger<TrackEventApplication.Handler> logger = new();
    TrackEventApplication.Handler handler = new(logger);
    Guid correlationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    Command command = new()
    {
      EventName = "SomeEvent",
      CorrelationId = correlationId
    };

    OneOf<Response, SharedProblemDetails> result = await handler.Handle(command, CancellationToken.None);

    result.IsT0.ShouldBeTrue();
    FakeLogRecord record = logger.Collector.LatestRecord;
    record.Level.ShouldBe(LogLevel.Information);
    IReadOnlyList<KeyValuePair<string, string?>> state = record.StructuredState.ShouldNotBeNull();
    state.ShouldContain(pair => pair.Key == AnalyticsMeters.EventNameTag && pair.Value == "SomeEvent");
    state.ShouldContain(pair => pair.Key == AnalyticsMeters.CorrelationIdTag && pair.Value == correlationId.ToString());
  }

  public static async Task Counter_Increment_Tagged_By_Event_Name()
  {
    FakeLogger<TrackEventApplication.Handler> logger = new();
    using MetricCollector<long> collector = new
    (
      meterScope: null,
      meterName: AnalyticsMeters.MeterName,
      instrumentName: AnalyticsMeters.EventsInstrumentName
    );
    TrackEventApplication.Handler handler = new(logger);
    string eventName = $"counter-event-{Guid.NewGuid():N}";

    OneOf<Response, SharedProblemDetails> result = await handler.Handle
    (
      new Command { EventName = eventName },
      CancellationToken.None
    );

    result.IsT0.ShouldBeTrue();
    List<CollectedMeasurement<long>> matches = collector.GetMeasurementSnapshot()
      .Where(item => Equals(item.Tags[AnalyticsMeters.EventNameTag], eventName))
      .ToList();
    matches.Count.ShouldBe(1);
    matches[0].Value.ShouldBe(1L);
  }
}
