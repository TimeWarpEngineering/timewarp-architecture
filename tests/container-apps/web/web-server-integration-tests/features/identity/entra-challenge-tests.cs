#region Purpose
// Integration tests for Entra challenge/link/bootstrap/sync-hit without a live tenant.
#endregion

#region Design
// Isolated TestServer (not the full web-server HostGraph) with FakeEntraHandler standing in for
// OpenIdConnect. Challenge immediately completes the ticket via EntraTicketHttp using tid/oid/iss
// from test headers. Unknown-handle bootstrap parks claims and 302s to /Login/Microsoft365/Choose
// (no principal). Create is exercised via the real CompleteEntraBootstrapCreate handler mapped on
// this host. Link of a foreign active handle merges; already-on-this-account is 409.
#endregion

namespace EntraChallenge_;

using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
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

internal static class FakeEntraHeaders
{
  public const string TenantId = "X-Test-Entra-Tid";
  public const string ObjectId = "X-Test-Entra-Oid";
  public const string Issuer = "X-Test-Entra-Iss";
  public const string OmitObjectId = "X-Test-Entra-Omit-Oid";
  public const string PreferredUsername = "X-Test-Entra-Preferred-Username";
  public const string Name = "X-Test-Entra-Name";
}

public class Challenge_Given_
{
  private const string TenantIdHeader = FakeEntraHeaders.TenantId;
  private const string ObjectIdHeader = FakeEntraHeaders.ObjectId;

  private static readonly Guid TrustedTenantId = Guid.Parse("30f3971f-4719-4f20-9b6f-88916e0b95bd");
  private static readonly Guid UntrustedTenantId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

  private static WebApplication? App;
  private static HttpClient? Client;
  private static IPrincipalStore? Store;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Challenge_Given_>();

  public static async Task SetupOnce()
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
    builder.Services.AddScoped<EntraTicketProcessor>();
    builder.Services.AddSingleton<IParkedEntraClaimsStore, InMemoryParkedEntraClaimsStore>();
    builder.Services.AddScoped<IEntraChoiceTicketAccessor, HttpEntraChoiceTicketAccessor>();
    builder.Services.AddScoped<TimeWarp.Architecture.Features.Identity.Application.CompleteEntraBootstrapCreate.Handler>();
    builder.Services.Configure<EntraAuthenticationOptions>(options =>
    {
      options.Enabled = true;
      options.AllowBootstrap = true;
      options.TenantId = TrustedTenantId.ToString("D");
    });

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

    App = builder.Build();
    App.UseAuthentication();
    App.UseAuthorization();
    App.UseFastEndpoints(config =>
    {
      config.Endpoints.RoutePrefix = null;
      config.Endpoints.AllowEmptyRequestDtos = true;
    });
    App.MapPost
    (
      "/test/signin/{principalId:guid}",
      async (Guid principalId, IBrowserSessionService sessions) =>
      {
        await sessions.IssueAsync(PrincipalId.From(principalId), "test", CancellationToken.None);
        return Results.NoContent();
      }
    );
    App.MapPost
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

    await App.StartAsync();
    Client = App.GetTestClient();
    Store = App.Services.GetRequiredService<IPrincipalStore>();
    ISiteSettingsStore siteSettingsStore = App.Services.GetRequiredService<ISiteSettingsStore>();
    await siteSettingsStore.AddAsync(
      SiteSettings.Create(
        entraSignInEnabled: true,
        entraAllowBootstrap: true,
        passkeyPromptMode: PasskeyPromptMode.Soft));
  }

  public static async Task CleanUpOnce()
  {
    Client?.Dispose();
    Client = null;
    Store = null;
    if (App is not null)
    {
      await App.DisposeAsync();
      App = null;
    }
  }

  public static async Task Link_Without_Session_Should_401()
  {
    HttpResponseMessage response = await SendChallengeAsync("link", TrustedTenantId, Guid.NewGuid());
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task Bootstrap_Untrusted_Tenant_Should_403()
  {
    HttpResponseMessage response = await SendChallengeAsync("bootstrap", UntrustedTenantId, Guid.NewGuid());
    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    SharedProblemDetails problem = await ReadProblemAsync(response);
    problem.Title.ShouldBe("Untrusted tenant");
  }

  public static async Task Link_Untrusted_Tenant_Should_403()
  {
    Store.ShouldNotBeNull();
    Principal caller = Principal.Create(PrincipalKind.Human);
    await Store.AddPrincipalAsync(caller);
    string cookie = await SignInAsync(caller.Id);

    HttpResponseMessage response = await SendChallengeAsync("link", UntrustedTenantId, Guid.NewGuid(), cookie);
    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    SharedProblemDetails problem = await ReadProblemAsync(response);
    problem.Title.ShouldBe("Untrusted tenant");
  }

  public static async Task Link_Foreign_Active_Handle_Should_Merge()
  {
    Store.ShouldNotBeNull();
    Guid objectId = Guid.NewGuid();
    Principal owner = Principal.Create(PrincipalKind.Human);
    owner.SetDisplayName("Owner");
    await Store.AddPrincipalAsync(owner);
    Credential entra = Credential.Create(
      owner.Id,
      CredentialType.EntraAccount,
      EntraAccountHandle.Encode(TrustedTenantId, objectId),
      EntraIssuerMaterial.FromTenantId(TrustedTenantId),
      "Microsoft 365");
    await Store.AddCredentialAsync(entra);

    Principal caller = Principal.Create(PrincipalKind.Human);
    caller.SetDisplayName("Caller");
    await Store.AddPrincipalAsync(caller);
    string cookie = await SignInAsync(caller.Id);

    HttpResponseMessage response = await SendChallengeAsync("link", TrustedTenantId, objectId, cookie);
    response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    Credential? found = await Store.FindCredentialByHandleAsync(
      CredentialType.EntraAccount,
      EntraAccountHandle.Encode(TrustedTenantId, objectId));
    found.ShouldNotBeNull();
    found!.PrincipalId.ShouldBe(caller.Id);
    Principal? retired = await Store.GetPrincipalAsync(owner.Id);
    retired.ShouldNotBeNull();
    retired.IsActive.ShouldBeFalse();
    retired.MergedIntoPrincipalId.ShouldBe(caller.Id);
  }

  public static async Task Link_Already_On_This_Account_Should_409()
  {
    Store.ShouldNotBeNull();
    Guid objectId = Guid.NewGuid();
    Principal caller = Principal.Create(PrincipalKind.Human);
    await Store.AddPrincipalAsync(caller);
    await Store.AddCredentialAsync(
      Credential.Create(
        caller.Id,
        CredentialType.EntraAccount,
        EntraAccountHandle.Encode(TrustedTenantId, objectId),
        EntraIssuerMaterial.FromTenantId(TrustedTenantId),
        "Microsoft 365"));
    string cookie = await SignInAsync(caller.Id);

    HttpResponseMessage response = await SendChallengeAsync("link", TrustedTenantId, objectId, cookie);
    response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    SharedProblemDetails problem = await ReadProblemAsync(response);
    problem.Title.ShouldBe("Already on this account");
  }

  public static async Task Bootstrap_Unknown_Handle_Should_Redirect_To_Choose_Without_Creating_Principal()
  {
    Store.ShouldNotBeNull();
    int principalCount = (await Store.ListPrincipalsAsync()).Count;
    HttpResponseMessage response = await SendChallengeAsync("bootstrap", TrustedTenantId, Guid.NewGuid());
    response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    response.Headers.Location.ShouldNotBeNull();
    response.Headers.Location!.ToString().ShouldStartWith(EntraChoiceCookie.ChoosePath);
    (await Store.ListPrincipalsAsync()).Count.ShouldBe(principalCount);
    response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues).ShouldBeTrue();
    setCookieValues.ShouldNotBeNull();
    setCookieValues.ShouldContain(value => value.Contains(EntraChoiceCookie.CookieName, StringComparison.Ordinal));
    setCookieValues.ShouldNotContain(value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal)
      && !value.Contains("expires=", StringComparison.OrdinalIgnoreCase));
  }

  public static async Task Bootstrap_Choose_Create_Should_Mint_Principal_Credential_And_Session()
  {
    Store.ShouldNotBeNull();
    Client.ShouldNotBeNull();
    int principalCount = (await Store.ListPrincipalsAsync()).Count;
    Guid objectId = Guid.NewGuid();
    HttpResponseMessage choose = await SendChallengeAsync("bootstrap", TrustedTenantId, objectId);
    choose.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    choose.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues).ShouldBeTrue();
    string choiceCookie = setCookieValues!
      .First(value => value.Contains(EntraChoiceCookie.CookieName, StringComparison.Ordinal))
      .Split(';', 2)[0];

    using HttpRequestMessage createRequest = new(HttpMethod.Post, "/api/identity/entra/choice/create");
    createRequest.Headers.TryAddWithoutValidation("Cookie", choiceCookie);
    createRequest.Content = new StringContent("{}", Encoding.UTF8, "application/json");
    HttpResponseMessage create = await Client.SendAsync(createRequest);
    create.StatusCode.ShouldBe(HttpStatusCode.OK);
    (await Store.ListPrincipalsAsync()).Count.ShouldBe(principalCount + 1);
    Credential? found = await Store.FindCredentialByHandleAsync(
      CredentialType.EntraAccount,
      EntraAccountHandle.Encode(TrustedTenantId, objectId));
    found.ShouldNotBeNull();
    found!.IsRevoked.ShouldBeFalse();
    create.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? sessionCookies).ShouldBeTrue();
    sessionCookies.ShouldContain(value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));
  }

  public static async Task Bootstrap_Sync_Hit_Should_Reuse_Existing_Principal()
  {
    Store.ShouldNotBeNull();
    Guid objectId = Guid.NewGuid();
    Principal existing = Principal.Create(PrincipalKind.Human);
    await Store.AddPrincipalAsync(existing);
    await Store.AddCredentialAsync(
      Credential.Create(
        existing.Id,
        CredentialType.EntraAccount,
        EntraAccountHandle.Encode(TrustedTenantId, objectId),
        EntraIssuerMaterial.FromTenantId(TrustedTenantId),
        "Microsoft 365"));
    int principalCount = (await Store.ListPrincipalsAsync()).Count;

    HttpResponseMessage response = await SendChallengeAsync("bootstrap", TrustedTenantId, objectId);
    response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    (await Store.ListPrincipalsAsync()).Count.ShouldBe(principalCount);
    Credential? found = await Store.FindCredentialByHandleAsync(
      CredentialType.EntraAccount,
      EntraAccountHandle.Encode(TrustedTenantId, objectId));
    found.ShouldNotBeNull();
    found!.PrincipalId.ShouldBe(existing.Id);
  }

  public static async Task Link_With_Session_Should_Attach_And_Redirect()
  {
    Store.ShouldNotBeNull();
    Guid objectId = Guid.NewGuid();
    Principal caller = Principal.Create(PrincipalKind.Human);
    await Store.AddPrincipalAsync(caller);
    string cookie = await SignInAsync(caller.Id);

    HttpResponseMessage response = await SendChallengeAsync("link", TrustedTenantId, objectId, cookie);
    response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    Credential? found = await Store.FindCredentialByHandleAsync(
      CredentialType.EntraAccount,
      EntraAccountHandle.Encode(TrustedTenantId, objectId));
    found.ShouldNotBeNull();
    found!.PrincipalId.ShouldBe(caller.Id);
    found.IsRevoked.ShouldBeFalse();
    found.Label.ShouldBe(EntraIdTokenClaims.ProviderLabel);
    found.AccountHint.ShouldBeNull("the name claim names the person, not the account (task 250)");
  }

  public static async Task Link_Should_Store_Preferred_Username_As_Account_Hint()
  {
    Store.ShouldNotBeNull();
    Guid objectId = Guid.NewGuid();
    Principal caller = Principal.Create(PrincipalKind.Human);
    await Store.AddPrincipalAsync(caller);
    string cookie = await SignInAsync(caller.Id);

    HttpResponseMessage response = await SendChallengeAsync(
      "link",
      TrustedTenantId,
      objectId,
      cookie,
      preferredUsername: "Steven.Cramer@TimeWarp.Enterprises");
    response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    Credential? found = await Store.FindCredentialByHandleAsync(
      CredentialType.EntraAccount,
      EntraAccountHandle.Encode(TrustedTenantId, objectId));
    found.ShouldNotBeNull();
    found!.Label.ShouldBe(EntraIdTokenClaims.ProviderLabel);
    found.AccountHint.ShouldBe("Steven.Cramer@TimeWarp.Enterprises");
  }

  public static async Task Link_Second_Active_Entra_Should_409()
  {
    Store.ShouldNotBeNull();
    Principal caller = Principal.Create(PrincipalKind.Human);
    await Store.AddPrincipalAsync(caller);
    Guid firstObjectId = Guid.NewGuid();
    await Store.AddCredentialAsync(
      Credential.Create(
        caller.Id,
        CredentialType.EntraAccount,
        EntraAccountHandle.Encode(TrustedTenantId, firstObjectId),
        EntraIssuerMaterial.FromTenantId(TrustedTenantId),
        "Steven.Cramer@TimeWarp.Enterprises"));
    string cookie = await SignInAsync(caller.Id);

    HttpResponseMessage response = await SendChallengeAsync("link", TrustedTenantId, Guid.NewGuid(), cookie);
    response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    SharedProblemDetails problem = await ReadProblemAsync(response);
    problem.Title.ShouldBe("Microsoft 365 already linked");
  }

  public static async Task Bootstrap_When_Not_Allowed_Should_403()
  {
    App.ShouldNotBeNull();
    ISiteSettingsStore siteSettingsStore = App.Services.GetRequiredService<ISiteSettingsStore>();
    SiteSettings? current = await siteSettingsStore.GetAsync();
    current.ShouldNotBeNull();
    bool previous = current!.EntraAllowBootstrap;
    current.ReplacePolicy(current.EntraSignInEnabled, false, current.PasskeyPromptMode);
    await siteSettingsStore.UpdateAsync(current);
    try
    {
      HttpResponseMessage response = await SendChallengeAsync("bootstrap", TrustedTenantId, Guid.NewGuid());
      response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      SharedProblemDetails problem = await ReadProblemAsync(response);
      problem.Title.ShouldBe("Bootstrap not allowed");
    }
    finally
    {
      SiteSettings? restore = await siteSettingsStore.GetAsync();
      restore.ShouldNotBeNull();
      restore!.ReplacePolicy(restore.EntraSignInEnabled, previous, restore.PasskeyPromptMode);
      await siteSettingsStore.UpdateAsync(restore);
    }
  }

  public static async Task Challenge_When_Sign_In_Disabled_Should_403()
  {
    App.ShouldNotBeNull();
    ISiteSettingsStore siteSettingsStore = App.Services.GetRequiredService<ISiteSettingsStore>();
    SiteSettings? current = await siteSettingsStore.GetAsync();
    current.ShouldNotBeNull();
    bool previous = current!.EntraSignInEnabled;
    current.ReplacePolicy(false, current.EntraAllowBootstrap, current.PasskeyPromptMode);
    await siteSettingsStore.UpdateAsync(current);
    try
    {
      HttpResponseMessage response = await SendChallengeAsync("bootstrap", TrustedTenantId, Guid.NewGuid());
      response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
      SharedProblemDetails problem = await ReadProblemAsync(response);
      problem.Title.ShouldBe("Sign-in disabled");
    }
    finally
    {
      SiteSettings? restore = await siteSettingsStore.GetAsync();
      restore.ShouldNotBeNull();
      restore!.ReplacePolicy(previous, restore.EntraAllowBootstrap, restore.PasskeyPromptMode);
      await siteSettingsStore.UpdateAsync(restore);
    }
  }

  public static async Task Bootstrap_With_Public_Origin_Should_Redirect_To_Local_Return_Url()
  {
    App.ShouldNotBeNull();
    EntraAuthenticationOptions options = App.Services.GetRequiredService<IOptions<EntraAuthenticationOptions>>().Value;
    string? previous = options.PublicOrigin;
    options.PublicOrigin = "https://arch.timewarp.work";
    try
    {
      HttpResponseMessage response = await SendChallengeAsync("bootstrap", TrustedTenantId, Guid.NewGuid());
      response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
      response.Headers.Location.ShouldNotBeNull();
      response.Headers.Location.ShouldNotBeNull();
      response.Headers.Location!.ToString().ShouldStartWith(EntraChoiceCookie.ChoosePath);
    }
    finally
    {
      options.PublicOrigin = previous;
    }
  }

  public static async Task Invalid_Mode_Should_400()
  {
    HttpResponseMessage response = await SendChallengeAsync("signin", TrustedTenantId, Guid.NewGuid());
    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    SharedProblemDetails problem = await ReadProblemAsync(response);
    problem.Title.ShouldBe("Invalid Entra challenge mode");
  }

  public static async Task Issuer_Mismatch_Should_400()
  {
    Client.ShouldNotBeNull();
    using HttpRequestMessage request = new(
      HttpMethod.Get,
      "/api/identity/entra/challenge?mode=bootstrap&returnUrl=%2F");
    request.Headers.TryAddWithoutValidation(TenantIdHeader, TrustedTenantId.ToString("D"));
    request.Headers.TryAddWithoutValidation(ObjectIdHeader, Guid.NewGuid().ToString("D"));
    request.Headers.TryAddWithoutValidation(FakeEntraHeaders.Issuer, "https://login.microsoftonline.com/wrong/v2.0");
    HttpResponseMessage response = await Client.SendAsync(request);
    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    SharedProblemDetails problem = await ReadProblemAsync(response);
    problem.Title.ShouldBe("Invalid Entra token");
    problem.Detail.ShouldBe("The Entra ID token issuer does not match the tenant.");
  }

  public static async Task Missing_Oid_Should_400_Naming_The_Failing_Check()
  {
    Client.ShouldNotBeNull();
    using HttpRequestMessage request = new(
      HttpMethod.Get,
      "/api/identity/entra/challenge?mode=bootstrap&returnUrl=%2F");
    request.Headers.TryAddWithoutValidation(TenantIdHeader, TrustedTenantId.ToString("D"));
    request.Headers.TryAddWithoutValidation(FakeEntraHeaders.OmitObjectId, "1");
    HttpResponseMessage response = await Client.SendAsync(request);
    response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    SharedProblemDetails problem = await ReadProblemAsync(response);
    problem.Title.ShouldBe("Invalid Entra token");
    problem.Detail.ShouldBe("The Entra ID token is missing the oid claim.");
    (problem.Detail ?? "").ShouldNotContain(TrustedTenantId.ToString("D"));
  }

  private static async Task<HttpResponseMessage> SendChallengeAsync
  (
    string mode,
    Guid tenantId,
    Guid objectId,
    string? cookie = null,
    string? preferredUsername = null,
    string? name = null
  )
  {
    Client.ShouldNotBeNull();
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

    return await Client.SendAsync(request);
  }

  private static async Task<string> SignInAsync(PrincipalId principalId)
  {
    Client.ShouldNotBeNull();
    HttpResponseMessage response = await Client.PostAsync($"/test/signin/{principalId.Value:D}", content: null);
    response.EnsureSuccessStatusCode();
    response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? setCookieValues).ShouldBeTrue();
    string setCookie = setCookieValues!.First(value => value.Contains(IdentitySessionDefaults.CookieName, StringComparison.Ordinal));
    return setCookie.Split(';', 2)[0];
  }

  private static async Task<SharedProblemDetails> ReadProblemAsync(HttpResponseMessage response)
  {
    string json = await response.Content.ReadAsStringAsync();
    SharedProblemDetails? problem = System.Text.Json.JsonSerializer.Deserialize<SharedProblemDetails>(
      json,
      ContractSerializationDefaults.Options);
    problem.ShouldNotBeNull();
    return problem;
  }
}

internal sealed class FakeEntraHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
  public FakeEntraHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : base(options, logger, encoder)
  {
  }

  protected override Task<AuthenticateResult> HandleAuthenticateAsync() =>
    Task.FromResult(AuthenticateResult.NoResult());

  protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
  {
    Guid tenantId = Guid.TryParse(Request.Headers[FakeEntraHeaders.TenantId], out Guid parsedTenant) ? parsedTenant : Guid.Empty;
    Guid objectId = Guid.TryParse(Request.Headers[FakeEntraHeaders.ObjectId], out Guid parsedObject) ? parsedObject : Guid.Empty;
    string issuer = Request.Headers[FakeEntraHeaders.Issuer].ToString();
    if (string.IsNullOrWhiteSpace(issuer))
    {
      issuer = Encoding.UTF8.GetString(EntraIssuerMaterial.FromTenantId(tenantId));
    }

    ClaimsIdentity identity = new(EntraLinkDefaults.Scheme);
    identity.AddClaim(new Claim("tid", tenantId.ToString("D")));
    if (!string.Equals(Request.Headers[FakeEntraHeaders.OmitObjectId], "1", StringComparison.Ordinal))
    {
      identity.AddClaim(new Claim("oid", objectId.ToString("D")));
    }

    identity.AddClaim(new Claim("iss", issuer));
    string name = Request.Headers[FakeEntraHeaders.Name].ToString();
    identity.AddClaim(new Claim("name", string.IsNullOrWhiteSpace(name) ? "Test User" : name));
    string preferredUsername = Request.Headers[FakeEntraHeaders.PreferredUsername].ToString();
    if (!string.IsNullOrWhiteSpace(preferredUsername))
    {
      identity.AddClaim(new Claim("preferred_username", preferredUsername));
    }

    await EntraTicketHttp.HandleTicketAsync(
      Context,
      new ClaimsPrincipal(identity),
      properties,
      Context.RequestAborted);
  }
}
