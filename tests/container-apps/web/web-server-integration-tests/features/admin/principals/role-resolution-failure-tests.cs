#region Purpose
// In-proc proof that a role-store failure for an authenticated principal is 503, never 401.
#endregion

#region Design
// Task 160: IClaimsTransformation → EffectiveRolesResolver → IPrincipalRoleStore. A throw on
// GetRoleIdsAsync must not be Challenge 401 (PolicyEvaluator treats failed authenticate as
// Challenge) and must not be empty-roles 403. This class owns its HostGraph (C-create) so it can
// DI-replace IPrincipalRoleStore with a Get-throwing fake — no live Postgres race. Registration
// stays anonymous (no cookie yet, so Transform does not call Get); TryClaimFirstAdministrator
// on the fake is a no-op so CompletePasskeyRegistration can mint a cookie. The next request
// with that cookie hits Transform and must be 503. Anonymous GetRoles on the same host stays
// 401 (store is not consulted). Isolated HttpClients so the shared jar cannot leak the cookie
// into the anonymous case.
#endregion

namespace RoleResolutionFailure_;

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;

[TestTag("Integration")]
public class Returns_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Returns_>();

  public static async Task SetupOnce()
  {
#if(api)
    Graph = await HostGraphFactory.CreateWebWithApiAsync(configureWeb: ReplaceRoleStore);
#else
    Graph = await HostGraphFactory.CreateWebAsync(configureWeb: ReplaceRoleStore);
#endif
  }

  public static async Task CleanUpOnce()
  {
    if (Graph is not null)
    {
      await Graph.DisposeAsync();
      Graph = null;
    }
  }

  public static async Task Unauthorized_Given_Anonymous_Get_When_RoleStoreThrows()
  {
    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };

    HttpResponseMessage response = await client.GetAsync("api/Roles");

    response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
  }

  public static async Task ServiceUnavailable_Given_AuthenticatedPrincipal_When_RoleStoreThrows()
  {
    (_, string sessionCookie) = await CredentialCeremonyHelpers.RegisterPasskeyAndMintSessionAsync(Web);

    using HttpClient client = new() { BaseAddress = Web.HttpClient.BaseAddress };
    client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

    HttpResponseMessage response = await client.GetAsync("api/Roles");

    response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
    response.StatusCode.ShouldNotBe(HttpStatusCode.Forbidden);
  }

  private static void ReplaceRoleStore(IServiceCollection services)
  {
    services.RemoveAll<IPrincipalRoleStore>();
    services.AddSingleton<IPrincipalRoleStore, ThrowingGetPrincipalRoleStore>();
  }

  private sealed class ThrowingGetPrincipalRoleStore : IPrincipalRoleStore
  {
    public Task<IReadOnlyList<Guid>> GetRoleIdsAsync(
      PrincipalId principalId,
      CancellationToken cancellationToken = default) =>
      throw new InvalidOperationException("simulated role-store failure");

    public Task SetRoleIdsAsync(
      PrincipalId principalId,
      IReadOnlyList<Guid> roleIds,
      CancellationToken cancellationToken = default) =>
      Task.CompletedTask;

    public Task<bool> TryClaimFirstAdministratorAsync(
      PrincipalId principalId,
      CancellationToken cancellationToken = default) =>
      Task.FromResult(false);
  }
}
