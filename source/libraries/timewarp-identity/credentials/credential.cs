#region Purpose
// Authentication material bound to a principal: passkey, agent key, or Entra account, with type-dependent verification material and revoke/restore lifecycle.
#endregion

#region Design
// Multi-credential by model: many Credential rows per PrincipalId (list/revoke APIs come later). Handle is the lookup key
// (credential id / key id / Entra tid:oid). PublicMaterial is type-dependent verification material: COSE/SPKI public key
// for Passkey/AgentKey; UTF-8 issuer URI (EntraIssuerMaterial) for EntraAccount only. Hosts must never feed EntraAccount
// rows into WebAuthnAuthentication.Verify or AgentKeyProof.Verify — those APIs consume cryptographic public keys, not
// issuer URIs. Create copies inputs; getters return fresh copies so callers cannot mutate stored material
// (D8: keep byte[] copy-on-get for Wave 1).
// Empty PrincipalId rejected at Create. CredentialType.None rejected. Id is CredentialId (RFC D3), not raw Guid.
// Type and Handle are immutable after Create — store Update replaces by Id only (revoke / restore / label persistence); no handle migration.
// ReparentTo mutates in-memory PrincipalId for MergePrincipalAsync's own snapshot/replace path;
// UpdateCredentialAsync rejects PrincipalId changes (re-parent only via MergePrincipalAsync).
// Type/Handle stay put so the authenticator's credential id still looks up.
// Revoke is one-shot (throws if already revoked). Restore is the one-shot inverse (throws if not revoked) so Graph
// re-enable can reuse the same (Type, Handle) row; unique (Type, Handle) plus Find-returns-revoked makes re-insert of
// the same tid:oid impossible. Clocks (D5, closed 104-006): CreatedAt/RevokedAt remain wall-clock
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
// Add-time identity (task 248-001): Label is the PROVIDER name (AAGUID map — "Proton Pass"; Entra
// display label) and is immutable; Nickname is the USER's name for the row, optional, mutable via
// Rename (1..MaxNicknameLength chars after trim). The two were one field before 248-001 (a
// caller-supplied label silently overwrote the provider name), which made same-provider rows
// indistinguishable once renamed. RegisteredWith is captured once at Create (attachment + browser/OS
// family; see its Design region) and never changes. Fingerprint is computed from HandleField on demand
// (CredentialFingerprint) — display-safe, never the handle itself. Snapshot copies Nickname and
// RegisteredWith too — the same "incomplete snapshot silently loses state" reasoning as RevokedAt.
// RegisteredWith is mirrored into three PRIVATE scalar properties (Registered{Attachment,Browser,Os})
// that the EF mapping binds by name through the private constructor — chosen over an owned/complex
// type so the store's detach-and-attach replacement path (PersistReplacementAsync) stays a
// single-row operation. (EF rejects field-only properties whose name differs from the field, so
// these are properties, not fields.)
//
// IAggregateRoot: deliberately NOT implemented here, for the same reason as Principal — see
// principal.cs's Design region. Identity's own guard clauses (Create, Revoke, Restore) are the invariant
// enforcement; aligning with the nested-Invariants/IAggregateRoot pattern is a later task.
#endregion

namespace TimeWarp.Identity;

/// <summary>
/// Authentication material bound to a principal — passkey, agent key, or Entra account — with revoke/restore lifecycle.
/// </summary>
public sealed class Credential : Entity<CredentialId>
{
  /// <summary>Upper bound for <see cref="Nickname"/> after trimming (matches the contract validator).</summary>
  public const int MaxNicknameLength = 64;

  private readonly byte[] HandleField;
  private readonly byte[] PublicMaterialField;

  // Private scalar projections of RegisteredWith so the EF mapping (private properties mapped by
  // name, see CredentialEntityTypeConfiguration) can persist three columns without an owned/complex
  // type. Never read by domain code — RegisteredWith is the public surface.
  private AuthenticatorAttachment RegisteredAttachment { get; }
  private string? RegisteredBrowser { get; }
  private string? RegisteredOs { get; }

  private Credential(
    CredentialId id,
    PrincipalId principalId,
    CredentialType type,
    byte[] handle,
    byte[] publicMaterial,
    DateTimeOffset createdAt,
    DateTimeOffset? revokedAt,
    string? label,
    string? nickname,
    AuthenticatorAttachment registeredAttachment,
    string? registeredBrowser,
    string? registeredOs,
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
    Nickname = nickname;
    RegisteredWith = new RegisteredWith(registeredAttachment, registeredBrowser, registeredOs);
    RegisteredAttachment = RegisteredWith.Attachment;
    RegisteredBrowser = RegisteredWith.Browser;
    RegisteredOs = RegisteredWith.Os;
  }

  /// <summary>Owning principal; mutable only via <see cref="ReparentTo"/> during merge.</summary>
  public PrincipalId PrincipalId { get; private set; }

  /// <summary>Immutable credential kind set at create time.</summary>
  public CredentialType Type { get; }

#pragma warning disable CA1819 // Binary material is intentionally exposed as byte[] copies
  /// <summary>Lookup key bytes (credential id / key id / tid:oid); defensive copy.</summary>
  public byte[] Handle => HandleField.ToArray();

  /// <summary>Type-dependent verification material (COSE/SPKI or Entra issuer URI); defensive copy.</summary>
  public byte[] PublicMaterial => PublicMaterialField.ToArray();
#pragma warning restore CA1819

  /// <summary>UTC create stamp minted by <see cref="Create"/>.</summary>
  public DateTimeOffset CreatedAt { get; }

  /// <summary>UTC revoke stamp when revoked; null while active.</summary>
  public DateTimeOffset? RevokedAt { get; private set; }

  /// <summary>Provider label (AAGUID / Entra display); immutable; null when unknown or whitespace-only at create.</summary>
  public string? Label { get; }

  /// <summary>User-chosen nickname; null until <see cref="Rename"/> or a nickname was supplied at create.</summary>
  public string? Nickname { get; private set; }

  /// <summary>Registration context captured at create; <see cref="RegisteredWith.Unknown"/> when nothing was captured.</summary>
  public RegisteredWith RegisteredWith { get; }

  /// <summary>Display-safe 8-hex discriminator derived from the handle (never the handle itself).</summary>
  public string Fingerprint => CredentialFingerprint.Compute(HandleField);

  /// <summary>True when <see cref="RevokedAt"/> is set.</summary>
  public bool IsRevoked => RevokedAt is not null;

  /// <summary>
  /// Mints a new credential at version 0 with defensive copies of handle and public material.
  /// </summary>
  public static Credential Create(
    PrincipalId principalId,
    CredentialType type,
    byte[] handle,
    byte[] publicMaterial,
    string? label = null,
    string? nickname = null,
    RegisteredWith? registeredWith = null)
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
    string? normalizedNickname = NormalizeNickname(nickname);
    RegisteredWith context = registeredWith ?? RegisteredWith.Unknown;

    return new Credential(
      CredentialId.New(),
      principalId,
      type,
      handle.ToArray(),
      publicMaterial.ToArray(),
      DateTimeOffset.UtcNow,
      revokedAt: null,
      normalizedLabel,
      normalizedNickname,
      context.Attachment,
      context.Browser,
      context.Os,
      version: 0);
  }

  /// <summary>
  /// Store-only rehydration: copies already-valid state at a specific version — no id minting, no
  /// re-validation, and fresh byte[] copies (preserves D8) so the snapshot shares no storage with
  /// this instance. Internal because it must only ever be called on an existing Credential instance
  /// that already passed Create's guards.
  /// </summary>
  internal Credential Snapshot(long version) =>
    new(
      Id,
      PrincipalId,
      Type,
      HandleField.ToArray(),
      PublicMaterialField.ToArray(),
      CreatedAt,
      RevokedAt,
      Label,
      Nickname,
      RegisteredWith.Attachment,
      RegisteredWith.Browser,
      RegisteredWith.Os,
      version);

  /// <summary>
  /// Sets the user-chosen nickname. Trimmed; must be 1..<see cref="MaxNicknameLength"/> characters.
  /// </summary>
  /// <exception cref="ArgumentException">The nickname is empty/whitespace or too long after trimming.</exception>
  public void Rename(string nickname)
  {
    ArgumentNullException.ThrowIfNull(nickname);

    string? normalized = NormalizeNickname(nickname);
    if (normalized is null)
    {
      throw new ArgumentException("Nickname must contain at least one non-whitespace character.", nameof(nickname));
    }

    Nickname = normalized;
  }

  /// <summary>One-shot revoke: sets <see cref="RevokedAt"/>; throws if already revoked.</summary>
  public void Revoke()
  {
    if (RevokedAt is not null)
    {
      throw new InvalidOperationException("Credential is already revoked.");
    }

    RevokedAt = DateTimeOffset.UtcNow;
  }

  /// <summary>
  /// One-shot inverse of <see cref="Revoke"/>: clears <see cref="RevokedAt"/> so the same
  /// (Type, Handle) row can be reused. Throws if the credential is not revoked.
  /// </summary>
  /// <exception cref="InvalidOperationException">The credential is not revoked.</exception>
  public void Restore()
  {
    if (RevokedAt is null)
    {
      throw new InvalidOperationException("Credential is not revoked.");
    }

    RevokedAt = null;
  }

  /// <summary>
  /// Moves this credential onto <paramref name="target"/>. Type and Handle stay immutable.
  /// </summary>
  /// <exception cref="ArgumentException"><paramref name="target"/> is empty.</exception>
  /// <exception cref="InvalidOperationException">The credential is already on <paramref name="target"/>.</exception>
  public void ReparentTo(PrincipalId target)
  {
    if (target.IsEmpty)
    {
      throw new ArgumentException("Target PrincipalId cannot be empty.", nameof(target));
    }

    if (target == PrincipalId)
    {
      throw new InvalidOperationException("Credential is already on this principal.");
    }

    PrincipalId = target;
  }

  private static string? NormalizeNickname(string? nickname)
  {
    if (nickname is null)
    {
      return null;
    }

    string trimmed = nickname.Trim();
    if (trimmed.Length == 0)
    {
      return null;
    }

    if (trimmed.Length > MaxNicknameLength)
    {
      throw new ArgumentException($"Nickname cannot exceed {MaxNicknameLength} characters after trimming.", nameof(nickname));
    }

    return trimmed;
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
