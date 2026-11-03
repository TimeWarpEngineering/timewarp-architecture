#region Purpose
// Shared host factory, HTML fetch, role / credential seeding and assertion helpers for the protected-page
// deep-link test classes (task 154).
#endregion

#region Design
// C-create: each test class owns its HostGraph (CreateHostAsync in SetupOnce, DisposeAsync in
// CleanUpOnce) and passes its WebTestServerApplication to the helpers that touch the host. The
// fixture keeps no host state, so no host is shared across classes.
#endregion

namespace ProtectedPageDeepLink_;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using SignInOptions = TimeWarp.Architecture.Features.Identity.Application.SignInOptions;
using TimeWarp.Identity;

internal static class ProtectedPageDeepLinkFixture
{
  /// <summary>
  /// Builds the host graph a test class owns for its lifetime (C-create): the class stores it in its
  /// SetupOnce and disposes it in its CleanUpOnce.
  /// </summary>
  public static Task<HostGraph> CreateHostAsync() =>
#if(api)
    HostGraphFactory.CreateWebWithApiAsync();
#else
    HostGraphFactory.CreateWebAsync();
#endif

  public const string LastCredentialHint = "Add another passkey or agent key before revoking this one.";

  public static string FindTagContaining(string html, string marker)
  {
    int searchFrom = 0;
    while (searchFrom < html.Length)
    {
      int start = IndexOfOpeningTag(html, searchFrom);
      if (start < 0)
      {
        break;
      }

      int end = html.IndexOf('>', start, StringComparison.Ordinal);
      if (end < 0)
      {
        break;
      }

      string tag = html[start..(end + 1)];
      if (tag.Contains(marker, StringComparison.Ordinal))
      {
        return tag;
      }

      searchFrom = end + 1;
    }

    throw new ShouldAssertException($"Expected an opening button tag containing {marker}.");
  }

  public static int IndexOfOpeningTag(string html, int startIndex)
  {
    int fluentButton = html.IndexOf("<fluent-button", startIndex, StringComparison.OrdinalIgnoreCase);
    int button = html.IndexOf("<button", startIndex, StringComparison.OrdinalIgnoreCase);
    if (fluentButton < 0)
    {
      return button;
    }

    if (button < 0)
    {
      return fluentButton;
    }

    return Math.Min(fluentButton, button);
  }

  public static async Task<HttpResponseMessage> GetPageHtml(WebTestServerApplication web, string path, string sessionCookie)
  {
    using HttpClient client = CreateNoRedirectClient(web);
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);
    using HttpRequestMessage request = new(HttpMethod.Get, path);
    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
    return await client.SendAsync(request);
  }

  public static async Task SetRolesAsync(WebTestServerApplication web, PrincipalId principalId, Guid[] roleIds)
  {
    // Scope required: under postgres IPrincipalRoleStore is scoped (EfPrincipalRoleStore).
    await using AsyncServiceScope scope = web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalRoleStore roleStore = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    await roleStore.SetRoleIdsAsync(principalId, roleIds);
  }

  // Task 250 shape: Label is the provider, the linked account is AccountHint (the row's context line).

  public static async Task AddEntraAccountAsync(WebTestServerApplication web, PrincipalId principalId, string accountHint)
  {
    await using AsyncServiceScope scope = web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalStore principalStore = scope.ServiceProvider.GetRequiredService<IPrincipalStore>();
    Guid tenantId = Guid.NewGuid();
    await principalStore.AddCredentialAsync(
      Credential.Create(
        principalId,
        CredentialType.EntraAccount,
        EntraAccountHandle.Encode(tenantId, Guid.NewGuid()),
        EntraIssuerMaterial.FromTenantId(tenantId),
        label: "Microsoft 365",
        accountHint: accountHint));
  }

  public static async Task AddAgentKeyAsync(WebTestServerApplication web, PrincipalId principalId, string label)
  {
    // Store-level insert (same shape as AddEntraAccountAsync): the count under test is "active
    // credentials of any kind", so the material only needs to be a distinct AgentKey row.
    await using AsyncServiceScope scope = web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalStore principalStore = scope.ServiceProvider.GetRequiredService<IPrincipalStore>();
    byte[] keyId = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
    byte[] publicMaterial = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
    await principalStore.AddCredentialAsync(
      Credential.Create(principalId, CredentialType.AgentKey, keyId, publicMaterial, label));
  }

  public static async Task RevokePasskeysAsync(WebTestServerApplication web, PrincipalId principalId)
  {
    await using AsyncServiceScope scope = web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalStore principalStore = scope.ServiceProvider.GetRequiredService<IPrincipalStore>();
    IReadOnlyList<Credential> credentials = await principalStore.ListCredentialsAsync(principalId);
    foreach (Credential credential in credentials.Where(c => c.Type == CredentialType.Passkey && !c.IsRevoked))
    {
      credential.Revoke();
      await principalStore.UpdateCredentialAsync(credential);
    }
  }

  public static async Task<IAsyncDisposable> EnableMicrosoft365OfferedAsync(WebTestServerApplication web)
  {
    AsyncServiceScope scope = web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    SignInOptions signIn =
      scope.ServiceProvider.GetRequiredService<IOptions<SignInOptions>>().Value;
    ISiteSettingsStore store = scope.ServiceProvider.GetRequiredService<ISiteSettingsStore>();
    SiteSettings? settings = await store.GetAsync();
    settings.ShouldNotBeNull();
    List<SignInMethod> previousMethods = [.. signIn.Methods];
    bool previousSignIn = settings.EntraSignInEnabled;
    bool previousBootstrap = settings.EntraAllowBootstrap;
    PasskeyPromptMode previousMode = settings.PasskeyPromptMode;
    signIn.Methods = [SignInMethod.Passkey, SignInMethod.Microsoft365];
    settings.ReplacePolicy(true, previousBootstrap, previousMode);
    await store.UpdateAsync(settings);
    return new RestoreMicrosoft365Offered(scope, signIn, previousMethods, store, previousSignIn, previousBootstrap, previousMode);
  }

  internal sealed class RestoreMicrosoft365Offered : IAsyncDisposable
  {
    private readonly AsyncServiceScope Scope;
    private readonly SignInOptions SignIn;
    private readonly List<SignInMethod> PreviousMethods;
    private readonly ISiteSettingsStore Store;
    private readonly bool PreviousSignIn;
    private readonly bool PreviousBootstrap;
    private readonly PasskeyPromptMode PreviousMode;

    public RestoreMicrosoft365Offered(
      AsyncServiceScope scope,
      SignInOptions signIn,
      List<SignInMethod> previousMethods,
      ISiteSettingsStore store,
      bool previousSignIn,
      bool previousBootstrap,
      PasskeyPromptMode previousMode)
    {
      Scope = scope;
      SignIn = signIn;
      PreviousMethods = previousMethods;
      Store = store;
      PreviousSignIn = previousSignIn;
      PreviousBootstrap = previousBootstrap;
      PreviousMode = previousMode;
    }

    public async ValueTask DisposeAsync()
    {
      SignIn.Methods = PreviousMethods;
      SiteSettings? restore = await Store.GetAsync();
      restore.ShouldNotBeNull();
      restore.ReplacePolicy(PreviousSignIn, PreviousBootstrap, PreviousMode);
      await Store.UpdateAsync(restore);
      await Scope.DisposeAsync();
    }
  }

  public static HttpClient CreateNoRedirectClient(WebTestServerApplication web)
  {
    HttpClientHandler handler = new()
    {
      AllowAutoRedirect = false,
      CheckCertificateRevocationList = true,
    };
    return new HttpClient(handler) { BaseAddress = web.HttpClient.BaseAddress };
  }

  public static void AssertRedirectToLogin(HttpResponseMessage response, string expectedReturnUrl)
  {
    response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    response.Headers.Location.ShouldNotBeNull();

    string location = response.Headers.Location.IsAbsoluteUri
      ? response.Headers.Location.PathAndQuery
      : response.Headers.Location.OriginalString;

    // Exact shape from IdentitySessionCookieChallenge.BuildLoginRedirectTarget.
    string expected =
      IdentitySessionCookieChallenge.LoginPath
      + $"?{IdentitySessionCookieChallenge.ReturnUrlQueryParameter}"
      + $"={Uri.EscapeDataString(expectedReturnUrl)}";
    location.ShouldBe(expected);
  }
}
