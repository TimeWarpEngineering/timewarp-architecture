#region Purpose
// Integration tests for the Entra link challenge mode (attach a Microsoft 365 identity to a signed-in principal).
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
using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Foundation.Types;
using TimeWarp.Identity;
using static EntraChallenge_.EntraChallengeFixture;

public class Challenge_Link_Given_
{
  private static WebApplication? App;
  private static HttpClient? Client;
  private static IPrincipalStore? Store;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Challenge_Link_Given_>();

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

  public static async Task Link_Without_Session_Should_401()
  {
    HttpResponseMessage response = await SendChallengeAsync("link", TrustedTenantId, Guid.NewGuid());
    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
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
    found.LastUsedAt.ShouldNotBeNull("linking stamps last-used (task 252)");
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

  private static Task<HttpResponseMessage> SendChallengeAsync(
    string mode,
    Guid tenantId,
    Guid objectId,
    string? cookie = null,
    string? preferredUsername = null,
    string? name = null) =>
    EntraChallengeFixture.SendChallengeAsync(Client, mode, tenantId, objectId, cookie, preferredUsername, name);

  private static Task<string> SignInAsync(PrincipalId principalId) => EntraChallengeFixture.SignInAsync(Client, principalId);
}
