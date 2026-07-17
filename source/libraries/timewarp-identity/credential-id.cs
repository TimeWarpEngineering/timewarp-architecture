#region Purpose
// Stable credential identity: a non-empty Guid that never recycles and distinguishes one credential from another.
#endregion

#region Design
// Mirrors PrincipalId so call sites cannot mix principal Guids with credential Guids (RFC D3). Empty is rejected —
// an unassigned id is not a credential. New uses Guid v7 for time-sortable ids.
#endregion

namespace TimeWarp.Identity;

public readonly record struct CredentialId
{
  public Guid Value { get; }

  /// <summary>True when this is the default struct (never produced by <see cref="New"/> or <see cref="From"/>).</summary>
  public bool IsEmpty => Value == Guid.Empty;

  private CredentialId(Guid value)
  {
    Value = value;
  }

  public static CredentialId New() => new(Guid.CreateVersion7());

  public static CredentialId From(Guid value)
  {
    if (value == Guid.Empty)
    {
      throw new ArgumentException("CredentialId cannot be empty.", nameof(value));
    }

    return new CredentialId(value);
  }

  public override string ToString() => Value.ToString();
}
