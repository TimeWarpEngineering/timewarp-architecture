#nullable enable
namespace TimeWarp.Architecture.Features.Account;

[StateAccessMixin]
public sealed partial class AccountState : State<AccountState>
{
  public string? Alias { get; private set; }
  public string? WalletAddress { get; private set; }
  public string? SessionToken { get; private set; }
  public bool IsAuthenticated { get; private set; }

  public AccountState() { }

  public override void Initialize()
  {
    SessionToken = null;
    IsAuthenticated = false;
  }
}
