#region Purpose
// SetPendingNickname: marks a just-registered credential as awaiting its user nickname (prefilled with the provider name).
#endregion

#region Design
// Used by flows that complete a ceremony OUTSIDE CredentialsState (PasskeysPage's
// CompletePasskeyRegistration via PasskeyCeremonyClient). AddPasskey's own handler sets the same
// two fields directly. Pure state mutation — no HTTP. Task 248-001.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

partial class CredentialsState
{
  public static class SetPendingNicknameActionSet
  {
    public sealed class Action : IBaseAction
    {
      public Action(Guid credentialId, string providerLabel)
      {
        CredentialId = credentialId;
        // Generated dispatcher drops nullability; empty means "no provider name" (prefill falls back).
        ProviderLabel = string.IsNullOrWhiteSpace(providerLabel) ? null : providerLabel;
      }

      public Guid CredentialId { get; }
      public string? ProviderLabel { get; }
    }

    internal sealed class Handler : BaseHandler<Action>
    {
      public Handler(IStore store) : base(store)
      {
      }

      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        _ = cancellationToken;
        CredentialsState.PendingNicknameCredentialId = action.CredentialId;
        CredentialsState.PendingNicknameDefault = action.ProviderLabel;
        return ValueTask.CompletedTask;
      }
    }
  }
}
