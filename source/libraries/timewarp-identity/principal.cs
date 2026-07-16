#region Purpose
// Domain aggregate for an identity subject: kind, trust tier, optional display name — no registration-form fields required.
#endregion

#region Design
// Hybrid model: PrincipalId is server-minted; credentials attach separately (1:N). Profile/email/password are not required
// and may land later as optional progressive profile. Agent principals need no linked human. New principals start Keyed
// (credential path exists or is about to); Funded/Established/Quarantined are explicit tier transitions.
// CreatedAt uses DateTimeOffset for unambiguous UTC.
#endregion

namespace TimeWarp.Identity;

public sealed class Principal
{
  private Principal(PrincipalId id, PrincipalKind kind, TrustTier trustTier, DateTimeOffset createdAt)
  {
    Id = id;
    Kind = kind;
    TrustTier = trustTier;
    CreatedAt = createdAt;
  }

  public PrincipalId Id { get; }
  public PrincipalKind Kind { get; }
  public TrustTier TrustTier { get; private set; }
  public DateTimeOffset CreatedAt { get; }
  public string? DisplayName { get; private set; }

  public static Principal Create(PrincipalKind kind)
  {
    if (!Enum.IsDefined(kind))
    {
      throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown PrincipalKind value.");
    }

    return new Principal(
      PrincipalId.New(),
      kind,
      TrustTier.Keyed,
      DateTimeOffset.UtcNow);
  }

  public void SetDisplayName(string? displayName)
  {
    if (displayName is null)
    {
      DisplayName = null;
      return;
    }

    string trimmed = displayName.Trim();
    DisplayName = trimmed.Length == 0 ? null : trimmed;
  }

  public void SetTrustTier(TrustTier trustTier)
  {
    if (!Enum.IsDefined(trustTier))
    {
      throw new ArgumentOutOfRangeException(nameof(trustTier), trustTier, "Unknown TrustTier value.");
    }

    TrustTier = trustTier;
  }
}
