#region Purpose
// Proves GenAI spans, the sensitive-payload gate, and upstream error-body logging.
#endregion

#region Design
// The instrumented client is OpenAiXaiChatClientFactory.Instrument around a stub, so a completion
// never dials xAI. ActivityListener records spans for GenAiTelemetry.SourceName. CompleteAsync
// is built with a mutable options monitor and a factory that records the sensitive flag or throws
// ClientResultException. The response body is the only text the log may carry; an Authorization
// header value must stay out of the record and out of the client problem.
#endregion

namespace XaiChatTelemetry_;

using System.ClientModel;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Features.AgentChats.Application;
using TimeWarp.Foundation.Configuration;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.AgentChats.CompleteAgentChat;

public class Instrument_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Instrument_Given_>();

  [Proves(typeof(OpenAiXaiChatClientFactory), nameof(OpenAiXaiChatClientFactory.Instrument))]
  public static async Task Sensitive_Should_RecordPromptReplyAndTokenUsage()
  {
    const string prompt = "SECRET-PROMPT-ALPHA";
    const string reply = "SECRET-REPLY-GAMMA";
    List<Activity> stopped = [];
    using ActivityListener listener = Listen(stopped);
    using MetricCollector<int> tokens = new
    (
      meterScope: null,
      meterName: GenAiTelemetry.MeterName,
      instrumentName: "gen_ai.client.token.usage"
    );
    using IChatClient client = OpenAiXaiChatClientFactory.Instrument
    (
      new ScriptedChatClient(reply),
      NullLoggerFactory.Instance,
      enableSensitiveTelemetry: true
    );

    ChatResponse response = await client.GetResponseAsync
    (
      [new ChatMessage(ChatRole.User, prompt)],
      new ChatOptions { ModelId = "grok-4.7" }
    );

    response.Text.ShouldBe(reply);
    stopped.Count.ShouldBe(1);
    Activity activity = stopped[0];
    activity.Source.Name.ShouldBe(GenAiTelemetry.SourceName);
    string tags = JoinTags(activity);
    tags.ShouldContain(prompt);
    tags.ShouldContain(reply);
    tokens.GetMeasurementSnapshot().Count.ShouldBeGreaterThan(0);
  }

  [Proves(typeof(OpenAiXaiChatClientFactory), nameof(OpenAiXaiChatClientFactory.Instrument))]
  public static async Task Omitted_Should_LeavePromptAndReplyOffTheSpan()
  {
    const string prompt = "SECRET-PROMPT-BETA";
    const string reply = "SECRET-REPLY-DELTA";
    List<Activity> stopped = [];
    using ActivityListener listener = Listen(stopped);
    using IChatClient client = OpenAiXaiChatClientFactory.Instrument
    (
      new ScriptedChatClient(reply),
      NullLoggerFactory.Instance,
      enableSensitiveTelemetry: false
    );

    await client.GetResponseAsync
    (
      [new ChatMessage(ChatRole.User, prompt)],
      new ChatOptions { ModelId = "grok-4.7" }
    );

    stopped.Count.ShouldBe(1);
    Activity activity = stopped[0];
    activity.Source.Name.ShouldBe(GenAiTelemetry.SourceName);
    activity.Tags.ShouldContain(tag => tag.Key == "gen_ai.operation.name" && tag.Value == "chat");
    string tags = JoinTags(activity);
    tags.ShouldNotContain(prompt);
    tags.ShouldNotContain(reply);
  }

  private static ActivityListener Listen(List<Activity> stopped)
  {
    ActivityListener listener = new()
    {
      ShouldListenTo = source => source.Name == GenAiTelemetry.SourceName,
      Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
      ActivityStopped = activity => stopped.Add(activity)
    };
    ActivitySource.AddActivityListener(listener);
    return listener;
  }

  private static string JoinTags(Activity activity)
  {
    StringBuilder builder = new();
    foreach (KeyValuePair<string, string?> tag in activity.Tags)
    {
      builder.Append(tag.Key);
      builder.Append('=');
      builder.Append(tag.Value);
      builder.Append('\n');
    }

    return builder.ToString();
  }
}

public class Rent_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Rent_Given_>();

  [Proves(typeof(XaiChatClientCache), nameof(XaiChatClientCache.Rent))]
  public static async Task FlagChange_Should_BuildASeparateClient()
  {
    RecordingFactory factory = new();
    using XaiChatClientCache cache = new(factory);

    IChatClient first = cache.Rent("xai-test-key", XaiChatDefaults.DefaultEndpoint, XaiChatDefaults.DefaultModel, false);
    IChatClient again = cache.Rent("xai-test-key", XaiChatDefaults.DefaultEndpoint, XaiChatDefaults.DefaultModel, false);
    cache.Release(again);
    IChatClient sensitive = cache.Rent("xai-test-key", XaiChatDefaults.DefaultEndpoint, XaiChatDefaults.DefaultModel, true);

    ReferenceEquals(first, again).ShouldBeTrue();
    ReferenceEquals(first, sensitive).ShouldBeFalse();
    factory.Flags.ShouldBe([false, true]);
    await Task.CompletedTask;
  }
}

public class CompleteAsync_Given_
{
  private const string Sentinel = "Bearer SENTINEL-KEY-SHOULD-NOT-LOG";
  private const string ErrorBody = """{"code":"invalid-argument","error":"Incorrect API key provided."}""";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CompleteAsync_Given_>();

  [Proves(typeof(XaiChatUpstream), nameof(XaiChatUpstream.CompleteAsync))]
  public static async Task ClientResult_Should_LogStatusAndBodyAndReturnUpstreamFailed()
  {
    ClientResultException exception = new
    (
      "Status: 400 (Bad Request)",
      new ScriptedPipelineResponse(400, "Bad Request", ErrorBody, Sentinel),
      innerException: null
    );
    FakeLogger<XaiChatUpstream> logger = new();
    using XaiChatUpstream upstream = CreateUpstream(logger, new ThrowingFactory(exception), enableSensitiveTelemetry: false);

    OneOf<Response, SharedProblemDetails> result = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);

    result.IsT1.ShouldBeTrue();
    SharedProblemDetails problem = result.AsT1;
    problem.Status.ShouldBe(502);
    problem.Title.ShouldBe("Model request failed");
    string detail = problem.Detail.ShouldNotBeNull();
    detail.ShouldBe("The model request failed.");
    detail.ShouldNotContain("Incorrect API key");
    detail.ShouldNotContain(Sentinel);
    FakeLogRecord record = logger.Collector.LatestRecord;
    record.Level.ShouldBe(LogLevel.Error);
    IReadOnlyList<KeyValuePair<string, string?>> state = record.StructuredState.ShouldNotBeNull();
    state.ShouldContain(pair => pair.Key == "Status" && pair.Value == "400");
    state.ShouldContain(pair => pair.Key == "ResponseBody" && pair.Value == ErrorBody);
    AssertNoSentinel(record, state);
  }

  [Proves(typeof(XaiChatUpstream), nameof(XaiChatUpstream.CompleteAsync))]
  public static async Task ClientResult_Should_NormalizeAndCapTheBody()
  {
    string noisy = "  alpha\n\nbeta   " + new string('x', 600);
    ClientResultException exception = new
    (
      "Status: 400 (Bad Request)",
      new ScriptedPipelineResponse(400, "Bad Request", noisy, Sentinel),
      innerException: null
    );
    FakeLogger<XaiChatUpstream> logger = new();
    using XaiChatUpstream upstream = CreateUpstream(logger, new ThrowingFactory(exception), enableSensitiveTelemetry: false);

    OneOf<Response, SharedProblemDetails> result = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);

    result.IsT1.ShouldBeTrue();
    result.AsT1.Detail.ShouldBe("The model request failed.");
    FakeLogRecord record = logger.Collector.LatestRecord;
    IReadOnlyList<KeyValuePair<string, string?>> state = record.StructuredState.ShouldNotBeNull();
    string? body = state.Single(pair => pair.Key == "ResponseBody").Value;
    body.ShouldNotBeNull();
    body.Length.ShouldBe(512);
    body.ShouldStartWith("alpha beta ");
    body.ShouldNotContain("\n");
    body.ShouldNotContain("  ");
    body[^1].ShouldBe('x');
    AssertNoSentinel(record, state);
  }

  [Proves(typeof(XaiChatUpstream), nameof(XaiChatUpstream.CompleteAsync))]
  public static async Task OptionChange_Should_RentWithTheNewFlag()
  {
    RecordingFactory factory = new();
    MutableMonitor monitor = new()
    {
      CurrentValue = new XaiChatOptions
      {
        ApiKey = "xai-test-key",
        EnableSensitiveTelemetry = false
      }
    };
    FakeLogger<XaiChatUpstream> logger = new();
    using XaiChatUpstream upstream = CreateUpstream(logger, factory, monitor);

    OneOf<Response, SharedProblemDetails> first = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);
    monitor.CurrentValue.EnableSensitiveTelemetry = true;
    OneOf<Response, SharedProblemDetails> second = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);

    first.IsT0.ShouldBeTrue();
    second.IsT0.ShouldBeTrue();
    factory.Flags.ShouldBe([false, true]);
  }

  private static void AssertNoSentinel(FakeLogRecord record, IReadOnlyList<KeyValuePair<string, string?>> state)
  {
    record.Message.ShouldNotContain(Sentinel);
    (record.Exception?.ToString() ?? "").ShouldNotContain(Sentinel);
    foreach (KeyValuePair<string, string?> pair in state)
    {
      (pair.Value ?? "").ShouldNotContain(Sentinel);
    }
  }

  private static XaiChatUpstream CreateUpstream(
    FakeLogger<XaiChatUpstream> logger,
    IXaiChatClientFactory clients,
    bool enableSensitiveTelemetry)
  {
    MutableMonitor monitor = new()
    {
      CurrentValue = new XaiChatOptions
      {
        ApiKey = "xai-test-key",
        EnableSensitiveTelemetry = enableSensitiveTelemetry
      }
    };
    return CreateUpstream(logger, clients, monitor);
  }

  private static XaiChatUpstream CreateUpstream(
    FakeLogger<XaiChatUpstream> logger,
    IXaiChatClientFactory clients,
    MutableMonitor monitor)
  {
    ServiceCollection services = new();
    services.AddSingleton<IXaiApiKeyResolver>(new FixedResolver("xai-test-key"));
    ServiceProvider provider = services.BuildServiceProvider();
    return new XaiChatUpstream
    (
      monitor,
      new XaiChatUpstream_.XaiChatUpstreamFixture.NamedEnvironment("Production"),
      provider.GetRequiredService<IServiceScopeFactory>(),
      clients,
      logger
    );
  }

  private static Command UserTurn() => new()
  {
    Messages = [new Turn { Role = "user", Text = "What is on this page?" }]
  };

  private sealed class MutableMonitor : IOptionsMonitor<XaiChatOptions>
  {
    public required XaiChatOptions CurrentValue { get; set; }

    public XaiChatOptions Get(string? name) => CurrentValue;

    public IDisposable OnChange(Action<XaiChatOptions, string?> listener) => new Noop();

    private sealed class Noop : IDisposable
    {
      public void Dispose()
      {
      }
    }
  }

  private sealed class FixedResolver : IXaiApiKeyResolver
  {
    private readonly string ApiKey;

    public FixedResolver(string apiKey) => ApiKey = apiKey;

    public Task<ResolvedXaiApiKey> ResolveAsync(CancellationToken cancellationToken) =>
      Task.FromResult(new ResolvedXaiApiKey(ApiKey, XaiByokMode.Allow, XaiApiKeyOrigin.Host));
  }

  private sealed class ThrowingFactory : IXaiChatClientFactory
  {
    private readonly ClientResultException Exception;

    public ThrowingFactory(ClientResultException exception) => Exception = exception;

    public IChatClient Create(string apiKey, string endpoint, string model, bool enableSensitiveTelemetry)
    {
      _ = apiKey;
      _ = endpoint;
      _ = model;
      _ = enableSensitiveTelemetry;
      return new ThrowingChatClient(Exception);
    }
  }
}

public class DevelopmentSettings_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DevelopmentSettings_Given_>();

  public static async Task EnableSensitiveTelemetry_Should_DefaultOffAndBeOnInDevelopmentOnly()
  {
    new XaiChatOptions().EnableSensitiveTelemetry.ShouldBeFalse();
    Bind("appsettings.json").EnableSensitiveTelemetry.ShouldBeFalse();
    Bind("appsettings.Production.json").EnableSensitiveTelemetry.ShouldBeFalse();
    Bind("appsettings.Development.json").EnableSensitiveTelemetry.ShouldBeTrue();
    await Task.CompletedTask;
  }

  private static XaiChatOptions Bind(string fileName)
  {
    string directory = ProjectContentRoot.Resolve(typeof(XaiChatRegistration).Assembly);
    IConfiguration configuration = new ConfigurationBuilder()
      .AddJsonFile(Path.Combine(directory, fileName))
      .Build();
    XaiChatOptions options = new();
    configuration.GetSection(XaiChatOptions.SectionName).Bind(options);
    return options;
  }
}
