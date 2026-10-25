#region Purpose
// Entra issuer rules and a stubbed discovery client: found, not found, timeout, and cache.
#endregion

#region Design
// Microsoft's endpoint is an externality. These tests never open a socket. The handler records
// the request URI and returns a scripted status, a thrown HttpRequestException, or a delay that
// honors the caller's token so a short LookupTimeout proves TimedOut. Found and NotFound stay
// in the memory cache. Network failure, timeout, unexpected status, and unexpected issuer do not,
// and each of those writes one Warning.
#endregion

namespace EntraTenantResolver_;

using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public class Issuer_Given_
{
  private static readonly Guid Tenant = Guid.Parse("11111111-2222-3333-4444-555555555555");

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Issuer_Given_>();

  public static Task Valid_V2_Issuer_Should_Return_The_Guid()
  {
    string issuer = $"https://login.microsoftonline.com/{Tenant:D}/v2.0";
    EntraIssuer.TryParse(issuer, out Guid tenantId, out EntraIssuerParseFailure failure).ShouldBeTrue();
    tenantId.ShouldBe(Tenant);
    failure.ShouldBe(EntraIssuerParseFailure.None);
    return Task.CompletedTask;
  }

  public static Task Blank_Issuer_Should_Be_Malformed()
  {
    EntraIssuer.TryParse("  ", out Guid tenantId, out EntraIssuerParseFailure failure).ShouldBeFalse();
    tenantId.ShouldBe(Guid.Empty);
    failure.ShouldBe(EntraIssuerParseFailure.Malformed);
    return Task.CompletedTask;
  }

  public static Task Non_Uri_Should_Be_Malformed()
  {
    EntraIssuer.TryParse("not a url", out _, out EntraIssuerParseFailure failure).ShouldBeFalse();
    failure.ShouldBe(EntraIssuerParseFailure.Malformed);
    return Task.CompletedTask;
  }

  public static Task Wrong_Host_Should_Be_WrongHost()
  {
    EntraIssuer.TryParse(
      $"https://login.microsoft.com/{Tenant:D}/v2.0",
      out _,
      out EntraIssuerParseFailure failure).ShouldBeFalse();
    failure.ShouldBe(EntraIssuerParseFailure.WrongHost);
    return Task.CompletedTask;
  }

  public static Task Non_Guid_Segment_Should_Be_NotAGuid()
  {
    EntraIssuer.TryParse(
      "https://login.microsoftonline.com/contoso/v2.0",
      out _,
      out EntraIssuerParseFailure failure).ShouldBeFalse();
    failure.ShouldBe(EntraIssuerParseFailure.NotAGuid);
    return Task.CompletedTask;
  }

  public static Task Empty_Guid_Should_Be_NotAGuid()
  {
    EntraIssuer.TryParse(
      "https://login.microsoftonline.com/00000000-0000-0000-0000-000000000000/v2.0",
      out _,
      out EntraIssuerParseFailure failure).ShouldBeFalse();
    failure.ShouldBe(EntraIssuerParseFailure.NotAGuid);
    return Task.CompletedTask;
  }

  public static Task Query_String_Should_Be_Malformed()
  {
    EntraIssuer.TryParse(
      $"https://login.microsoftonline.com/{Tenant:D}/v2.0?x=1",
      out _,
      out EntraIssuerParseFailure failure).ShouldBeFalse();
    failure.ShouldBe(EntraIssuerParseFailure.Malformed);
    return Task.CompletedTask;
  }
}

public class Lookup_Given_
{
  private static readonly Guid Tenant = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Lookup_Given_>();

  public static async Task Success_Should_Parse_The_Issuer_Guid()
  {
    using LookupHarness harness = LookupHarness.Script(_ => Json(HttpStatusCode.OK, IssuerBody(Tenant)));

    EntraTenantLookupResult result = await harness.Resolver.ResolveAsync("Contoso.COM", CancellationToken.None);

    result.Outcome.ShouldBe(EntraTenantLookupOutcome.Found);
    result.TenantId.ShouldBe(Tenant);
    result.Domain.ShouldBe("contoso.com");
    harness.Handler.LastUri.ShouldBe(
      new Uri("https://login.microsoftonline.com/contoso.com/.well-known/openid-configuration"));
    harness.Logger.Warnings.ShouldBeEmpty();
  }

  public static async Task Not_Found_Status_Should_Be_Distinct_And_Cached()
  {
    using LookupHarness harness = LookupHarness.Script(_ => Json(HttpStatusCode.NotFound, """{"error":"not_found"}"""));

    EntraTenantLookupResult first = await harness.Resolver.ResolveAsync("missing.example", CancellationToken.None);
    EntraTenantLookupResult second = await harness.Resolver.ResolveAsync("missing.example", CancellationToken.None);

    first.Outcome.ShouldBe(EntraTenantLookupOutcome.NotFound);
    second.Outcome.ShouldBe(EntraTenantLookupOutcome.NotFound);
    harness.Handler.Calls.ShouldBe(1);
    harness.Logger.Warnings.ShouldBeEmpty();
  }

  public static async Task Invalid_Tenant_Body_Should_Be_Not_Found()
  {
    using LookupHarness harness = LookupHarness.Script(_ => Json(
      HttpStatusCode.BadRequest,
      """{"error":"invalid_tenant","error_codes":[90002]}"""));

    EntraTenantLookupResult result = await harness.Resolver.ResolveAsync("unknown.example", CancellationToken.None);

    result.Outcome.ShouldBe(EntraTenantLookupOutcome.NotFound);
    harness.Logger.Warnings.ShouldBeEmpty();
  }

  public static async Task Other_Client_Error_Should_Be_Unexpected_Status_And_Logged()
  {
    using LookupHarness harness = LookupHarness.Script(_ => Json(HttpStatusCode.BadRequest, """{"error":"temporarily_unavailable"}"""));

    EntraTenantLookupResult first = await harness.Resolver.ResolveAsync("contoso.com", CancellationToken.None);
    EntraTenantLookupResult second = await harness.Resolver.ResolveAsync("contoso.com", CancellationToken.None);

    first.Outcome.ShouldBe(EntraTenantLookupOutcome.UnexpectedStatus);
    first.StatusCode.ShouldBe(400);
    second.Outcome.ShouldBe(EntraTenantLookupOutcome.UnexpectedStatus);
    harness.Handler.Calls.ShouldBe(2);
    harness.Logger.Warnings.Count.ShouldBe(2);
    harness.Logger.Warnings[0].ShouldContain("unexpected HTTP status");
  }

  public static async Task Missing_Issuer_Should_Be_Unexpected_Issuer()
  {
    using LookupHarness harness = LookupHarness.Script(_ => Json(HttpStatusCode.OK, """{"authorization_endpoint":"https://example"}"""));

    EntraTenantLookupResult result = await harness.Resolver.ResolveAsync("contoso.com", CancellationToken.None);

    result.Outcome.ShouldBe(EntraTenantLookupOutcome.UnexpectedIssuer);
    result.IssuerFailure.ShouldBe(EntraIssuerParseFailure.Missing);
    harness.Logger.Warnings.ShouldContain(message => message.Contains("unexpected issuer", StringComparison.Ordinal));
  }

  public static async Task Malformed_Issuer_Should_Be_Logged()
  {
    using LookupHarness harness = LookupHarness.Script(_ => Json(HttpStatusCode.OK, """{"issuer":"https://login.microsoft.com/not-a-guid/v2.0"}"""));

    EntraTenantLookupResult result = await harness.Resolver.ResolveAsync("contoso.com", CancellationToken.None);

    result.Outcome.ShouldBe(EntraTenantLookupOutcome.UnexpectedIssuer);
    result.IssuerFailure.ShouldBe(EntraIssuerParseFailure.WrongHost);
    harness.Logger.Warnings.Count.ShouldBe(1);
  }

  public static async Task Non_Json_Success_Should_Be_Malformed_Issuer()
  {
    using LookupHarness harness = LookupHarness.Script(_ => Text(HttpStatusCode.OK, "not-json"));

    EntraTenantLookupResult result = await harness.Resolver.ResolveAsync("contoso.com", CancellationToken.None);

    result.Outcome.ShouldBe(EntraTenantLookupOutcome.UnexpectedIssuer);
    result.IssuerFailure.ShouldBe(EntraIssuerParseFailure.Malformed);
    harness.Logger.Warnings.Count.ShouldBe(1);
  }

  public static async Task Timeout_Should_Be_Distinct_Logged_And_Not_Cached()
  {
    using LookupHarness harness = LookupHarness.Script(
      async (_, cancellationToken) =>
      {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return Json(HttpStatusCode.OK, IssuerBody(Tenant));
      },
      TimeSpan.FromMilliseconds(30));

    EntraTenantLookupResult first = await harness.Resolver.ResolveAsync("contoso.com", CancellationToken.None);
    EntraTenantLookupResult second = await harness.Resolver.ResolveAsync("contoso.com", CancellationToken.None);

    first.Outcome.ShouldBe(EntraTenantLookupOutcome.Timeout);
    second.Outcome.ShouldBe(EntraTenantLookupOutcome.Timeout);
    harness.Handler.Calls.ShouldBe(2);
    harness.Logger.Warnings.ShouldAllBe(message => message.Contains("timed out", StringComparison.Ordinal));
  }

  public static async Task Network_Failure_Should_Be_Distinct_Logged_And_Not_Cached()
  {
    using LookupHarness harness = LookupHarness.Script(_ => throw new HttpRequestException("down"));

    EntraTenantLookupResult first = await harness.Resolver.ResolveAsync("contoso.com", CancellationToken.None);
    EntraTenantLookupResult second = await harness.Resolver.ResolveAsync("contoso.com", CancellationToken.None);

    first.Outcome.ShouldBe(EntraTenantLookupOutcome.NetworkFailure);
    second.Outcome.ShouldBe(EntraTenantLookupOutcome.NetworkFailure);
    harness.Handler.Calls.ShouldBe(2);
    harness.Logger.Warnings.ShouldAllBe(message => message.Contains("network request", StringComparison.Ordinal));
  }

  public static async Task Found_Result_Should_Be_Served_From_Cache()
  {
    using LookupHarness harness = LookupHarness.Script(_ => Json(HttpStatusCode.OK, IssuerBody(Tenant)));

    await harness.Resolver.ResolveAsync("contoso.com", CancellationToken.None);
    EntraTenantLookupResult second = await harness.Resolver.ResolveAsync("contoso.com", CancellationToken.None);

    second.TenantId.ShouldBe(Tenant);
    harness.Handler.Calls.ShouldBe(1);
  }

  public static async Task Invalid_Domain_Should_Not_Call_Microsoft()
  {
    using LookupHarness harness = LookupHarness.Script(_ => Json(HttpStatusCode.OK, IssuerBody(Tenant)));

    EntraTenantLookupResult result = await harness.Resolver.ResolveAsync("not a domain", CancellationToken.None);

    result.Outcome.ShouldBe(EntraTenantLookupOutcome.InvalidDomain);
    harness.Handler.Calls.ShouldBe(0);
  }

  public static async Task Caller_Cancellation_Should_Propagate()
  {
    using LookupHarness harness = LookupHarness.Script(
      async (_, cancellationToken) =>
      {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return Json(HttpStatusCode.OK, IssuerBody(Tenant));
      });
    using var cancelled = new CancellationTokenSource();
    await cancelled.CancelAsync();

    await Should.ThrowAsync<OperationCanceledException>(
      () => harness.Resolver.ResolveAsync("contoso.com", cancelled.Token));
  }

  private static string IssuerBody(Guid tenantId) =>
    $$"""{"issuer":"https://login.microsoftonline.com/{{tenantId:D}}/v2.0"}""";

  private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
    new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

  private static HttpResponseMessage Text(HttpStatusCode status, string body) =>
    new(status) { Content = new StringContent(body, Encoding.UTF8, "text/plain") };

  private sealed class LookupHarness : IDisposable
  {
    public ScriptedHandler Handler { get; }
    public CollectingLogger Logger { get; }
    public EntraTenantResolver Resolver { get; }
    private readonly MemoryCache Cache;

    private LookupHarness(ScriptedHandler handler, TimeSpan timeout)
    {
      Handler = handler;
      Logger = new CollectingLogger();
      Cache = new MemoryCache(new MemoryCacheOptions());
      Resolver = new EntraTenantResolver(new ScriptedFactory(handler), Cache, Logger, timeout, TimeSpan.FromMinutes(10));
    }

    public static LookupHarness Script(
      Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond,
      TimeSpan? timeout = null)
    {
      var handler = new ScriptedHandler(respond);
      return new LookupHarness(handler, timeout ?? TimeSpan.FromSeconds(2));
    }

    public static LookupHarness Script(
      Func<HttpRequestMessage, HttpResponseMessage> respond,
      TimeSpan? timeout = null) =>
      Script((request, _) => Task.FromResult(respond(request)), timeout);

    public void Dispose() => Cache.Dispose();
  }

  private sealed class ScriptedHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    : HttpMessageHandler
  {
    public int Calls { get; private set; }
    public Uri? LastUri { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      Calls++;
      LastUri = request.RequestUri;
      return respond(request, cancellationToken);
    }
  }

  private sealed class ScriptedFactory(HttpMessageHandler handler) : IHttpClientFactory
  {
    public HttpClient CreateClient(string name) =>
      new(handler, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan };
  }

  private sealed class CollectingLogger : ILogger<EntraTenantResolver>
  {
    public List<string> Warnings { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
      LogLevel logLevel,
      EventId eventId,
      TState state,
      Exception? exception,
      Func<TState, Exception?, string> formatter)
    {
      if (logLevel >= LogLevel.Warning)
      {
        Warnings.Add(formatter(state, exception));
      }
    }
  }
}
