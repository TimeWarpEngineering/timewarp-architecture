#region Purpose
// Falsifiable FastEndpoints + ASP.NET Core proof of when a non-default authentication scheme runs.
#endregion

#region Design
// Task 161: isolates FastEndpoints 8.3 Policies() vs AuthSchemes() against ASP.NET Core 10
// PolicyEvaluator.AuthenticateAsync. A full web-server HostGraph cannot strip AuthenticationSchemes
// off product contracts, so this boots a TestServer with four probe endpoints and two schemes:
// default "cookies" (unused by the probe-header requests) and named "probe" (succeeds on X-Probe).
// Proven (task 161):
// A. Policies-only + named policy AddAuthenticationSchemes("probe") — probe handler runs
//    (Combine copies policy schemes; api-server GetAgentBearerIdentity lives this way).
// B. Policies-only + named policy with no schemes — probe does not run, 401 (post-182
//    PermissionIds registration; PolicyEvaluator no-ops; only UseAuthentication's default
//    scheme ran).
// C. AuthSchemes("probe") + Policies(...) against a no-scheme policy — probe runs (current
//    web [EndpointAuthorize(AuthenticationSchemes)] emission).
// ProbeAuthenticationHandler increments a counter at the top of HandleAuthenticateAsync (same
// rigor as task 158's throw-in-handler instrumentation, durable). Authenticated probes return
// 204 NoContent (FE empty-response default) — tests assert IsSuccessStatusCode, not 200.
#endregion

namespace FastEndpointAuthSchemes_;

using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class ProbeScheme_Given_
{
  private const string ProbeHeader = "X-Probe";
  private const string ProbeScheme = "probe";
  private const string CookieScheme = "cookies";
  private const string PolicyWithSchemes = "with-schemes";
  private const string PolicyNoSchemes = "no-schemes";

  private static WebApplication? App;
  private static HttpClient? Client;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ProbeScheme_Given_>();

  public static async Task SetupOnce()
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(
      new WebApplicationOptions { EnvironmentName = Environments.Development });
    builder.WebHost.UseTestServer();
    builder.Logging.ClearProviders();

    builder.Services
      .AddAuthentication(CookieScheme)
      .AddScheme<AuthenticationSchemeOptions, CookieAuthenticationHandler>(CookieScheme, _ => { })
      .AddScheme<AuthenticationSchemeOptions, ProbeAuthenticationHandler>(ProbeScheme, _ => { });

    builder.Services.AddAuthorizationBuilder()
      .AddPolicy(
        PolicyWithSchemes,
        policy => policy.AddAuthenticationSchemes(ProbeScheme).RequireAuthenticatedUser())
      .AddPolicy(
        PolicyNoSchemes,
        policy => policy.RequireAuthenticatedUser());

    builder.Services.AddFastEndpoints(options =>
    {
      options.DisableAutoDiscovery = true;
      options.Assemblies = [typeof(PoliciesOnlyWithPolicySchemesEndpoint).Assembly];
      options.Filter = type => type.Namespace == typeof(PoliciesOnlyWithPolicySchemesEndpoint).Namespace
        && type.Name.EndsWith("Endpoint", StringComparison.Ordinal);
    });

    App = builder.Build();
    App.UseAuthentication();
    App.UseAuthorization();
    App.UseFastEndpoints(config => config.Endpoints.RoutePrefix = null);
    await App.StartAsync();
    Client = App.GetTestClient();
  }

  public static async Task CleanUpOnce()
  {
    Client?.Dispose();
    Client = null;
    if (App is not null)
    {
      await App.DisposeAsync();
      App = null;
    }
  }

  public static Task Setup()
  {
    ProbeAuthenticationHandler.Reset();
    return Task.CompletedTask;
  }

  public static async Task PoliciesOnly_And_PolicyListsProbe_Should_Invoke_Probe()
  {
    HttpResponseMessage response = await SendProbeAsync("/probe/policies-only-with-policy-schemes");

    ProbeAuthenticationHandler.InvokeCount.ShouldBeGreaterThan(
      0,
      "Named-policy AddAuthenticationSchemes must reach PolicyEvaluator via Combine when FastEndpoints emits Policies(...) only.");
    response.IsSuccessStatusCode.ShouldBeTrue(
      $"Authenticated probe must not 401/403 (got {(int)response.StatusCode} {response.StatusCode}).");
  }

  public static async Task PoliciesOnly_And_PolicyHasNoSchemes_Should_Not_Invoke_Probe()
  {
    HttpResponseMessage response = await SendProbeAsync("/probe/policies-only-no-policy-schemes");

    ProbeAuthenticationHandler.InvokeCount.ShouldBe(
      0,
      "A Policies-only endpoint against a policy with no AddAuthenticationSchemes must not authenticate a non-default scheme (post-182 PermissionIds shape).");
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task AuthSchemesPlusPolicies_And_PolicyHasNoSchemes_Should_Invoke_Probe()
  {
    HttpResponseMessage response = await SendProbeAsync("/probe/authschemes-no-policy-schemes");

    ProbeAuthenticationHandler.InvokeCount.ShouldBeGreaterThan(
      0,
      "FastEndpoints AuthSchemes(...) must invoke the named scheme even when the named policy lists none.");
    response.IsSuccessStatusCode.ShouldBeTrue(
      $"Authenticated probe must not 401/403 (got {(int)response.StatusCode} {response.StatusCode}).");
  }

  public static async Task AuthSchemesPlusPolicies_And_PolicyListsProbe_Should_Invoke_Probe()
  {
    HttpResponseMessage response = await SendProbeAsync("/probe/authschemes-with-policy-schemes");

    ProbeAuthenticationHandler.InvokeCount.ShouldBeGreaterThan(0);
    response.IsSuccessStatusCode.ShouldBeTrue(
      $"Authenticated probe must not 401/403 (got {(int)response.StatusCode} {response.StatusCode}).");
  }

  private static async Task<HttpResponseMessage> SendProbeAsync(string path)
  {
    Client.ShouldNotBeNull();
    using HttpRequestMessage request = new(HttpMethod.Get, path);
    request.Headers.Add(ProbeHeader, "1");
    return await Client.SendAsync(request);
  }
}

public sealed class CookieAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
  public CookieAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : base(options, logger, encoder)
  {
  }

  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    => Task.FromResult(AuthenticateResult.NoResult());
}

public sealed class ProbeAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
  private static int InvokeCounter;

  public static int InvokeCount => Volatile.Read(ref InvokeCounter);

  public static void Reset() => Volatile.Write(ref InvokeCounter, 0);

  public ProbeAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : base(options, logger, encoder)
  {
  }

  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    Interlocked.Increment(ref InvokeCounter);
    if (!Request.Headers.ContainsKey("X-Probe"))
    {
      return Task.FromResult(AuthenticateResult.NoResult());
    }

    ClaimsIdentity identity = new(Scheme.Name);
    identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "probe-user"));
    ClaimsPrincipal principal = new(identity);
    AuthenticationTicket ticket = new(principal, Scheme.Name);
    return Task.FromResult(AuthenticateResult.Success(ticket));
  }
}

public sealed class PoliciesOnlyWithPolicySchemesEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/probe/policies-only-with-policy-schemes");
    Policies("with-schemes");
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class PoliciesOnlyNoPolicySchemesEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/probe/policies-only-no-policy-schemes");
    Policies("no-schemes");
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class AuthSchemesNoPolicySchemesEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/probe/authschemes-no-policy-schemes");
    AuthSchemes("probe");
    Policies("no-schemes");
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}

public sealed class AuthSchemesWithPolicySchemesEndpoint : EndpointWithoutRequest
{
  public override void Configure()
  {
    Get("/probe/authschemes-with-policy-schemes");
    AuthSchemes("probe");
    Policies("with-schemes");
  }

  public override Task HandleAsync(CancellationToken ct)
  {
    _ = ct;
    return Task.CompletedTask;
  }
}
