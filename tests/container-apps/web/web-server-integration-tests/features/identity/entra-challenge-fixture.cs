#region Purpose
// Shared host builder and request helpers for the Entra challenge test classes. Builds an isolated
// TestServer with FakeEntraHandler so each class owns its host and the arrange code lives once.
#endregion

#region Design
// Isolated TestServer (not the full web-server HostGraph) with FakeEntraHandler standing in for
// OpenIdConnect. Challenge immediately completes the ticket via EntraTicketHttp using tid/oid/iss
// from test headers. CredentialUsageRecorder is registered as the singleton the real host's
// InMemoryIdentityStoresModule provides, so a test can assert the last-used stamp end-to-end
// (task 252). The settings row is Restricted and lists TrustedTenantId; options.TenantId is only
// the OIDC authority. Each test class calls StartAsync in its SetupOnce and owns that host.
#endregion

namespace EntraChallenge_;

using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Abstractions;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Architecture.Features.Identity.Infrastructure;
using TimeWarp.Architecture.Services;
using TimeWarp.Foundation.Types;
using TimeWarp.Identity;

internal static class EntraChallengeFixture
{
  public const string TenantIdHeader = FakeEntraHeaders.TenantId;
  public const string ObjectIdHeader = FakeEntraHeaders.ObjectId;

  public static readonly Guid TrustedTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
  public static readonly Guid UntrustedTenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

  public static async Task<WebApplication> StartAsync()
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(
      new WebApplicationOptions { EnvironmentName = Environments.Development });
    builder.WebHost.UseTestServer();
    builder.Logging.ClearProviders();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddLogging();
    builder.Services.AddSingleton<IPrincipalStore, InMemoryPrincipalStore>();
    builder.Services.AddSingleton<IPrincipalRoleStore, InMemoryPrincipalRoleStore>();
    builder.Services.AddSingleton<ISiteSettingsStore, InMemorySiteSettingsStore>();
    builder.Services.AddScoped<IEntraSignInPolicy, SiteSettingsEntraSignInPolicy>();
    builder.Services.AddScoped<IBrowserSessionService, CookieBrowserSessionService>();
    builder.Services.AddSingleton<CredentialUsageRecorder>();
    builder.Services.AddScoped<EntraTicketProcessor>();
    builder.Services.AddSingleton<IParkedEntraClaimsStore, InMemoryParkedEntraClaimsStore>();
    builder.Services.AddScoped<IEntraChoiceTicketAccessor, HttpEntraChoiceTicketAccessor>();
    builder.Services.AddScoped<TimeWarp.Architecture.Features.Identity.Application.CompleteEntraBootstrapCreate.Handler>();
    builder.Services.Configure<EntraAuthenticationOptions>(options =>
    {
      options.AllowBootstrap = true;
      options.TenantId = TrustedTenantId.ToString("D");
    });
    builder.Services.AddSingleton<IOptions<TimeWarp.Architecture.Features.Identity.Application.SignInOptions>>(
      Options.Create(new TimeWarp.Architecture.Features.Identity.Application.SignInOptions
      {
        Methods =
        [
          TimeWarp.Architecture.Features.Identity.SignInMethod.Passkey,
          TimeWarp.Architecture.Features.Identity.SignInMethod.Microsoft365
        ]
      }));

    builder.Services
      .AddAuthentication(IdentitySessionDefaults.Scheme)
      .AddCookie
      (
        IdentitySessionDefaults.Scheme,
        options =>
        {
          options.Cookie.Name = IdentitySessionDefaults.CookieName;
          options.Events.OnRedirectToLogin = context =>
          {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
          };
        }
      )
      .AddScheme<AuthenticationSchemeOptions, FakeEntraHandler>(EntraLinkDefaults.Scheme, _ => { });

    builder.Services.AddAuthorization();
    builder.Services.AddFastEndpoints(options =>
    {
      options.DisableAutoDiscovery = true;
      options.Assemblies = [typeof(ChallengeEntraEndpoint).Assembly];
      options.Filter = type => type == typeof(ChallengeEntraEndpoint);
    });

    WebApplication app = builder.Build();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseFastEndpoints(config =>
    {
      config.Endpoints.RoutePrefix = null;
      config.Endpoints.AllowEmptyRequestDtos = true;
    });
    app.MapPost
    (
      "/test/signin/{principalId:guid}",
      async (Guid principalId, IBrowserSessionService sessions) =>
      {
        await sessions.IssueAsync(PrincipalId.From(principalId), "test", CancellationToken.None);
        return Results.NoContent();
      }
    );
    app.MapPost
    (
      "/api/identity/entra/choice/create",
      async (TimeWarp.Architecture.Features.Identity.Application.CompleteEntraBootstrapCreate.Handler handler, HttpContext httpContext) =>
      {
        OneOf<TimeWarp.Architecture.Features.Identity.CompleteEntraBootstrapCreate.Response, SharedProblemDetails> result =
          await handler.Handle(new TimeWarp.Architecture.Features.Identity.CompleteEntraBootstrapCreate.Command(), httpContext.RequestAborted);
        if (result.IsT1)
        {
          httpContext.Response.StatusCode = result.AsT1.Status ?? StatusCodes.Status400BadRequest;
          await httpContext.Response.WriteAsJsonAsync(
            result.AsT1,
            ContractSerializationDefaults.Options,
            httpContext.RequestAborted);
          return;
        }

        await httpContext.Response.WriteAsJsonAsync(
          result.AsT0,
          ContractSerializationDefaults.Options,
          httpContext.RequestAborted);
      }
    );

    await app.StartAsync();
    ISiteSettingsStore siteSettingsStore = app.Services.GetRequiredService<ISiteSettingsStore>();
    await siteSettingsStore.AddAsync(
      SiteSettings.Create(
        entraSignInEnabled: true,
        entraAllowBootstrap: true,
        passkeyPromptMode: PasskeyPromptMode.Soft,
        entraAcceptedTenants: [EntraAcceptedTenant.IdOnly(TrustedTenantId)]));

    return app;
  }

  public static async Task<HttpResponseMessage> SendChallengeAsync
  (
    HttpClient? httpClient,
    string mode,
    Guid tenantId,
    Guid objectId,
    string? cookie = null,
    string? preferredUsername = null,
    string? name = null
  )
  {
    httpClient.ShouldNotBeNull();
    using HttpRequestMessage request = new(
      HttpMethod.Get,
      $"/api/identity/entra/challenge?mode={mode}&returnUrl=%2F");
    request.Headers.TryAddWithoutValidation(TenantIdHeader, tenantId.ToString("D"));
    request.Headers.TryAddWithoutValidation(ObjectIdHeader, objectId.ToString("D"));
    if (!string.IsNullOrEmpty(cookie))
    {
      request.Headers.TryAddWithoutValidation("Cookie", cookie);
    }

    if (!string.IsNullOrEmpty(preferredUsername))
    {
      request.Headers.TryAddWithoutValidation(FakeEntraHeaders.PreferredUsername, preferredUsername);
    }

    if (!string.IsNullOrEmpty(name))
    {
      request.Headers.TryAddWithoutValidation(FakeEntraHeaders.Name, name);
    }

    return await httpClient.SendAsync(request);
  }

  public static async Task<string> SignInAsync(HttpClient? httpClient, PrincipalId principalId)
  {
    httpClient.ShouldNotBeNull();
    HttpResponseMessage response = await httpClient.PostAsync($"/test/signin/{principalId.Value:D}", content: null);
    response.EnsureSuccessStatusCode();
    response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues).ShouldBeTrue();
    string setCookie = setCookieValues.First(value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));
    return setCookie.Split(';', 2)[0];
  }

  public static async Task<SharedProblemDetails> ReadProblemAsync(HttpResponseMessage response)
  {
    string json = await response.Content.ReadAsStringAsync();
    SharedProblemDetails? problem = System.Text.Json.JsonSerializer.Deserialize<SharedProblemDetails>(
      json,
      ContractSerializationDefaults.Options);
    problem.ShouldNotBeNull();
    return problem;
  }
}
