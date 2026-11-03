#region Purpose
// Integration tests for Entra challenge requests that are rejected (disabled, invalid mode, bad token claims).
#endregion

#region Design
// Isolated TestServer (not the full web-server HostGraph) with FakeEntraHandler standing in for
// OpenIdConnect. Challenge immediately completes the ticket via EntraTicketHttp using tid/oid/iss
// from test headers. Unknown-handle bootstrap parks claims and 302s to /Login/Microsoft365/Choose
// (no principal). Create is exercised via the real CompleteEntraBootstrapCreate handler mapped on
// this host. Link of a foreign active handle merges; already-on-this-account is 409.
// CredentialUsageRecorder is registered as the singleton the real host's InMemoryIdentityStoresModule
// provides; sync-hit and link assert the last-used stamp end-to-end (task 252).
// The settings row is Restricted and lists TrustedTenantId. options.TenantId is only the OIDC
// authority. One case switches the row to Any and expects UntrustedTenantId to reach choose.
#endregion

namespace EntraChallenge_;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TimeWarp.Foundation.Types;
using TimeWarp.Identity;
using static EntraChallenge_.EntraChallengeFixture;

public class Challenge_Rejection_Given_
{
  private static WebApplication? App;
  private static HttpClient? Client;
  private static IPrincipalStore? Store;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Challenge_Rejection_Given_>();

  public static async Task SetupOnce()
  {
    App = await EntraChallengeFixture.StartAsync();
    Client = App.GetTestClient();
    Store = App.Services.GetRequiredService<IPrincipalStore>();
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

  private static Task<HttpResponseMessage> SendChallengeAsync(
    string mode,
    Guid tenantId,
    Guid objectId,
    string? cookie = null,
    string? preferredUsername = null,
    string? name = null) =>
    EntraChallengeFixture.SendChallengeAsync(Client, mode, tenantId, objectId, cookie, preferredUsername, name);
}
