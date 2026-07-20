#region Purpose
// Round-trip tests for the identity feature contracts (tasks 104-003, 104-004) — the shapes where
// serialization can actually diverge: typed-id (PrincipalId) Responses with a ctor Guard,
// optional-property Commands, list properties, and enum properties (PrincipalKind/TrustTier).
#endregion

#region Design
// StartPasskeyRegistration/StartPasskeyAuthentication/StartAgentKeyRegistration/
// StartAgentTokenIssuance's empty Command bodies and their Response's single-string-property shape
// are plain auto-property POCOs — no test here per the skill's "trivial auto-property POCOs are
// deliberately not written" guidance; the Complete* commands/responses, GetCurrentSession, and
// GetAgentIdentity are the shapes worth pinning (typed-id ctor Guard, optional property, nullable
// typed-id, list property, enum properties).
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;
using TimeWarp.Identity;

public class CompletePasskeyRegistration_Command_Should
{
  public static void SerializeAndDeserialize()
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
  }
}

public class CompletePasskeyRegistration_Response_Should
{
  public static void SerializeAndDeserialize_Via_Constructor()
  {
    CompletePasskeyRegistration.Response response = new(PrincipalId.New());

    CompletePasskeyRegistration.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.PrincipalId.ShouldBe(response.PrincipalId);
  }

  public static void Reject_EmptyPrincipalId_During_Deserialization()
  {
    // PrincipalId's own [TypedId] JsonConverter fail-closes on an empty guid before the Response
    // ctor's Guard even runs — either seam rejecting it is the contract that matters here.
    string json = """{"principalId":"00000000-0000-0000-0000-000000000000"}""";

    Should.Throw<Exception>(() =>
      JsonSerializer.Deserialize<CompletePasskeyRegistration.Response>(json, ContractSerialization.Options));
  }
}

public class CompletePasskeyAuthentication_Command_Should
{
  public static void SerializeAndDeserialize_Including_Optional_UserHandle()
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
  }

  public static void SerializeAndDeserialize_Without_UserHandle()
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
  }
}

public class GetCurrentSession_Response_Should
{
  public static void SerializeAndDeserialize_Authenticated()
  {
    GetCurrentSession.Response response = new(isAuthenticated: true, PrincipalId.New());

    GetCurrentSession.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.IsAuthenticated.ShouldBeTrue();
    parsed.PrincipalId.ShouldBe(response.PrincipalId);
  }

  public static void SerializeAndDeserialize_Unauthenticated()
  {
    GetCurrentSession.Response response = new(isAuthenticated: false, principalId: null);

    GetCurrentSession.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.IsAuthenticated.ShouldBeFalse();
    parsed.PrincipalId.ShouldBeNull();
  }
}

public class CompleteAgentKeyRegistration_Command_Should
{
  public static void SerializeAndDeserialize_Including_Optional_Label()
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
  }

  public static void SerializeAndDeserialize_Without_Label()
  {
    CompleteAgentKeyRegistration.Command command = new()
    {
      PublicKey = "AQIDBA",
      Challenge = "BQYHCA",
      Signature = "CQoLDA"
    };

    CompleteAgentKeyRegistration.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.Label.ShouldBeNull();
  }
}

public class CompleteAgentKeyRegistration_Response_Should
{
  public static void SerializeAndDeserialize_Via_Constructor()
  {
    CompleteAgentKeyRegistration.Response response = new(PrincipalId.New(), "a1b2c3");

    CompleteAgentKeyRegistration.Response parsed = ContractSerialization.RoundTrip(response);

    parsed.PrincipalId.ShouldBe(response.PrincipalId);
    parsed.KeyId.ShouldBe(response.KeyId);
  }

  public static void Reject_EmptyPrincipalId_During_Deserialization()
  {
    string json = """{"principalId":"00000000-0000-0000-0000-000000000000","keyId":"a1b2c3"}""";

    Should.Throw<Exception>(() =>
      JsonSerializer.Deserialize<CompleteAgentKeyRegistration.Response>(json, ContractSerialization.Options));
  }
}

public class CompleteAgentTokenIssuance_Command_Should
{
  public static void SerializeAndDeserialize_Scopes_List()
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
  }
}

public class CompleteAgentTokenIssuance_Response_Should
{
  public static void SerializeAndDeserialize_Via_Constructor()
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
  }
}

public class GetAgentIdentity_Response_Should
{
  public static void SerializeAndDeserialize_Via_Constructor()
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
  }

  public static void Serializes_Enums_As_PascalCase_Strings()
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
  }

  public static void Reject_Unknown_Kind_String()
  {
    string json =
      """{"principalId":"019f6a8b-0000-7000-8000-000000000001","kind":"NotAKind","trustTier":"Keyed","scopes":[]}""";

    Should.Throw<JsonException>(() =>
      JsonSerializer.Deserialize<GetAgentIdentity.Response>(json, ContractSerialization.Options));
  }

  public static void Reject_Integer_Kind()
  {
    string json =
      """{"principalId":"019f6a8b-0000-7000-8000-000000000001","kind":2,"trustTier":"Keyed","scopes":[]}""";

    Should.Throw<JsonException>(() =>
      JsonSerializer.Deserialize<GetAgentIdentity.Response>(json, ContractSerialization.Options));
  }

  public static void Accepts_Lowercase_Kind_String_Case_Insensitive_Read()
  {
    // JsonStringEnumConverter deserializes case-insensitively; wire emission is still PascalCase
    // ("Agent"). Fail-closed targets integers and unknown names, not case variants.
    string json =
      """{"principalId":"019f6a8b-0000-7000-8000-000000000001","kind":"agent","trustTier":"Keyed","scopes":[]}""";

    GetAgentIdentity.Response? parsed =
      JsonSerializer.Deserialize<GetAgentIdentity.Response>(json, ContractSerialization.Options);

    parsed.ShouldNotBeNull();
    parsed.Kind.ShouldBe(PrincipalKind.Agent);
  }
}

public class CredentialType_Should
{
  public static void RoundTrip_Passkey_And_AgentKey_As_Strings()
  {
    string passkeyJson = JsonSerializer.Serialize(CredentialType.Passkey, ContractSerialization.Options);
    string agentKeyJson = JsonSerializer.Serialize(CredentialType.AgentKey, ContractSerialization.Options);

    passkeyJson.ShouldBe("\"Passkey\"");
    agentKeyJson.ShouldBe("\"AgentKey\"");

    JsonSerializer.Deserialize<CredentialType>(passkeyJson, ContractSerialization.Options)
      .ShouldBe(CredentialType.Passkey);
    JsonSerializer.Deserialize<CredentialType>(agentKeyJson, ContractSerialization.Options)
      .ShouldBe(CredentialType.AgentKey);
  }
}
