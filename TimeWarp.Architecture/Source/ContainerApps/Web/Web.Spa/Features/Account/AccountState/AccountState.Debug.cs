# nullable enable
namespace TimeWarp.Architecture.Features.Account;

partial class AccountState
{
  public override AccountState Hydrate(IDictionary<string, object> aKeyValuePairs)
  {
    return new AccountState
    {
      Guid = new Guid(aKeyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Guid))].ToString()!),
      Alias = aKeyValuePairs[CamelCase.MemberNameToCamelCase(nameof(Alias))].ToString(),
      WalletAddress = aKeyValuePairs[CamelCase.MemberNameToCamelCase(nameof(WalletAddress))].ToString(),
      SessionToken = aKeyValuePairs[CamelCase.MemberNameToCamelCase(nameof(SessionToken))].ToString(),
      IsAuthenticated = bool.Parse(aKeyValuePairs[CamelCase.MemberNameToCamelCase(nameof(IsAuthenticated))].ToString()!),
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
