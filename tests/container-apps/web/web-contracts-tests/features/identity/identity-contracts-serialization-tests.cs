#region Purpose
// Round-trip tests for the identity feature contracts (tasks 104-003, 104-004, 104-005) — the shapes
// where serialization can actually diverge: typed-id (PrincipalId/CredentialId) Responses with a ctor
// Guard, optional-property Commands, list properties, and enum properties
// (PrincipalKind/TrustTier/CredentialType).
#endregion

#region Design
// StartPasskeyRegistration/StartPasskeyAuthentication/StartAgentKeyRegistration/
// StartAgentTokenIssuance's empty Command bodies and their Response's single-string-property shape
// are plain auto-property POCOs — no test here per the skill's "trivial auto-property POCOs are
// deliberately not written" guidance; the Complete* commands/responses, GetCurrentSession, and
// GetAgentIdentity are the shapes worth pinning (typed-id ctor Guard, optional property, nullable
// typed-id, list property, enum properties).
// GetCredentials_Response_Should.Never_Serializes_Handle_Or_PublicMaterial (task 104-005) is the
// contract-level half of a two-layer pin — a reflection-based structural assertion (CredentialSummary
// has no Handle/PublicMaterial property, so a Label containing "handle" cannot false-fail it — round-1
// review M4) plus a wire-level json.ShouldNotContain as belt-and-suspenders, proving the promise
// survives actual JSON serialization, not just the C# type shape. The integration-level twin
// (Credential_List_Tests.cs's Never_Serializes_Handle_Or_PublicMaterial) proves the same two-layer
// promise survives the whole generated-endpoint pipeline too.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;
using TimeWarp.Identity;

public class CompletePasskeyRegistration_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CompletePasskeyRegistration_Command_Should>();

  public static Task SerializeAndDeserialize()
  {
    CompletePasskeyRegistration.Command command = new()
    {
      CredentialId = "AQIDBA",
      ClientDataJson = "eyJ0eXBlIjoid2ViYXV0aG4uY3JlYXRlIn0",
      AttestationObject = "o2NmbXRkbm9uZQ"
    };

    CompletePasskeyRegistration.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.CredentialId.ShouldBe(command.CredentialId);
    parsed.ClientDataJson.ShouldBe(command.ClientDataJson);
    parsed.AttestationObject.ShouldBe(command.AttestationObject);
    return Task.CompletedTask;
  }
}

public class CompletePasskeyRegistration_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CompletePasskeyRegistration_Response_Should>();

  public static Task SerializeAndDeserialize_Via_Constructor()
  {
    CompletePasskeyRegistration.Response response = new(PrincipalId.New());

    CompletePasskeyRegistration.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.PrincipalId.ShouldBe(response.PrincipalId);
    return Task.CompletedTask;
  }

  public static Task Reject_EmptyPrincipalId_During_Deserialization()
  {
    // PrincipalId's own [TypedId] JsonConverter fail-closes on an empty guid before the Response
    // ctor's Guard even runs — either seam rejecting it is the contract that matters here.
    string json = """{"principalId":"00000000-0000-0000-0000-000000000000"}""";

    Should.Throw<Exception>(() =>
      JsonSerializer.Deserialize<CompletePasskeyRegistration.Response>(json, ContractSerialization.Options));
    return Task.CompletedTask;
  }
}

public class CompletePasskeyAuthentication_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CompletePasskeyAuthentication_Command_Should>();

  public static Task SerializeAndDeserialize_Including_Optional_UserHandle()
  {
    CompletePasskeyAuthentication.Command command = new()
    {
      CredentialId = "AQIDBA",
      ClientDataJson = "eyJ0eXBlIjoid2ViYXV0aG4uZ2V0In0",
      AuthenticatorData = "AQIDBA",
      Signature = "AQIDBA",
      UserHandle = "dXNlckhhbmRsZQ"
    };

    CompletePasskeyAuthentication.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.CredentialId.ShouldBe(command.CredentialId);
    parsed.ClientDataJson.ShouldBe(command.ClientDataJson);
    parsed.AuthenticatorData.ShouldBe(command.AuthenticatorData);
    parsed.Signature.ShouldBe(command.Signature);
    parsed.UserHandle.ShouldBe(command.UserHandle);
    return Task.CompletedTask;
  }

  public static Task SerializeAndDeserialize_Without_UserHandle()
  {
    CompletePasskeyAuthentication.Command command = new()
    {
      CredentialId = "AQIDBA",
      ClientDataJson = "eyJ0eXBlIjoid2ViYXV0aG4uZ2V0In0",
      AuthenticatorData = "AQIDBA",
      Signature = "AQIDBA"
    };

    CompletePasskeyAuthentication.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.UserHandle.ShouldBeNull();
    return Task.CompletedTask;
  }
}

public class GetCurrentSession_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetCurrentSession_Response_Should>();

  public static Task SerializeAndDeserialize_Authenticated()
  {
    GetCurrentSession.Response response = new(isAuthenticated: true, PrincipalId.New());

    GetCurrentSession.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.IsAuthenticated.ShouldBeTrue();
    parsed.PrincipalId.ShouldBe(response.PrincipalId);
    return Task.CompletedTask;
  }

  public static Task SerializeAndDeserialize_Unauthenticated()
  {
    GetCurrentSession.Response response = new(isAuthenticated: false, principalId: null);

    GetCurrentSession.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.IsAuthenticated.ShouldBeFalse();
    parsed.PrincipalId.ShouldBeNull();
    return Task.CompletedTask;
  }
}

public class CompleteAgentKeyRegistration_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CompleteAgentKeyRegistration_Command_Should>();

  public static Task SerializeAndDeserialize_Including_Optional_Label()
  {
    CompleteAgentKeyRegistration.Command command = new()
    {
      PublicKey = "AQIDBA",
      Challenge = "BQYHCA",
      Signature = "CQoLDA",
      Label = "prod-worker-3"
    };

    CompleteAgentKeyRegistration.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.PublicKey.ShouldBe(command.PublicKey);
    parsed.Challenge.ShouldBe(command.Challenge);
    parsed.Signature.ShouldBe(command.Signature);
    parsed.Label.ShouldBe(command.Label);
    return Task.CompletedTask;
  }

  public static Task SerializeAndDeserialize_Without_Label()
  {
    CompleteAgentKeyRegistration.Command command = new()
    {
      PublicKey = "AQIDBA",
      Challenge = "BQYHCA",
      Signature = "CQoLDA"
    };

    CompleteAgentKeyRegistration.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.Label.ShouldBeNull();
    return Task.CompletedTask;
  }
}

public class CompleteAgentKeyRegistration_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CompleteAgentKeyRegistration_Response_Should>();

  public static Task SerializeAndDeserialize_Via_Constructor()
  {
    CompleteAgentKeyRegistration.Response response = new(PrincipalId.New(), "a1b2c3");

    CompleteAgentKeyRegistration.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.PrincipalId.ShouldBe(response.PrincipalId);
    parsed.KeyId.ShouldBe(response.KeyId);
    return Task.CompletedTask;
  }

  public static Task Reject_EmptyPrincipalId_During_Deserialization()
  {
    string json = """{"principalId":"00000000-0000-0000-0000-000000000000","keyId":"a1b2c3"}""";

    Should.Throw<Exception>(() =>
      JsonSerializer.Deserialize<CompleteAgentKeyRegistration.Response>(json, ContractSerialization.Options));
    return Task.CompletedTask;
  }
}

public class CompleteAgentTokenIssuance_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CompleteAgentTokenIssuance_Command_Should>();

  public static Task SerializeAndDeserialize_Scopes_List()
  {
    CompleteAgentTokenIssuance.Command command = new()
    {
      KeyId = "a1b2c3",
      Challenge = "BQYHCA",
      Signature = "CQoLDA",
      Scopes = [AgentScopes.IdentityRead, AgentScopes.DemoInvoke]
    };

    CompleteAgentTokenIssuance.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.KeyId.ShouldBe(command.KeyId);
    parsed.Challenge.ShouldBe(command.Challenge);
    parsed.Signature.ShouldBe(command.Signature);
    parsed.Scopes.ShouldBe(command.Scopes);
    return Task.CompletedTask;
  }
}

public class CompleteAgentTokenIssuance_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CompleteAgentTokenIssuance_Response_Should>();

  public static Task SerializeAndDeserialize_Via_Constructor()
  {
    CompleteAgentTokenIssuance.Response response = new
    (
      accessToken: "opaque-token-value",
      expiresInSeconds: 900,
      scopes: [AgentScopes.IdentityRead],
      principalId: PrincipalId.New()
    );

    CompleteAgentTokenIssuance.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.AccessToken.ShouldBe(response.AccessToken);
    parsed.TokenType.ShouldBe("Bearer");
    parsed.ExpiresInSeconds.ShouldBe(900);
    parsed.Scopes.ShouldBe(response.Scopes);
    parsed.PrincipalId.ShouldBe(response.PrincipalId);
    return Task.CompletedTask;
  }
}

public class GetAgentIdentity_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetAgentIdentity_Response_Should>();

  public static Task SerializeAndDeserialize_Via_Constructor()
  {
    GetAgentIdentity.Response response = new
    (
      principalId: PrincipalId.New(),
      kind: PrincipalKind.Agent,
      trustTier: TrustTier.Keyed,
      scopes: [AgentScopes.IdentityRead]
    );

    GetAgentIdentity.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.PrincipalId.ShouldBe(response.PrincipalId);
    parsed.Kind.ShouldBe(PrincipalKind.Agent);
    parsed.TrustTier.ShouldBe(TrustTier.Keyed);
    parsed.Scopes.ShouldBe(response.Scopes);
    return Task.CompletedTask;
  }

  public static Task Serializes_Enums_As_PascalCase_Strings()
  {
    GetAgentIdentity.Response response = new
    (
      principalId: PrincipalId.New(),
      kind: PrincipalKind.Agent,
      trustTier: TrustTier.Keyed,
      scopes: [AgentScopes.IdentityRead]
    );

    string json = JsonSerializer.Serialize(response, ContractSerialization.Options);

    json.ShouldContain("\"kind\":\"Agent\"");
    json.ShouldContain("\"trustTier\":\"Keyed\"");
    json.ShouldNotContain("\"kind\":2");
    json.ShouldNotContain("\"trustTier\":2");
    return Task.CompletedTask;
  }

  public static Task Reject_Unknown_Kind_String()
  {
    string json =
      """{"principalId":"019f6a8b-0000-7000-8000-000000000001","kind":"NotAKind","trustTier":"Keyed","scopes":[]}""";

    Should.Throw<JsonException>(() =>
      JsonSerializer.Deserialize<GetAgentIdentity.Response>(json, ContractSerialization.Options));
    return Task.CompletedTask;
  }

  public static Task Reject_Integer_Kind()
  {
    string json =
      """{"principalId":"019f6a8b-0000-7000-8000-000000000001","kind":2,"trustTier":"Keyed","scopes":[]}""";

    Should.Throw<JsonException>(() =>
      JsonSerializer.Deserialize<GetAgentIdentity.Response>(json, ContractSerialization.Options));
    return Task.CompletedTask;
  }

  public static Task Accepts_Lowercase_Kind_String_Case_Insensitive_Read()
  {
    // JsonStringEnumConverter deserializes case-insensitively; wire emission is still PascalCase
    // ("Agent"). Fail-closed targets integers and unknown names, not case variants.
    string json =
      """{"principalId":"019f6a8b-0000-7000-8000-000000000001","kind":"agent","trustTier":"Keyed","scopes":[]}""";

    GetAgentIdentity.Response? parsed =
      JsonSerializer.Deserialize<GetAgentIdentity.Response>(json, ContractSerialization.Options);

    parsed.ShouldNotBeNull();
    parsed.Kind.ShouldBe(PrincipalKind.Agent);
    return Task.CompletedTask;
  }
}

public class CredentialType_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CredentialType_Should>();

  public static Task RoundTrip_Passkey_And_AgentKey_As_Strings()
  {
    string passkeyJson = JsonSerializer.Serialize(CredentialType.Passkey, ContractSerialization.Options);
    string agentKeyJson = JsonSerializer.Serialize(CredentialType.AgentKey, ContractSerialization.Options);

    passkeyJson.ShouldBe("\"Passkey\"");
    agentKeyJson.ShouldBe("\"AgentKey\"");

    JsonSerializer.Deserialize<CredentialType>(passkeyJson, ContractSerialization.Options)
      .ShouldBe(CredentialType.Passkey);
    JsonSerializer.Deserialize<CredentialType>(agentKeyJson, ContractSerialization.Options)
      .ShouldBe(CredentialType.AgentKey);
    return Task.CompletedTask;
  }
}

public class GetCredentials_Query_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetCredentials_Query_Should>();

  public static Task SerializeAndDeserialize_Including_Generated_RouteProperty()
  {
    GetCredentials.Query query = new() { UserId = Guid.NewGuid(), IncludeRevoked = true };

    GetCredentials.Query parsed = ContractSerialization.RoundTrip(query);

    parsed.UserId.ShouldBe(query.UserId);
    parsed.IncludeRevoked.ShouldBeTrue();
    return Task.CompletedTask;
  }
}

public class GetCredentials_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<GetCredentials_Response_Should>();

  public static Task SerializeAndDeserialize_Via_Constructor()
  {
    GetCredentials.Response response = new
    (
      [
        new GetCredentials.CredentialSummary
        (
          CredentialId.New(), CredentialType.Passkey, "laptop",
          DateTimeOffset.UtcNow.AddDays(-10), revokedAt: null, isActive: true
        ),
        new GetCredentials.CredentialSummary
        (
          CredentialId.New(), CredentialType.AgentKey, label: null,
          DateTimeOffset.UtcNow.AddDays(-5), revokedAt: DateTimeOffset.UtcNow, isActive: false
        )
      ]
    );

    GetCredentials.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.Credentials.Count.ShouldBe(2);
    parsed.Credentials[0].Id.ShouldBe(response.Credentials[0].Id);
    parsed.Credentials[0].Type.ShouldBe(CredentialType.Passkey);
    parsed.Credentials[0].Label.ShouldBe("laptop");
    parsed.Credentials[0].IsActive.ShouldBeTrue();
    parsed.Credentials[0].RevokedAt.ShouldBeNull();
    parsed.Credentials[1].Label.ShouldBeNull();
    parsed.Credentials[1].IsActive.ShouldBeFalse();
    parsed.Credentials[1].RevokedAt.ShouldNotBeNull();
    return Task.CompletedTask;
  }

  // Load-bearing security pin (task 104-005): CredentialSummary structurally omits
  // Handle/PublicMaterial — this proves that holds through actual JSON serialization, not just the
  // C# type shape. See this file's Design region for the second (integration-level) half of this pin.
  public static Task Never_Serializes_Handle_Or_PublicMaterial()
  {
    // Structural check FIRST (round-1 review M4) — the real guarantee, and the only one that cannot
    // false-fail on Label content: CredentialSummary itself has no Handle/PublicMaterial member.
    string[] propertyNames = typeof(GetCredentials.CredentialSummary).GetProperties().Select(p => p.Name).ToArray();
    propertyNames.ShouldNotContain(nameof(Credential.Handle));
    propertyNames.ShouldNotContain(nameof(Credential.PublicMaterial));

    GetCredentials.Response response = new
    (
      [new GetCredentials.CredentialSummary(CredentialId.New(), CredentialType.Passkey, "laptop", DateTimeOffset.UtcNow, revokedAt: null, isActive: true)]
    );

    string json = JsonSerializer.Serialize(response, ContractSerialization.Options);

    // Wire-level check SECOND, belt-and-suspenders — matches Credential_List_Tests.cs's
    // integration-level twin.
    json.ToLowerInvariant().ShouldNotContain("handle");
    json.ToLowerInvariant().ShouldNotContain("publicmaterial");
    return Task.CompletedTask;
  }
}

public class RevokeCredential_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RevokeCredential_Command_Should>();

  public static Task SerializeAndDeserialize_Including_Generated_RouteProperty()
  {
    RevokeCredential.Command command = new() { UserId = Guid.NewGuid(), CredentialId = Guid.NewGuid() };

    RevokeCredential.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.UserId.ShouldBe(command.UserId);
    parsed.CredentialId.ShouldBe(command.CredentialId);
    return Task.CompletedTask;
  }
}

public class AddPasskey_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AddPasskey_Command_Should>();

  public static Task SerializeAndDeserialize_Including_Optional_Label()
  {
    AddPasskey.Command command = new()
    {
      UserId = Guid.NewGuid(),
      CredentialId = "AQIDBA",
      ClientDataJson = "eyJ0eXBlIjoid2ViYXV0aG4uY3JlYXRlIn0",
      AttestationObject = "o2NmbXRkbm9uZQ",
      Label = "MacBook"
    };

    AddPasskey.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.CredentialId.ShouldBe(command.CredentialId);
    parsed.ClientDataJson.ShouldBe(command.ClientDataJson);
    parsed.AttestationObject.ShouldBe(command.AttestationObject);
    parsed.Label.ShouldBe("MacBook");
    return Task.CompletedTask;
  }

  public static Task SerializeAndDeserialize_Without_Label()
  {
    AddPasskey.Command command = new()
    {
      UserId = Guid.NewGuid(),
      CredentialId = "AQIDBA",
      ClientDataJson = "eyJ0eXBlIjoid2ViYXV0aG4uY3JlYXRlIn0",
      AttestationObject = "o2NmbXRkbm9uZQ"
    };

    AddPasskey.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.Label.ShouldBeNull();
    return Task.CompletedTask;
  }
}

public class AddPasskey_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AddPasskey_Response_Should>();

  public static Task SerializeAndDeserialize_Via_Constructor()
  {
    AddPasskey.Response response = new(CredentialId.New());

    AddPasskey.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.CredentialId.ShouldBe(response.CredentialId);
    return Task.CompletedTask;
  }

  public static Task Reject_EmptyCredentialId_During_Deserialization()
  {
    string json = """{"credentialId":"00000000-0000-0000-0000-000000000000"}""";

    Should.Throw<Exception>(() =>
      JsonSerializer.Deserialize<AddPasskey.Response>(json, ContractSerialization.Options));
    return Task.CompletedTask;
  }
}

public class AddAgentKey_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AddAgentKey_Command_Should>();

  public static Task SerializeAndDeserialize_Including_Optional_Label()
  {
    AddAgentKey.Command command = new()
    {
      UserId = Guid.NewGuid(),
      PublicKey = "AQIDBA",
      Challenge = "BQYHCA",
      Signature = "CQoLDA",
      Label = "prod-worker-4"
    };

    AddAgentKey.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.PublicKey.ShouldBe(command.PublicKey);
    parsed.Challenge.ShouldBe(command.Challenge);
    parsed.Signature.ShouldBe(command.Signature);
    parsed.Label.ShouldBe("prod-worker-4");
    return Task.CompletedTask;
  }

  public static Task SerializeAndDeserialize_Without_Label()
  {
    AddAgentKey.Command command = new()
    {
      UserId = Guid.NewGuid(),
      PublicKey = "AQIDBA",
      Challenge = "BQYHCA",
      Signature = "CQoLDA"
    };

    AddAgentKey.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.Label.ShouldBeNull();
    return Task.CompletedTask;
  }
}

public class AddAgentKey_Response_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AddAgentKey_Response_Should>();

  public static Task SerializeAndDeserialize_Via_Constructor()
  {
    AddAgentKey.Response response = new(CredentialId.New(), "a1b2c3");

    AddAgentKey.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.CredentialId.ShouldBe(response.CredentialId);
    parsed.KeyId.ShouldBe(response.KeyId);
    return Task.CompletedTask;
  }

  public static Task Reject_EmptyCredentialId_During_Deserialization()
  {
    string json = """{"credentialId":"00000000-0000-0000-0000-000000000000","keyId":"a1b2c3"}""";

    Should.Throw<Exception>(() =>
      JsonSerializer.Deserialize<AddAgentKey.Response>(json, ContractSerialization.Options));
    return Task.CompletedTask;
  }
}
