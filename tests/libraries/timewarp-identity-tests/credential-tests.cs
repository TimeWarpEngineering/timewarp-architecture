namespace Credential_;

public class Create
{
  public void Sets_identity_fields()
  {
    PrincipalId principalId = PrincipalId.New();
    byte[] handle = [1, 2, 3];
    byte[] material = [9, 8, 7];

    Credential credential = Credential.Create(principalId, CredentialType.Passkey, handle, material, " laptop ");

    credential.Id.ShouldNotBe(Guid.Empty);
    credential.PrincipalId.ShouldBe(principalId);
    credential.Type.ShouldBe(CredentialType.Passkey);
    credential.Handle.ShouldBe(handle);
    credential.PublicMaterial.ShouldBe(material);
    credential.Label.ShouldBe("laptop");
    credential.RevokedAt.ShouldBeNull();
    credential.IsRevoked.ShouldBeFalse();
  }

  public void Defensive_copies_handle_and_public_material()
  {
    byte[] handle = [1, 2, 3];
    byte[] material = [4, 5, 6];

    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.AgentKey, handle, material);

    handle[0] = 99;
    material[0] = 99;

    credential.Handle[0].ShouldBe((byte)1);
    credential.PublicMaterial[0].ShouldBe((byte)4);
  }

  public void Getter_returns_are_not_writable_into_storage()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1, 2, 3], [4, 5, 6]);

    byte[] handleView = credential.Handle;
    handleView[0] = 99;

    credential.Handle[0].ShouldBe((byte)1);
  }

  public void Rejects_empty_principal_id() =>
    Should.Throw<ArgumentException>(() =>
      Credential.Create(default, CredentialType.Passkey, [1], [2]));

  public void Rejects_undefined_type() =>
    Should.Throw<ArgumentOutOfRangeException>(() =>
      Credential.Create(PrincipalId.New(), (CredentialType)999, [1], [2]));

  public void Rejects_null_handle() =>
    Should.Throw<ArgumentNullException>(() =>
      Credential.Create(PrincipalId.New(), CredentialType.Passkey, null!, [1]));

  public void Rejects_null_public_material() =>
    Should.Throw<ArgumentNullException>(() =>
      Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], null!));

  public void Rejects_empty_handle() =>
    Should.Throw<ArgumentException>(() =>
      Credential.Create(PrincipalId.New(), CredentialType.Passkey, [], [1]));

  public void Rejects_empty_public_material() =>
    Should.Throw<ArgumentException>(() =>
      Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], []));
}

public class Revoke
{
  public void Sets_revoked_at_once()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]);
    DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-1);

    credential.Revoke();

    credential.IsRevoked.ShouldBeTrue();
    credential.RevokedAt.ShouldNotBeNull();
    credential.RevokedAt.Value.ShouldBeInRange(before, DateTimeOffset.UtcNow.AddSeconds(1));
  }

  public void Second_revoke_throws()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.AgentKey, [1], [2]);
    credential.Revoke();
    Should.Throw<InvalidOperationException>(credential.Revoke);
  }
}
