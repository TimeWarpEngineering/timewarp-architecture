// ReSharper disable InconsistentNaming
namespace CliJson_;

public class WhoAmI_Wire_Shape
{
  public void Deserializes_numeric_kind_and_trust_tier()
  {
    var json = new CliJson();
    // Server STJ (ContractSerializationDefaults) emits enums as numbers, not strings.
    const string body = """
      {"principalId":"019f6a8b-0000-7000-8000-000000000001","kind":2,"trustTier":2,"scopes":["identity:read"]}
      """;

    WhoAmIResponse? me = json.Deserialize<WhoAmIResponse>(body);

    me.ShouldNotBeNull();
    me.PrincipalId.ShouldBe("019f6a8b-0000-7000-8000-000000000001");
    me.Kind.ShouldBe(PrincipalKind.Agent);
    me.TrustTier.ShouldBe(TrustTier.Keyed);
    me.Scopes.ShouldBe(["identity:read"]);
  }
}
