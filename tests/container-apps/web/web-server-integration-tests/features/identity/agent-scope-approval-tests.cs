#region Purpose
// Issuance accepts only admin-approved agent scopes, with identity:read always allowed.
#endregion

#region Design
// Task 466. Own host so a Developer role's agent scopes cannot leak into another class.
// A new agent has no effective role, so only identity:read succeeds even when Member
// carries other scopes. Role coverage assigns Developer (a product role the effective
// resolver keeps) and puts the feedback scopes on that role. Principal coverage writes
// IPrincipalAgentScopeStore directly. A mixed request is refused whole: no token, and the
// consumed challenge cannot be replayed.
#endregion

namespace AgentScopeApproval_;

using System.Buffers.Text;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;
using IRoleStore = TimeWarp.Architecture.Features.Admin.Roles.Application.IRoleStore;

public class Refuses_
{
  private static HostGraph? Graph;
  private static WebTestServerApplication Web => Graph!.Web!;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Refuses_>();

  public static async Task SetupOnce()
  {
#if(api)
    Graph = await HostGraphFactory.CreateWebWithApiAsync();
#else
    Graph = await HostGraphFactory.CreateWebAsync();
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

  public static async Task IdentityRead_Succeeds_And_FeedbackRead_NamesTheScope()
  {
    var allowedKey = new IntegrationSoftwareAgentKey();
    (PrincipalId allowedPrincipal, OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> allowed, _) =
      await RegisterAndRequestAsync(allowedKey, [AgentScopes.IdentityRead]);
    _ = allowedPrincipal;
    allowed.IsT0.ShouldBeTrue("identity:read is approved for a new agent.");
    allowed.AsT0.Scopes.ShouldBe([AgentScopes.IdentityRead]);

    var refusedKey = new IntegrationSoftwareAgentKey();
    (PrincipalId refusedPrincipal, OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> refused, CompleteAgentTokenIssuance.Command refusedCommand) =
      await RegisterAndRequestAsync(refusedKey, [AgentScopes.FeedbackRead]);
    refused.IsT2.ShouldBeTrue();
    refused.AsT2.Status.ShouldBe(403);
    refused.AsT2.Title.ShouldBe("unapproved_scope");
    refused.AsT2.Detail.ShouldBe($"Unapproved scope(s): {AgentScopes.FeedbackRead}.");

    IPrincipalStore store = Web.WebApplicationHost.ServiceProvider.GetRequiredService<IPrincipalStore>();
    Credential credential = (await store.ListCredentialsAsync(refusedPrincipal)).Single();
    credential.LastUsedAt.ShouldBeNull("a refused issuance does not stamp last-used");

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> replay =
      await Web.GetResponse<CompleteAgentTokenIssuance.Response>(refusedCommand, CancellationToken.None);
    replay.IsT2.ShouldBeTrue();
    replay.AsT2.Status.ShouldBe(400);
    replay.AsT2.Title.ShouldBe("Challenge invalid");
  }

  public static async Task RoleGrant_Allows_FeedbackRead()
  {
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IRoleStore roleScopes = scope.ServiceProvider.GetRequiredService<IRoleStore>();
    IPrincipalRoleStore roles = scope.ServiceProvider.GetRequiredService<IPrincipalRoleStore>();
    try
    {
      await roleScopes.SetScopesForRoleAsync(
        RoleIds.Developer,
        [
          AgentScopes.FeedbackRead,
          AgentScopes.FeedbackNote,
          AgentScopes.FeedbackSeverity,
          AgentScopes.FeedbackStatus,
        ]);

      var key = new IntegrationSoftwareAgentKey();
      (PrincipalId principalId, string keyId) = await RegisterAgentAsync(key);
      await roles.SetRoleIdsAsync(principalId, [RoleIds.Developer]);

      OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> issued =
        await RequestAsync(key, keyId, [AgentScopes.FeedbackRead]);
      issued.IsT0.ShouldBeTrue("a role's agent scopes approve the request.");
      issued.AsT0.Scopes.ShouldBe([AgentScopes.FeedbackRead]);
    }
    finally
    {
      await roleScopes.SetScopesForRoleAsync(RoleIds.Developer, []);
    }
  }

  public static async Task PrincipalGrant_Allows_Then_Removal_Refuses()
  {
    var key = new IntegrationSoftwareAgentKey();
    (PrincipalId principalId, string keyId) = await RegisterAgentAsync(key);

    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IPrincipalAgentScopeStore grants = scope.ServiceProvider.GetRequiredService<IPrincipalAgentScopeStore>();
    await grants.SetScopesAsync(principalId, [AgentScopes.FeedbackRead]);

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> issued =
      await RequestAsync(key, keyId, [AgentScopes.FeedbackRead]);
    issued.IsT0.ShouldBeTrue("a principal grant alone approves the scope.");
    issued.AsT0.Scopes.ShouldBe([AgentScopes.FeedbackRead]);

    await grants.SetScopesAsync(principalId, []);
    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> removed =
      await RequestAsync(key, keyId, [AgentScopes.FeedbackRead]);
    removed.IsT2.ShouldBeTrue();
    removed.AsT2.Status.ShouldBe(403);
    removed.AsT2.Title.ShouldBe("unapproved_scope");
    removed.AsT2.Detail.ShouldBe($"Unapproved scope(s): {AgentScopes.FeedbackRead}.");
  }

  public static async Task Mixed_Approved_And_Unapproved_Issues_No_Token()
  {
    var key = new IntegrationSoftwareAgentKey();
    (PrincipalId principalId, string keyId) = await RegisterAgentAsync(key);
    await AgentScopeApprovals.ApproveAsync(
      Web.WebApplicationHost.ServiceProvider,
      principalId,
      [AgentScopes.FeedbackRead]);

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> mixed =
      await RequestAsync(key, keyId, [AgentScopes.FeedbackRead, AgentScopes.DemoInvoke]);
    mixed.IsT2.ShouldBeTrue("one unapproved scope refuses the whole request.");
    mixed.AsT2.Status.ShouldBe(403);
    mixed.AsT2.Detail.ShouldBe($"Unapproved scope(s): {AgentScopes.DemoInvoke}.");

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> onlyApproved =
      await RequestAsync(key, keyId, [AgentScopes.FeedbackRead]);
    onlyApproved.IsT0.ShouldBeTrue();
    onlyApproved.AsT0.Scopes.ShouldBe([AgentScopes.FeedbackRead]);
    onlyApproved.AsT0.AccessToken.ShouldNotBeNullOrWhiteSpace();
  }

  public static async Task MemberScopes_Should_NotApproveARolelessAgent()
  {
    await using AsyncServiceScope scope = Web.WebApplicationHost.ServiceProvider.CreateAsyncScope();
    IRoleStore roleScopes = scope.ServiceProvider.GetRequiredService<IRoleStore>();
    IEffectiveRolesResolver roles = scope.ServiceProvider.GetRequiredService<IEffectiveRolesResolver>();
    try
    {
      await roleScopes.SetScopesForRoleAsync(RoleIds.Member, [AgentScopes.FeedbackRead]);

      IntegrationSoftwareAgentKey key = new();
      (PrincipalId principalId, string keyId) = await RegisterAgentAsync(key);

      IReadOnlyList<Guid> effective = await roles.GetEffectiveRoleIdsAsync(principalId);
      effective.ShouldBeEmpty();

      OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> allowed =
        await RequestAsync(key, keyId, [AgentScopes.IdentityRead]);
      allowed.IsT0.ShouldBeTrue("identity:read stays approved with no role.");
      allowed.AsT0.Scopes.ShouldBe([AgentScopes.IdentityRead]);

      OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> refused =
        await RequestAsync(key, keyId, [AgentScopes.FeedbackRead]);
      refused.IsT2.ShouldBeTrue();
      refused.AsT2.Status.ShouldBe(403);
      refused.AsT2.Title.ShouldBe("unapproved_scope");
      refused.AsT2.Detail.ShouldBe($"Unapproved scope(s): {AgentScopes.FeedbackRead}.");
    }
    finally
    {
      await roleScopes.SetScopesForRoleAsync(RoleIds.Member, []);
    }
  }

  private static async Task<(PrincipalId PrincipalId, OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> Result, CompleteAgentTokenIssuance.Command Command)> RegisterAndRequestAsync(
    IntegrationSoftwareAgentKey key,
    List<string> scopes)
  {
    (PrincipalId principalId, string keyId) = await RegisterAgentAsync(key);
    (OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> result, CompleteAgentTokenIssuance.Command command) =
      await RequestWithCommandAsync(key, keyId, scopes);
    return (principalId, result, command);
  }

  private static async Task<(PrincipalId PrincipalId, string KeyId)> RegisterAgentAsync(IntegrationSoftwareAgentKey key)
  {
    (string publicKey, string challenge, string signature) =
      await CredentialCeremonyHelpers.BuildAgentKeyRegistrationProofAsync(Web, key);

    OneOf<CompleteAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> registered =
      await Web.GetResponse<CompleteAgentKeyRegistration.Response>(
        new CompleteAgentKeyRegistration.Command
        {
          PublicKey = publicKey,
          Challenge = challenge,
          Signature = signature,
        },
        CancellationToken.None);
    registered.IsT0.ShouldBeTrue("Agent key registration setup should succeed.");
    return (registered.AsT0.PrincipalId, registered.AsT0.KeyId);
  }

  private static async Task<OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails>> RequestAsync(
    IntegrationSoftwareAgentKey key,
    string keyId,
    List<string> scopes)
  {
    (OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> result, _) =
      await RequestWithCommandAsync(key, keyId, scopes);
    return result;
  }

  private static async Task<(OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> Result, CompleteAgentTokenIssuance.Command Command)> RequestWithCommandAsync(
    IntegrationSoftwareAgentKey key,
    string keyId,
    List<string> scopes)
  {
    OneOf<StartAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> started =
      await Web.GetResponse<StartAgentTokenIssuance.Response>(new StartAgentTokenIssuance.Command(), CancellationToken.None);
    started.IsT0.ShouldBeTrue();
    byte[] challenge = Base64Url.DecodeFromChars(started.AsT0.Challenge);
    byte[] signature = key.Sign(AgentKeyCeremonyType.TokenIssuance, challenge);
    var command = new CompleteAgentTokenIssuance.Command
    {
      KeyId = keyId,
      Challenge = Base64Url.EncodeToString(challenge),
      Signature = Base64Url.EncodeToString(signature),
      Scopes = scopes,
    };

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> result =
      await Web.GetResponse<CompleteAgentTokenIssuance.Response>(command, CancellationToken.None);
    return (result, command);
  }
}
