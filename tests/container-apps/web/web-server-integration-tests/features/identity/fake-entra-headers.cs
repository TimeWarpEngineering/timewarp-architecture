#region Purpose
// Header names the Entra challenge tests use to tell FakeEntraHandler which claims to mint.
#endregion

namespace EntraChallenge_;

internal static class FakeEntraHeaders
{
  public const string TenantId = "X-Test-Entra-Tid";
  public const string ObjectId = "X-Test-Entra-Oid";
  public const string Issuer = "X-Test-Entra-Iss";
  public const string OmitObjectId = "X-Test-Entra-Omit-Oid";
  public const string PreferredUsername = "X-Test-Entra-Preferred-Username";
  public const string Name = "X-Test-Entra-Name";
}
