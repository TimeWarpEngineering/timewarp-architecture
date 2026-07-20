#region Purpose
// Authentication material bound to a principal: passkey or agent key with handle, public material, and revoke lifecycle.
#endregion

#region Design
// Multi-credential by model: many Credential rows per PrincipalId (list/revoke APIs come later). Handle is the lookup key
// (credential id / key id); PublicMaterial is verification material (COSE/public key). Create copies inputs; getters return
// fresh copies so callers cannot mutate stored material (D8: keep byte[] copy-on-get for Wave 1).
// Empty PrincipalId rejected at Create. CredentialType.None rejected. Id is CredentialId (RFC D3), not raw Guid.
// Type and Handle are immutable after Create — store Update replaces by Id only (revoke / label persistence); no handle migration.
// Revoke is one-shot. Clocks (D5, closed 104-006): CreatedAt/RevokedAt remain wall-clock
// DateTimeOffset with fuzzy tests; ceremony challenge/token stores already take optional
// TimeProvider. Full TimeProvider on domain entities is not required for the Wave 1 gate.
//
// Concurrency (task 104-028, supersedes D6 last-write-wins): Credential inherits Entity<CredentialId>
// — typed Id, identity-based (type+Id) equality, and the store-owned Version optimistic-concurrency
// token — instead of declaring its own Id property. Version is not settable by domain code; Create
// always mints Version 0. The private constructor is copy-free by design — HandleField/
// PublicMaterialField are stored as-is, not defensively copied — so every caller of the ctor (Create,
// Snapshot) is individually responsible for passing arrays it already owns; Create does this via
// `handle.ToArray()`/`publicMaterial.ToArray()` on the caller-supplied input, and Snapshot does the
// same on its OWN already-stored fields, so a store's rehydrated copy shares no array with the
// original instance it snapshotted (this is what keeps D8's "byte[] copy-on-get" guarantee intact
// across the concurrency-token seam — dropping either ToArray would let a caller mutate storage
// through a byte[] reference).
// Snapshot copies RevokedAt too — an incomplete snapshot (e.g. dropping RevokedAt) would silently
// let a revoked credential rehydrate as active, which is exactly the state-loss class this token
// exists to prevent.
//
// IAggregateRoot: deliberately NOT implemented here, for the same reason as Principal — see
// principal.cs's Design region. Identity's own guard clauses (Create, Revoke) are the invariant
// enforcement; aligning with the nested-Invariants/IAggregateRoot pattern is a later task.
#endregion

namespace TimeWarp.Identity;

public sealed class Credential : Entity<CredentialId>
{
  private readonly byte[] HandleField;
  private readonly byte[] PublicMaterialField;

  private Credential(
    CredentialId id,
    PrincipalId principalId,
    CredentialType type,
    byte[] handle,
    byte[] publicMaterial,
    DateTimeOffset createdAt,
    DateTimeOffset? revokedAt,
    string? label,
    long version)
    : base(id, version)
  {
    PrincipalId = principalId;
    Type = type;
    HandleField = handle;
    PublicMaterialField = publicMaterial;
    CreatedAt = createdAt;
    RevokedAt = revokedAt;
    Label = label;
  }

  public PrincipalId PrincipalId { get; }
  public CredentialType Type { get; }

#pragma warning disable CA1819 // Binary material is intentionally exposed as byte[] copies
  public byte[] Handle => HandleField.ToArray();
  public byte[] PublicMaterial => PublicMaterialField.ToArray();
#pragma warning restore CA1819

  public DateTimeOffset CreatedAt { get; }
  public DateTimeOffset? RevokedAt { get; private set; }
  public string? Label { get; }
  public bool IsRevoked => RevokedAt is not null;

  public static Credential Create(
    PrincipalId principalId,
    CredentialType type,
    byte[] handle,
    byte[] publicMaterial,
    string? label = null)
  {
    ArgumentNullException.ThrowIfNull(handle);
    ArgumentNullException.ThrowIfNull(publicMaterial);

    if (principalId.IsEmpty)
    {
      throw new ArgumentException("PrincipalId cannot be empty.", nameof(principalId));
    }

    if (!Enum.IsDefined(type) || type == CredentialType.None)
    {
      throw new ArgumentOutOfRangeException(nameof(type), type, "CredentialType must be a defined non-None value.");
    }

    if (handle.Length == 0)
    {
      throw new ArgumentException("Handle must be non-empty.", nameof(handle));
    }

    if (publicMaterial.Length == 0)
    {
      throw new ArgumentException("PublicMaterial must be non-empty.", nameof(publicMaterial));
    }

    string? normalizedLabel = NormalizeLabel(label);

    return new Credential(
      CredentialId.New(),
      principalId,
      type,
      handle.ToArray(),
      publicMaterial.ToArray(),
      DateTimeOffset.UtcNow,
      revokedAt: null,
      normalizedLabel,
      version: 0);
  }

  /// <summary>
  /// Store-only rehydration: copies already-valid state at a specific version — no id minting, no
  /// re-validation, and fresh byte[] copies (preserves D8) so the snapshot shares no storage with
  /// this instance. Internal because it must only ever be called on an existing Credential instance
  /// that already passed Create's guards.
  /// </summary>
  internal Credential Snapshot(long version) =>
    new(Id, PrincipalId, Type, HandleField.ToArray(), PublicMaterialField.ToArray(), CreatedAt, RevokedAt, Label, version);

  public void Revoke()
  {
    if (RevokedAt is not null)
    {
      throw new InvalidOperationException("Credential is already revoked.");
    }

    RevokedAt = DateTimeOffset.UtcNow;
  }

  private static string? NormalizeLabel(string? label)
  {
    if (label is null)
    {
      return null;
    }

    string trimmed = label.Trim();
    return trimmed.Length == 0 ? null : trimmed;
  }
}
