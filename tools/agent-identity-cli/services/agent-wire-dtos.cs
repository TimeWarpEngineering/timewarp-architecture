#region Purpose
// CLI-local camelCase wire DTOs for the agent identity HTTP ceremony (no web-contracts ref).
#endregion
#region Design
// Mirrors the public JSON shapes of Start/Complete agent registration and token
// issuance plus GetAgentIdentity. principalId/keyId stay strings (typed-id STJ
// converters not required for this thin client). Kind/TrustTier use the Identity
// enums; CliJson registers JsonStringEnumConverter so wire values are PascalCase
// strings ("Agent", "Keyed"), not integers — matching ContractSerializationDefaults.
#endregion

namespace AgentIdentityCli.Services;

internal sealed class ChallengeResponse
{
  public string Challenge { get; set; } = string.Empty;
}

internal sealed class RegisterRequest
{
  public string PublicKey { get; set; } = string.Empty;
  public string Challenge { get; set; } = string.Empty;
  public string Signature { get; set; } = string.Empty;
  public string? Label { get; set; }
}

internal sealed class RegisterResponse
{
  public string PrincipalId { get; set; } = string.Empty;
  public string KeyId { get; set; } = string.Empty;
}

internal sealed class TokenRequest
{
  public string KeyId { get; set; } = string.Empty;
  public string Challenge { get; set; } = string.Empty;
  public string Signature { get; set; } = string.Empty;
  public List<string> Scopes { get; set; } = [];
}

internal sealed class TokenResponse
{
  public string AccessToken { get; set; } = string.Empty;
  public string TokenType { get; set; } = "Bearer";
  public int ExpiresInSeconds { get; set; }
  public List<string> Scopes { get; set; } = [];
  public string PrincipalId { get; set; } = string.Empty;
}

internal sealed class WhoAmIResponse
{
  public string PrincipalId { get; set; } = string.Empty;
  public PrincipalKind Kind { get; set; }
  public TrustTier TrustTier { get; set; }
  public List<string> Scopes { get; set; } = [];
}
