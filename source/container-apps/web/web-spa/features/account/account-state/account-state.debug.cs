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
