#region Purpose
// Round-trip tests for the identity feature contract GetAgentIdentity_Response (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;
using TimeWarp.Identity;

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
    const string json =
      """{"principalId":"019f6a8b-0000-7000-8000-000000000001","kind":"NotAKind","trustTier":"Keyed","scopes":[]}""";

    Should.Throw<JsonException>(() =>
      JsonSerializer.Deserialize<GetAgentIdentity.Response>(json, ContractSerialization.Options));
    return Task.CompletedTask;
  }

  public static Task Reject_Integer_Kind()
  {
    const string json =
      """{"principalId":"019f6a8b-0000-7000-8000-000000000001","kind":2,"trustTier":"Keyed","scopes":[]}""";

    Should.Throw<JsonException>(() =>
      JsonSerializer.Deserialize<GetAgentIdentity.Response>(json, ContractSerialization.Options));
    return Task.CompletedTask;
  }

  public static Task Accepts_Lowercase_Kind_String_Case_Insensitive_Read()
  {
    // JsonStringEnumConverter deserializes case-insensitively; wire emission is still PascalCase
    // ("Agent"). Fail-closed targets integers and unknown names, not case variants.
    const string json =
      """{"principalId":"019f6a8b-0000-7000-8000-000000000001","kind":"agent","trustTier":"Keyed","scopes":[]}""";

    GetAgentIdentity.Response? parsed =
      JsonSerializer.Deserialize<GetAgentIdentity.Response>(json, ContractSerialization.Options);

    parsed.ShouldNotBeNull();
    parsed.Kind.ShouldBe(PrincipalKind.Agent);
    return Task.CompletedTask;
  }
}
