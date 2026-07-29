#region Purpose
// End-to-end tests for the agent access-token issuance ceremony (StartAgentTokenIssuance +
// CompleteAgentTokenIssuance): register-then-issue against a deterministic-shape software agent key.
#endregion

#region Design
// Same per-class fixture-sharing / per-instance-random-key posture as Agent_Registration_Tests.cs —
// see IntegrationSoftwareAgentKey's Design region.
// No-enumeration-oracle assertions (unknown KeyId vs bad signature both fail identically) directly
// exercise CompleteAgentTokenIssuance.Handler's documented posture (task 104-004 §5).
#endregion

namespace AgentToken_;

using System.Buffers.Text;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Server.Integration.Tests.Features.Identity.Infrastructure;
using TimeWarp.Identity;

public class Returns_
{
  private readonly WebTestServerApplication WebTestServerApplication;

  public Returns_(WebTestServerApplication webTestServerApplication)
  {
    WebTestServerApplication = webTestServerApplication;
  }

  public async Task Ok_With_Bearer_Token_Given_Valid_Issuance()
  {
    var key = new IntegrationSoftwareAgentKey();
    string keyId = await RegisterAgentKey(key);

    CompleteAgentTokenIssuance.Command tokenCommand = await BuildValidTokenCommand(key, keyId, [AgentScopes.IdentityRead]);

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> result =
      await WebTestServerApplication.GetResponse<CompleteAgentTokenIssuance.Response>(tokenCommand, CancellationToken.None);

    result.IsT0.ShouldBeTrue("Token issuance should succeed.");
    result.AsT0.AccessToken.ShouldNotBeNullOrEmpty();
    result.AsT0.TokenType.ShouldBe("Bearer");
    // 20 minutes: the test host's appsettings.json overrides AgentTokenOptions:TokenLifetimeMinutes
    // (not the C# default of 15) — see AgentTokenOptionsBinding_ for why that override exists.
    result.AsT0.ExpiresInSeconds.ShouldBe(20 * 60);
    result.AsT0.Scopes.ShouldBe([AgentScopes.IdentityRead]);
  }

  public async Task BadRequest_Given_Unknown_KeyId()
  {
    var key = new IntegrationSoftwareAgentKey();
    // Never registered — FindCredentialByHandleAsync must return null.
    string neverRegisteredKeyId = Base64Url.EncodeToString(key.KeyId);

    CompleteAgentTokenIssuance.Command tokenCommand = await BuildValidTokenCommand(key, neverRegisteredKeyId, [AgentScopes.IdentityRead]);

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> result =
      await WebTestServerApplication.GetResponse<CompleteAgentTokenIssuance.Response>(tokenCommand, CancellationToken.None);

    result.IsT2.ShouldBeTrue("Issuance with an unregistered key should fail.");
    result.AsT2.Status.ShouldBe(400);
    // Pins the no-enumeration-oracle equivalence (round-1 finding M5): must be byte-identical to
    // the bad-signature rejection below, not merely the same status code.
    result.AsT2.Title.ShouldBe("Token issuance failed");
  }

  public async Task BadRequest_Given_Bad_Signature_Identical_To_Unknown_KeyId()
  {
    // No-enumeration-oracle: registered-but-bad-signature and never-registered must produce the
    // SAME problem shape, not a distinguishable error.
    var key = new IntegrationSoftwareAgentKey();
    string keyId = await RegisterAgentKey(key);

    OneOf<StartAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> start =
      await WebTestServerApplication.GetResponse<StartAgentTokenIssuance.Response>(new StartAgentTokenIssuance.Command(), CancellationToken.None);
    byte[] challenge = Base64Url.DecodeFromChars(start.AsT0.Challenge);
    byte[] signature = key.Sign(AgentKeyCeremonyType.TokenIssuance, challenge);
    signature[0] ^= 0xFF;

    var tokenCommand = new CompleteAgentTokenIssuance.Command
    {
      KeyId = keyId,
      Challenge = Base64Url.EncodeToString(challenge),
      Signature = Base64Url.EncodeToString(signature),
      Scopes = [AgentScopes.IdentityRead]
    };

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> result =
      await WebTestServerApplication.GetResponse<CompleteAgentTokenIssuance.Response>(tokenCommand, CancellationToken.None);

    result.IsT2.ShouldBeTrue("Issuance with a tampered signature should fail.");
    result.AsT2.Status.ShouldBe(400);
    result.AsT2.Title.ShouldBe("Token issuance failed");
  }

  public async Task BadRequest_InvalidScope_Given_Unknown_Scope()
  {
    var key = new IntegrationSoftwareAgentKey();
    string keyId = await RegisterAgentKey(key);

    CompleteAgentTokenIssuance.Command tokenCommand = await BuildValidTokenCommand(key, keyId, ["not-a-real-scope"]);

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> result =
      await WebTestServerApplication.GetResponse<CompleteAgentTokenIssuance.Response>(tokenCommand, CancellationToken.None);

    result.IsT2.ShouldBeTrue("Issuance with an unknown scope should fail.");
    result.AsT2.Status.ShouldBe(400);
    result.AsT2.Title.ShouldBe("invalid_scope");
  }

  public async Task ValidationError_Given_Null_Scopes()
  {
    // Round-1 finding M1: a JSON body with "scopes": null must produce a clean 400 validation
    // problem, not an unhandled 500 (FluentValidation's NotEmpty-then-Must cascade previously
    // dereferenced a null Scopes list). ContractSerializationDefaults writes explicit nulls (no
    // DefaultIgnoreCondition configured), so setting Scopes = null! here genuinely reaches the wire
    // as literal "scopes":null, not an omitted property.
    var key = new IntegrationSoftwareAgentKey();
    string keyId = await RegisterAgentKey(key);

    OneOf<StartAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> start =
      await WebTestServerApplication.GetResponse<StartAgentTokenIssuance.Response>(new StartAgentTokenIssuance.Command(), CancellationToken.None);
    byte[] challenge = Base64Url.DecodeFromChars(start.AsT0.Challenge);
    byte[] signature = key.Sign(AgentKeyCeremonyType.TokenIssuance, challenge);

    var tokenCommand = new CompleteAgentTokenIssuance.Command
    {
      KeyId = keyId,
      Challenge = Base64Url.EncodeToString(challenge),
      Signature = Base64Url.EncodeToString(signature),
      Scopes = null!
    };

    await WebTestServerApplication.ConfirmEndpointValidationError<CompleteAgentTokenIssuance.Response>
      (tokenCommand, nameof(CompleteAgentTokenIssuance.Command.Scopes));
  }

  public async Task BadRequest_Given_Reused_Challenge()
  {
    var key = new IntegrationSoftwareAgentKey();
    string keyId = await RegisterAgentKey(key);

    CompleteAgentTokenIssuance.Command tokenCommand = await BuildValidTokenCommand(key, keyId, [AgentScopes.IdentityRead]);

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> first =
      await WebTestServerApplication.GetResponse<CompleteAgentTokenIssuance.Response>(tokenCommand, CancellationToken.None);
    first.IsT0.ShouldBeTrue("First issuance should succeed.");

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> replay =
      await WebTestServerApplication.GetResponse<CompleteAgentTokenIssuance.Response>(tokenCommand, CancellationToken.None);

    replay.IsT2.ShouldBeTrue("Replayed issuance should fail.");
    replay.AsT2.Status.ShouldBe(400);
  }

  public async Task Forbidden_Given_Quarantined_Principal()
  {
    // G3 (104-006): proof must succeed, then quarantine is the distinct 403 signal — not 400.
    var key = new IntegrationSoftwareAgentKey();
    (string keyId, PrincipalId principalId) = await RegisterAgentKeyWithPrincipal(key);
    await QuarantinePrincipal(principalId);

    CompleteAgentTokenIssuance.Command tokenCommand = await BuildValidTokenCommand(key, keyId, [AgentScopes.IdentityRead]);

    OneOf<CompleteAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> result =
      await WebTestServerApplication.GetResponse<CompleteAgentTokenIssuance.Response>(tokenCommand, CancellationToken.None);

    result.IsT2.ShouldBeTrue("Token issuance for a quarantined principal should return a problem.");
    result.AsT2.Status.ShouldBe(403);
    result.AsT2.Title.ShouldBe("Account quarantined");
  }

  private async Task QuarantinePrincipal(PrincipalId principalId)
  {
    IPrincipalStore store =
      WebTestServerApplication.WebApplicationHost.ServiceProvider.GetRequiredService<IPrincipalStore>();
    Principal? principal = await store.GetPrincipalAsync(principalId);
    principal.ShouldNotBeNull();
    principal.Quarantine();
    await store.UpdatePrincipalAsync(principal);
  }

  private async Task<string> RegisterAgentKey(IntegrationSoftwareAgentKey key)
  {
    (string keyId, PrincipalId _) = await RegisterAgentKeyWithPrincipal(key);
    return keyId;
  }

  private async Task<(string KeyId, PrincipalId PrincipalId)> RegisterAgentKeyWithPrincipal(IntegrationSoftwareAgentKey key)
  {
    OneOf<StartAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> start =
      await WebTestServerApplication.GetResponse<StartAgentKeyRegistration.Response>(new StartAgentKeyRegistration.Command(), CancellationToken.None);
    byte[] challenge = Base64Url.DecodeFromChars(start.AsT0.Challenge);
    byte[] signature = key.Sign(AgentKeyCeremonyType.Registration, challenge);

    var registerCommand = new CompleteAgentKeyRegistration.Command
    {
      PublicKey = Base64Url.EncodeToString(key.SpkiPublicKey),
      Challenge = Base64Url.EncodeToString(challenge),
      Signature = Base64Url.EncodeToString(signature)
    };

    OneOf<CompleteAgentKeyRegistration.Response, FileResponse, SharedProblemDetails> result =
      await WebTestServerApplication.GetResponse<CompleteAgentKeyRegistration.Response>(registerCommand, CancellationToken.None);

    result.IsT0.ShouldBeTrue("Registration setup for a token test should succeed.");
    return (result.AsT0.KeyId, result.AsT0.PrincipalId);
  }

  private async Task<CompleteAgentTokenIssuance.Command> BuildValidTokenCommand(IntegrationSoftwareAgentKey key, string keyId, List<string> scopes)
  {
    OneOf<StartAgentTokenIssuance.Response, FileResponse, SharedProblemDetails> start =
      await WebTestServerApplication.GetResponse<StartAgentTokenIssuance.Response>(new StartAgentTokenIssuance.Command(), CancellationToken.None);

    byte[] challenge = Base64Url.DecodeFromChars(start.AsT0.Challenge);
    byte[] signature = key.Sign(AgentKeyCeremonyType.TokenIssuance, challenge);

    return new CompleteAgentTokenIssuance.Command
    {
      KeyId = keyId,
      Challenge = Base64Url.EncodeToString(challenge),
      Signature = Base64Url.EncodeToString(signature),
      Scopes = scopes
    };
  }
}
