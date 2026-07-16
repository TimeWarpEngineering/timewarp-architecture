#region Purpose
// Stable principal identity: a non-empty Guid that never recycles and identifies a subject across credentials and sessions.
#endregion

#region Design
// Hybrid identity: the server mints PrincipalId (Guid v7) at first registration; passkeys/agent keys bind to it later.
// Wrapping Guid in a readonly record struct prevents accidental mix-ups with credential ids and other Guids at call sites.
// Empty is rejected — an unassigned id is not a principal.
#endregion

namespace TimeWarp.Identity;

public readonly record struct PrincipalId
{
  public Guid Value { get; }

  /// <summary>True when this is the default struct (never produced by <see cref="New"/> or <see cref="From"/>).</summary>
  public bool IsEmpty => Value == Guid.Empty;

  private PrincipalId(Guid value)
  {
    Value = value;
  }

  public static PrincipalId New() => new(Guid.CreateVersion7());

  public static PrincipalId From(Guid value)
  {
    if (value == Guid.Empty)
    {
      throw new ArgumentException("PrincipalId cannot be empty.", nameof(value));
    }

    return new PrincipalId(value);
  }

  public override string ToString() => Value.ToString();
}
