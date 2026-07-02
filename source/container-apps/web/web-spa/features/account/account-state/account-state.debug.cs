#region Purpose
// Debug/test support for AccountState: Redux DevTools rehydration and test-only seeding.
#endregion

#region Design
// Hydrate rebuilds state from the camelCased key/value payload Redux DevTools round-trips,
// enabling time-travel debugging.
// The Initialize overload bypasses the action pipeline so tests can seed state directly;
// ThrowIfNotTestAssembly blocks production callers from that shortcut.
#endregion

# nullable enable
namespace TimeWarp.Architecture.Features.Account;

partial class AccountState
{
  public override AccountState Hydrate(IDictionary<string, object> keyValuePairs)
  {
    return new AccountState
    {
      Guid = new Guid(keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Guid))].ToString()!),
      Alias = keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Alias))].ToString(),
      WalletAddress = keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(WalletAddress))].ToString(),
      SessionToken = keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(SessionToken))].ToString(),
      IsAuthenticated = bool.Parse(keyValuePairs[CamelCase.MemberNameToCamelCase(nameof(IsAuthenticated))].ToString()!),
    };
  }

  internal void Initialize(string? alias, string? walletAddress, string? sessionToken, bool isAuthenticated )
  {
    ThrowIfNotTestAssembly(Assembly.GetCallingAssembly());
    Alias = alias;
    WalletAddress = walletAddress;
    SessionToken = sessionToken;
    IsAuthenticated = isAuthenticated;
  }
}
