#region Purpose
// Proves the named entra OIDC challenge emits PublicOrigin redirect_uri when set, else request-derived.
#endregion

#region Design
// Isolated TestServer with the real OpenIdConnectHandler (not FakeEntraHandler). Static OIDC
// metadata avoids a live tenant. Auto-redirect is off so the 302 Location is the assertion.
// PublicOrigin is mutated on the bound options object per test — the redirect event reads IOptions
// at request time. Correlation/nonce Set-Cookie must carry Secure on this http TestServer because
// SecurePolicy is Always.
#endregion

namespace EntraPublicOrigin_;

using System.Net;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Architecture.Services;

public class Challenge_Given_
{
  private const string PublicOrigin = "https://arch.timewarp.work";
  private const string AuthorizationEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";

  private static WebApplication? App;
  private static HttpClient? Client;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Challenge_Given_>();

  public static async Task SetupOnce()
  {
    Guid tenantId = Guid.Parse("30f3971f-4719-4f20-9b6f-88916e0b95bd");
    WebApplicationBuilder builder = WebApplication.CreateBuilder(
      new WebApplicationOptions { EnvironmentName = Environments.Development });
    builder.WebHost.UseTestServer();
    builder.Logging.ClearProviders();
    builder.Configuration.AddInMemoryCollection
    (
      new Dictionary<string, string?>
      {
        [MockAuthenticationDefaults.EntraEnabledKey] = "true",
        ["Authentication:Entra:Instance"] = "https://login.microsoftonline.com/",
        ["Authentication:Entra:TenantId"] = tenantId.ToString("D"),
        ["Authentication:Entra:ClientId"] = Guid.NewGuid().ToString("D"),
        ["Authentication:Entra:CallbackPath"] = "/signin-oidc",
        ["Authentication:Entra:TrustedTenants:0"] = tenantId.ToString("D"),
        ["Authentication:Entra:AllowBootstrap"] = "true"
      }
    );

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddOptions<EntraAuthenticationOptions>()
      .Bind(builder.Configuration.GetSection(EntraAuthenticationOptions.SectionKey));
    builder.Services.PostConfigure<EntraAuthenticationOptions>(options => options.Enabled = true);

    AuthenticationBuilder authenticationBuilder = builder.Services
      .AddAuthentication(IdentitySessionDefaults.Scheme)
      .AddCookie(IdentitySessionDefaults.Scheme);
    EntraAuthenticationRegistration.AddNamedEntraScheme(authenticationBuilder, builder.Configuration);
    builder.Services.Configure<OpenIdConnectOptions>
    (
      EntraLinkDefaults.Scheme,
      options =>
      {
        options.Configuration = new OpenIdConnectConfiguration
        {
          AuthorizationEndpoint = AuthorizationEndpoint,
          TokenEndpoint = "https://login.microsoftonline.com/common/oauth2/v2.0/token",
          Issuer = "https://login.microsoftonline.com/common/v2.0"
        };
      }
    );

    builder.Services.AddAuthorization();
    builder.Services.AddFastEndpoints(options =>
    {
      options.DisableAutoDiscovery = true;
      options.Assemblies = [typeof(ChallengeEntraEndpoint).Assembly];
      options.Filter = type => type == typeof(ChallengeEntraEndpoint);
    });

    App = builder.Build();
    App.UseAuthentication();
    App.UseAuthorization();
    App.UseFastEndpoints(config =>
    {
      config.Endpoints.RoutePrefix = null;
      config.Endpoints.AllowEmptyRequestDtos = true;
    });

    await App.StartAsync();
    TestServer testServer = App.GetTestServer();
    Client = new HttpClient(testServer.CreateHandler())
    {
      BaseAddress = testServer.BaseAddress
    };
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

  public static async Task Unset_Public_Origin_Should_Use_Request_Derived_Redirect_Uri()
  {
    HttpResponseMessage response = await ChallengeBootstrapAsync();
    response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    response.Headers.Location.ShouldNotBeNull();
    ReadRedirectUri(response.Headers.Location!).ShouldBe("http://localhost/signin-oidc");
    AssertSecureOidcCookies(response);
  }

  public static async Task Set_Public_Origin_Should_Override_Redirect_Uri()
  {
    App.ShouldNotBeNull();
    EntraAuthenticationOptions options = App.Services.GetRequiredService<IOptions<EntraAuthenticationOptions>>().Value;
    string? previous = options.PublicOrigin;
    options.PublicOrigin = PublicOrigin;
    try
    {
      HttpResponseMessage response = await ChallengeBootstrapAsync();
      response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
      response.Headers.Location.ShouldNotBeNull();
      response.Headers.Location!.GetLeftPart(UriPartial.Path).ShouldBe(AuthorizationEndpoint);
      ReadRedirectUri(response.Headers.Location).ShouldBe($"{PublicOrigin}/signin-oidc");
      AssertSecureOidcCookies(response);
    }
    finally
    {
      options.PublicOrigin = previous;
    }
  }

  private static async Task<HttpResponseMessage> ChallengeBootstrapAsync()
  {
    Client.ShouldNotBeNull();
    using HttpRequestMessage request = new(
      HttpMethod.Get,
      "/api/identity/entra/challenge?mode=bootstrap&returnUrl=%2FProfile");
    return await Client.SendAsync(request);
  }

  private static string ReadRedirectUri(Uri location)
  {
    Dictionary<string, Microsoft.Extensions.Primitives.StringValues> query = QueryHelpers.ParseQuery(location.Query);
    query.TryGetValue("redirect_uri", out Microsoft.Extensions.Primitives.StringValues values).ShouldBeTrue();
    return values.ToString();
  }

  private static void AssertSecureOidcCookies(HttpResponseMessage response)
  {
    response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues).ShouldBeTrue();
    setCookieValues.ShouldNotBeNull();
    setCookieValues.ShouldContain
    (
      value => value.Contains(".AspNetCore.Correlation", StringComparison.Ordinal)
        && value.Contains("secure", StringComparison.OrdinalIgnoreCase)
    );
    setCookieValues.ShouldContain
    (
      value => value.Contains(".AspNetCore.OpenIdConnect.Nonce", StringComparison.Ordinal)
        && value.Contains("secure", StringComparison.OrdinalIgnoreCase)
    );
  }
}
