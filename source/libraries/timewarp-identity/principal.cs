#region Purpose
// Domain aggregate for an identity subject: kind, trust progression, quarantine risk flag, optional display name.
#endregion

#region Design
// Hybrid model: PrincipalId is server-minted; credentials attach separately (1:N). Profile/email/password are not required
// and may land later as optional progressive profile. Agent principals need no linked human.
//
// Trust (RFC D1 C refined):
// - Progression is TrustTier (Provisional → Keyed → Funded → Established). Birth = Provisional, not Keyed.
// - Risk is orthogonal IsQuarantined (not a tier). Free SetTrustTier removed.
// - Promote(target) allows any strictly higher progression tier when not quarantined; rejects None/backward/same.
// - RecordCredentialAttached: first credential always promotes Provisional → Keyed (even if quarantined);
//   risk is orthogonal — ops use IsActive / IsFundedAndActive. Store calls this on add.
// - Named predicates (IsFundedAndActive, IsActive) so handlers never write tier >= Funded.
// - Rich PrincipalStatus taxonomy deferred until 008/013 write real gates.
//
// Clocks: DateTimeOffset.UtcNow at Create (D5 TimeProvider deferred to 104-006).
// Concurrency: no version token in Wave 1 (D6); hosts use last-write-wins via UpdatePrincipalAsync.
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
  public bool IsQuarantined { get; private set; }
  public DateTimeOffset CreatedAt { get; }
  public string? DisplayName { get; private set; }

  /// <summary>True when not quarantined — operational activity may proceed subject to tier gates.</summary>
  public bool IsActive => !IsQuarantined;

  /// <summary>True when funded-or-higher progression and not quarantined (never use ordinal tier comparisons).</summary>
  public bool IsFundedAndActive =>
    !IsQuarantined && TrustTier is TrustTier.Funded or TrustTier.Established;

  public static Principal Create(PrincipalKind kind)
  {
    if (!Enum.IsDefined(kind) || kind == PrincipalKind.None)
    {
      throw new ArgumentOutOfRangeException(nameof(kind), kind, "PrincipalKind must be a defined non-None value.");
    }

    return new Principal(
      PrincipalId.New(),
      kind,
      TrustTier.Provisional,
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

  /// <summary>
  /// Forward-only trust progression: any strictly higher progression tier is allowed when not quarantined.
  /// </summary>
  public void Promote(TrustTier target)
  {
    if (!Enum.IsDefined(target) || target == TrustTier.None)
    {
      throw new ArgumentOutOfRangeException(nameof(target), target, "TrustTier must be a defined non-None progression value.");
    }

    if (IsQuarantined)
    {
      throw new InvalidOperationException("Cannot promote a quarantined principal.");
    }

    if ((int)target <= (int)TrustTier)
    {
      throw new InvalidOperationException(
        $"Cannot promote from {TrustTier} to {target}; target must be a strictly higher progression tier.");
    }

    TrustTier = target;
  }

  /// <summary>
  /// First-credential attach rule: when Provisional, advances to Keyed (even if quarantined).
  /// Quarantine gates operations via <see cref="IsActive"/> / <see cref="IsFundedAndActive"/>, not progression.
  /// </summary>
  public void RecordCredentialAttached()
  {
    if (TrustTier == TrustTier.Provisional)
    {
      TrustTier = TrustTier.Keyed;
    }
  }

  public void Quarantine()
  {
    IsQuarantined = true;
  }

  public void ClearQuarantine()
  {
    IsQuarantined = false;
  }
}
