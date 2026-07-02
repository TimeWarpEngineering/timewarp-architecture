#region Purpose
// Client-side authentication/session state for the signed-in account.
#endregion

#region Design
// Setters are private: mutation happens only through action-set handlers in sibling partial
// files, per TimeWarp.State conventions.
// Initialize resets only the session/authentication fields; a test-only seeding overload and
// DevTools hydration live in account-state.debug.cs.
#endregion

#nullable enable
namespace TimeWarp.Architecture.Features.Account;

[StateAccess]
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
