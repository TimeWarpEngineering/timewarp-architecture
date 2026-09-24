#region Purpose
// Real OpenIdConnectHandler round-trip: signed RSA id_token from a stub OIDC authority.
#endregion

#region Design
// Isolated TestServer with AddNamedEntraScheme (real OpenIdConnectHandler, MapInboundClaims=false,
// identity-session DefaultScheme). Static Configuration skips live metadata; BackchannelHttpHandler
// serves the token endpoint. The authorization code carries the challenge nonce so the stub can
// mint an Entra v2-shaped id_token (iss/aud/tid/oid/sub/name/preferred_username/ver/nonce/exp/nbf/iat)
// without shared mutable nonce state. PKCE is enabled on the scheme; the stub does not verify the
// verifier. Correlation and nonce cookies from the challenge are replayed on GET /signin-oidc.
// This path reproduced Invalid Entra token / missing iss: OpenIdConnectOptions deletes iss
// after TokenValidated. ClaimActions.Remove("iss") plus copying SecurityToken.Issuer keeps
// the pin on the principal so TryRead succeeds.
#endregion

namespace EntraOidcHandlerRoundTrip_;

using System.Net;
using System.Security.Cryptography;
using System.Text;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TimeWarp.Architecture.Abstractions;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Architecture.Features.Identity.Infrastructure;
using TimeWarp.Architecture.Services;
using TimeWarp.Identity;

public class Callback_Given_
{
  private static readonly Guid TrustedTenantId = Guid.Parse("30f3971f-4719-4f20-9b6f-88916e0b95bd");
  private static readonly Guid ObjectId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
  private static readonly string ClientId = Guid.Parse("f6d55605-aed5-4b5e-8b54-6260de4b323a").ToString("D");
  private const string ClientSecret = "test-client-secret";
  private const string AuthorizationEndpoint = "https://stub.example.test/oauth2/v2.0/authorize";
  private const string TokenEndpoint = "https://stub.example.test/oauth2/v2.0/token";

  private static RSA? Rsa;
  private static RsaSecurityKey? SigningKey;
  private static WebApplication? App;
  private static HttpClient? Client;
  private static string? Issuer;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Callback_Given_>();

  public static async Task SetupOnce()
  {
    Issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(TrustedTenantId));
    Rsa = RSA.Create(2048);
    SigningKey = new RsaSecurityKey(Rsa) { KeyId = "stub-key" };
    StubOidcTokenHandler stubHandler = new(SigningKey, ClientId, Issuer, TrustedTenantId, ObjectId);

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
        ["Authentication:Entra:TenantId"] = TrustedTenantId.ToString("D"),
        ["Authentication:Entra:ClientId"] = ClientId,
        ["Authentication:Entra:ClientSecret"] = ClientSecret,
        ["Authentication:Entra:CallbackPath"] = "/signin-oidc",
        ["Authentication:Entra:AllowBootstrap"] = "true"
      }
    );

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddSingleton<IPrincipalStore, InMemoryPrincipalStore>();
    builder.Services.AddSingleton<IPrincipalRoleStore, InMemoryPrincipalRoleStore>();
    builder.Services.AddSingleton<ISiteSettingsStore, InMemorySiteSettingsStore>();
    builder.Services.AddScoped<IEntraSignInPolicy, SiteSettingsEntraSignInPolicy>();
    builder.Services.AddScoped<IBrowserSessionService, CookieBrowserSessionService>();
    builder.Services.AddSingleton<CredentialUsageRecorder>();
    builder.Services.AddScoped<EntraTicketProcessor>();
    builder.Services.AddSingleton<IParkedEntraClaimsStore, InMemoryParkedEntraClaimsStore>();
    builder.Services.AddScoped<IEntraChoiceTicketAccessor, HttpEntraChoiceTicketAccessor>();
    builder.Services.AddOptions<EntraAuthenticationOptions>()
      .Bind(builder.Configuration.GetSection(EntraAuthenticationOptions.SectionKey));
    builder.Services.PostConfigure<EntraAuthenticationOptions>(options => options.Enabled = true);

    AuthenticationBuilder authenticationBuilder = builder.Services
      .AddAuthentication(IdentitySessionDefaults.Scheme)
      .AddCookie
      (
        IdentitySessionDefaults.Scheme,
        options => options.Cookie.Name = IdentitySessionDefaults.CookieName
      );
    EntraAuthenticationRegistration.AddNamedEntraScheme(authenticationBuilder, builder.Configuration);
    builder.Services.Configure<OpenIdConnectOptions>
    (
      EntraLinkDefaults.Scheme,
      options =>
      {
        options.RequireHttpsMetadata = false;
        options.BackchannelHttpHandler = stubHandler;
        options.Configuration = new OpenIdConnectConfiguration
        {
          AuthorizationEndpoint = AuthorizationEndpoint,
          TokenEndpoint = TokenEndpoint,
          Issuer = Issuer
        };
        options.Configuration.SigningKeys.Add(SigningKey);
        options.TokenValidationParameters.IssuerSigningKey = SigningKey;
        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
        options.TokenValidationParameters.ValidAudience = ClientId;
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
    ISiteSettingsStore siteSettingsStore = App.Services.GetRequiredService<ISiteSettingsStore>();
    await siteSettingsStore.AddAsync(
      SiteSettings.Create(
        entraSignInEnabled: true,
        entraAllowBootstrap: true,
        passkeyPromptMode: PasskeyPromptMode.Soft));
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

    Rsa?.Dispose();
    Rsa = null;
    SigningKey = null;
  }

  public static async Task Signed_Id_Token_Through_Real_Handler_Should_Issue_Identity_Session()
  {
    Client.ShouldNotBeNull();
    using HttpRequestMessage challengeRequest = new(
      HttpMethod.Get,
      "/api/identity/entra/challenge?mode=bootstrap&returnUrl=%2FProfile");
    HttpResponseMessage challenge = await Client.SendAsync(challengeRequest);
    challenge.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    challenge.Headers.Location.ShouldNotBeNull();
    Uri location = challenge.Headers.Location!;
    location.GetLeftPart(UriPartial.Path).ShouldBe(AuthorizationEndpoint);

    Dictionary<string, StringValues> query = QueryHelpers.ParseQuery(location.Query);
    query.TryGetValue("state", out StringValues stateValues).ShouldBeTrue();
    query.TryGetValue("nonce", out StringValues nonceValues).ShouldBeTrue();
    query.TryGetValue("code_challenge", out StringValues codeChallengeValues).ShouldBeTrue();
    codeChallengeValues.ToString().ShouldNotBeNullOrWhiteSpace();
    string state = stateValues.ToString();
    string nonce = nonceValues.ToString();
    state.ShouldNotBeNullOrWhiteSpace();
    nonce.ShouldNotBeNullOrWhiteSpace();

    string cookieHeader = CookieHeaderFrom(challenge);
    cookieHeader.ShouldContain(".AspNetCore.Correlation", Case.Sensitive);
    cookieHeader.ShouldContain(".AspNetCore.OpenIdConnect.Nonce", Case.Sensitive);

    using HttpRequestMessage callbackRequest = new(
      HttpMethod.Get,
      $"/signin-oidc?code={Uri.EscapeDataString(nonce)}&state={Uri.EscapeDataString(state)}");
    callbackRequest.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
    HttpResponseMessage callback = await Client.SendAsync(callbackRequest);
    string body = await callback.Content.ReadAsStringAsync();
    callback.StatusCode.ShouldBe(
      HttpStatusCode.Redirect,
      $"Expected choose-page redirect after real OIDC ticket. Body: {body}");
    callback.Headers.Location.ShouldNotBeNull();
    callback.Headers.Location!.ToString().ShouldStartWith(EntraChoiceCookie.ChoosePath);
    callback.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues).ShouldBeTrue();
    setCookieValues.ShouldNotBeNull();
    setCookieValues.ShouldContain(
      value => value.Contains(EntraChoiceCookie.CookieName, StringComparison.Ordinal));
  }

  private static string CookieHeaderFrom(HttpResponseMessage response)
  {
    response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues).ShouldBeTrue();
    setCookieValues.ShouldNotBeNull();
    return string.Join(
      "; ",
      setCookieValues.Select(setCookie => setCookie.Split(';', 2)[0]));
  }

  private sealed class StubOidcTokenHandler : HttpMessageHandler
  {
    private readonly RsaSecurityKey SigningKeyValue;
    private readonly string Audience;
    private readonly string TokenIssuer;
    private readonly Guid TenantId;
    private readonly Guid TokenObjectId;

    public StubOidcTokenHandler
    (
      RsaSecurityKey signingKey,
      string audience,
      string tokenIssuer,
      Guid tenantId,
      Guid tokenObjectId
    )
    {
      SigningKeyValue = signingKey;
      Audience = audience;
      TokenIssuer = tokenIssuer;
      TenantId = tenantId;
      TokenObjectId = tokenObjectId;
    }

    protected override async Task<HttpResponseMessage> SendAsync
    (
      HttpRequestMessage request,
      CancellationToken cancellationToken
    )
    {
      if (request.RequestUri is null
        || !string.Equals(request.RequestUri.AbsoluteUri, TokenEndpoint, StringComparison.Ordinal)
        || request.Method != HttpMethod.Post
        || request.Content is null)
      {
        return new HttpResponseMessage(HttpStatusCode.NotFound);
      }

      string form = await request.Content.ReadAsStringAsync(cancellationToken);
      Dictionary<string, StringValues> fields = QueryHelpers.ParseQuery(form);
      string nonce = fields.TryGetValue("code", out StringValues codeValues)
        ? codeValues.ToString()
        : "";
      if (string.IsNullOrWhiteSpace(nonce))
      {
        return new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
          Content = new StringContent("""{"error":"invalid_grant"}""", Encoding.UTF8, "application/json")
        };
      }

      string idToken = CreateIdToken(nonce);
      string json =
        $"{{\"token_type\":\"Bearer\",\"expires_in\":3600,\"access_token\":\"not-used\",\"id_token\":\"{idToken}\"}}";
      return new HttpResponseMessage(HttpStatusCode.OK)
      {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
      };
    }

    private string CreateIdToken(string nonce)
    {
      DateTime utcNow = DateTime.UtcNow;
      JsonWebTokenHandler jsonWebTokenHandler = new() { MapInboundClaims = false };
      SecurityTokenDescriptor descriptor = new()
      {
        Issuer = TokenIssuer,
        Audience = Audience,
        SigningCredentials = new SigningCredentials(SigningKeyValue, SecurityAlgorithms.RsaSha256),
        Claims = new Dictionary<string, object>
        {
          ["tid"] = TenantId.ToString("D"),
          ["oid"] = TokenObjectId.ToString("D"),
          ["sub"] = TokenObjectId.ToString("D"),
          ["name"] = "Test User",
          ["preferred_username"] = "user@example.com",
          ["ver"] = "2.0",
          ["nonce"] = nonce
        },
        NotBefore = utcNow.AddMinutes(-1),
        IssuedAt = utcNow,
        Expires = utcNow.AddMinutes(10)
      };
      return jsonWebTokenHandler.CreateToken(descriptor);
    }
  }
}
