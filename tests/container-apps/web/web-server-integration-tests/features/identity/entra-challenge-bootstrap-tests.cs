#region Purpose
// Integration tests for the Entra bootstrap challenge mode (sync-hit, choose redirect, create).
#endregion

#region Design
// Host and request helpers are in EntraChallengeFixture. Unknown-handle bootstrap parks claims and
// 302s to /Login/Microsoft365/Choose (no principal). Create is exercised via the real
// CompleteEntraBootstrapCreate handler mapped on this host. Sync-hit asserts the last-used stamp
// (task 252). One case switches the tenant row to Any and expects UntrustedTenantId to reach choose.
#endregion

namespace EntraChallenge_;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Foundation.Types;
using TimeWarp.Identity;
using static EntraChallenge_.EntraChallengeFixture;

public class Challenge_Bootstrap_Given_
{
  private static WebApplication? App;
  private static HttpClient? Client;
  private static IPrincipalStore? Store;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Challenge_Bootstrap_Given_>();

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

  public static async Task Bootstrap_Untrusted_Tenant_Should_403()
  {
    HttpResponseMessage response = await SendChallengeAsync("bootstrap", UntrustedTenantId, Guid.NewGuid());
    response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    SharedProblemDetails problem = await ReadProblemAsync(response);
    problem.Title.ShouldBe("Untrusted tenant");
  }

  public static async Task Bootstrap_Any_Mode_Untrusted_Tenant_Should_Redirect_To_Choose()
  {
    App.ShouldNotBeNull();
    Store.ShouldNotBeNull();
    ISiteSettingsStore siteSettingsStore = App.Services.GetRequiredService<ISiteSettingsStore>();
    SiteSettings? current = await siteSettingsStore.GetAsync();
    current.ShouldNotBeNull();
    EntraTenantAcceptanceMode previousMode = current!.EntraTenantAcceptanceMode;
    IReadOnlyList<EntraAcceptedTenant> previousTenants = current.EntraAcceptedTenants;
    current.ReplaceEntraTenantAcceptance(EntraTenantAcceptanceMode.Any, previousTenants);
    await siteSettingsStore.UpdateAsync(current);
    try
    {
      int principalCount = (await Store.ListPrincipalsAsync()).Count;
      HttpResponseMessage response = await SendChallengeAsync("bootstrap", UntrustedTenantId, Guid.NewGuid());
      response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
      response.Headers.Location.ShouldNotBeNull();
      response.Headers.Location!.ToString().ShouldStartWith(EntraChoiceCookie.ChoosePath);
      (await Store.ListPrincipalsAsync()).Count.ShouldBe(principalCount);
    }
    finally
    {
      SiteSettings? restore = await siteSettingsStore.GetAsync();
      restore.ShouldNotBeNull();
      restore!.ReplaceEntraTenantAcceptance(previousMode, previousTenants);
      await siteSettingsStore.UpdateAsync(restore);
    }
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
    found.LastUsedAt.ShouldNotBeNull("a Microsoft 365 sign-in stamps last-used (task 252)");
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

  private static Task<HttpResponseMessage> SendChallengeAsync(
    string mode,
    Guid tenantId,
    Guid objectId,
    string? cookie = null,
    string? preferredUsername = null,
    string? name = null) =>
    EntraChallengeFixture.SendChallengeAsync(Client, mode, tenantId, objectId, cookie, preferredUsername, name);
}
