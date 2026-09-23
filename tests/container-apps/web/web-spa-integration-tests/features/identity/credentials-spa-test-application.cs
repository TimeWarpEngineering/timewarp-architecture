#region Purpose
// In-proc SPA ServiceProvider for CredentialsState facts: TimeWarp.State + a scripted
// IWebServerApiService + a fixed signed-in AuthenticationStateProvider. No Aspire host.
#endregion

#region Design
// C-create (AGENTS.md fixture-lifetime default): these facts substitute IWebServerApiService so
// GetCredentials can answer with a chosen credential mix (passkey + agent key, one active, …)
// and RevokeCredential can "remove" a row from the next snapshot — the closed-box
// AspireSpaTestApplication cannot script the server. FetchCredentials / RevokeCredential handlers
// take an AuthenticationStateProvider, and ApiHandler silently drops actions from anonymous
// users, so the provider must answer with an authenticated principal carrying a Guid "sub".
// TimeWarp.State.Plus is included so [TrackAction] can resolve ActionTrackingState (same as
// AspireSpaTestApplication). Scripted is a singleton so SpaTestScope sees the same instance
// the handlers resolve.
#endregion

namespace TimeWarp.Architecture.Web.Spa.Integration.Tests.Features.Identity;

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Foundation.Features;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Identity.GetCredentials;

internal sealed class CredentialsSpaTestApplication : ISpaTestApplication, IDisposable
{
  public IServiceProvider ServiceProvider { get; }
  public ScriptedCredentialsApiService Scripted { get; }

  public CredentialsSpaTestApplication()
  {
    Scripted = new ScriptedCredentialsApiService();
    ServiceCollection services = new();

    services.AddLogging();
    services.AddWebSpaGeneratedMediator();
    services.AddTimeWarpState
    (
      options =>
      {
        options.Assemblies =
        [
          typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly,
          typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly
        ];
      }
    );

    // Plus notification handlers (LoadPersistentState) are linked into the generated
    // mediator and require IPersistenceService when the pipeline resolves them.
    services.AddScoped<
      TimeWarp.Features.Persistence.IPersistenceService,
      TimeWarp.Features.Persistence.PersistenceService>();

    // Fully qualified: TimeWarp.Architecture.Services is a global using only when the api
    // template flag is on; IWebServerApiService is the BFF client and exists without api.
    services.AddSingleton<TimeWarp.Architecture.Services.IWebServerApiService>(Scripted);
    services.AddScoped<AuthenticationStateProvider>(_ => new SignedInAuthenticationStateProvider());

    IJSRuntime fakeJsRuntime = FakeItEasy.A.Fake<IJSRuntime>();
    services.AddScoped(_ => fakeJsRuntime);

    ServiceProvider = services.BuildServiceProvider();
  }

  public void Dispose()
  {
    if (ServiceProvider is IDisposable disposable)
    {
      disposable.Dispose();
    }
  }

  /// <summary>Always-authenticated principal with a Guid "sub" so ApiHandler does not drop actions.</summary>
  private sealed class SignedInAuthenticationStateProvider : AuthenticationStateProvider
  {
    private static readonly Guid UserId = Guid.NewGuid();

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
      ClaimsIdentity identity = new([new Claim("sub", UserId.ToString())], authenticationType: "test");
      return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }
  }
}

/// <summary>
/// Scripted BFF client: GetCredentials answers with the current <see cref="Credentials"/> list;
/// RevokeCredential marks the named row revoked (IsActive=false) so the next GetCredentials
/// reflects it — the same sequence the real server produces for Revoke → Fetch.
/// </summary>
internal sealed class ScriptedCredentialsApiService : TimeWarp.Architecture.Services.IWebServerApiService
{
  public List<CredentialSummary> Credentials { get; } = [];
  public List<IApiRequest> Requests { get; } = [];

  public void Reset()
  {
    Credentials.Clear();
    Requests.Clear();
  }

  public static CredentialSummary Active(CredentialType type, string label) =>
    new(CredentialId.New(), type, label, nickname: null, DateTimeOffset.UtcNow.AddDays(-1), revokedAt: null, isActive: true, RegisteredWith.Unknown, fingerprint: "0123abcd");

  public static CredentialSummary Revoked(CredentialType type, string label) =>
    new(CredentialId.New(), type, label, nickname: null, DateTimeOffset.UtcNow.AddDays(-2), revokedAt: DateTimeOffset.UtcNow.AddDays(-1), isActive: false, RegisteredWith.Unknown, fingerprint: "0123abcd");

  public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
  (
    IApiRequest request,
    CancellationToken cancellationToken
  ) where TResponse : class
  {
    _ = cancellationToken;
    Requests.Add(request);

    switch (request)
    {
      case Query query:
      {
        IReadOnlyList<CredentialSummary> visible =
          query.IncludeRevoked ? [.. Credentials] : [.. Credentials.Where(c => c.IsActive)];
        if (new Response(visible) is TResponse listResponse)
        {
          return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(listResponse);
        }

        break;
      }

      case RevokeCredential.Command command:
      {
        int index = Credentials.FindIndex(c => c.Id.Value == command.CredentialId);
        if (index >= 0)
        {
          CredentialSummary row = Credentials[index];
          Credentials[index] = new CredentialSummary(row.Id, row.Type, row.Label, row.Nickname, row.CreatedAt, DateTimeOffset.UtcNow, isActive: false, row.RegisteredWith, row.Fingerprint, row.LastUsedAt);
        }

        if (new RevokeCredential.Response() is TResponse revokeResponse)
        {
          return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(revokeResponse);
        }

        break;
      }
    }

    throw new InvalidOperationException($"No scripted response for {request.GetType()} → {typeof(TResponse)}.");
  }
}
