namespace Credential_;

public class Create
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Create>();

  public static Task Sets_identity_fields()
  {
    PrincipalId principalId = PrincipalId.New();
    byte[] handle = [1, 2, 3];
    byte[] material = [9, 8, 7];

    Credential credential = Credential.Create(principalId, CredentialType.Passkey, handle, material, " laptop ");

    credential.Id.IsEmpty.ShouldBeFalse();
    credential.Id.Value.ShouldNotBe(Guid.Empty);
    credential.PrincipalId.ShouldBe(principalId);
    credential.Type.ShouldBe(CredentialType.Passkey);
    credential.Handle.ShouldBe(handle);
    credential.PublicMaterial.ShouldBe(material);
    credential.Label.ShouldBe("laptop");
    credential.RevokedAt.ShouldBeNull();
    credential.IsRevoked.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Defensive_copies_handle_and_public_material()
  {
    byte[] handle = [1, 2, 3];
    byte[] material = [4, 5, 6];

    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.AgentKey, handle, material);

    handle[0] = 99;
    material[0] = 99;

    credential.Handle[0].ShouldBe((byte)1);
    credential.PublicMaterial[0].ShouldBe((byte)4);
    return Task.CompletedTask;
  }

  public static Task Getter_returns_are_not_writable_into_storage()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1, 2, 3], [4, 5, 6]);

    byte[] handleView = credential.Handle;
    handleView[0] = 99;

    credential.Handle[0].ShouldBe((byte)1);
    return Task.CompletedTask;
  }

  public static Task Rejects_empty_principal_id()
  {
    Should.Throw<ArgumentException>(() =>
      Credential.Create(default, CredentialType.Passkey, [1], [2]));
    return Task.CompletedTask;
  }

  public static Task Rejects_none_type()
  {
    Should.Throw<ArgumentOutOfRangeException>(() =>
      Credential.Create(PrincipalId.New(), CredentialType.None, [1], [2]));
    return Task.CompletedTask;
  }

  public static Task Rejects_undefined_type()
  {
    Should.Throw<ArgumentOutOfRangeException>(() =>
      Credential.Create(PrincipalId.New(), (CredentialType)999, [1], [2]));
    return Task.CompletedTask;
  }

  public static Task Rejects_null_handle()
  {
    Should.Throw<ArgumentNullException>(() =>
      Credential.Create(PrincipalId.New(), CredentialType.Passkey, null!, [1]));
    return Task.CompletedTask;
  }

  public static Task Rejects_null_public_material()
  {
    Should.Throw<ArgumentNullException>(() =>
      Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], null!));
    return Task.CompletedTask;
  }

  public static Task Rejects_empty_handle()
  {
    Should.Throw<ArgumentException>(() =>
      Credential.Create(PrincipalId.New(), CredentialType.Passkey, [], [1]));
    return Task.CompletedTask;
  }

  public static Task Rejects_empty_public_material()
  {
    Should.Throw<ArgumentException>(() =>
      Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], []));
    return Task.CompletedTask;
  }

  public static Task Version_defaults_to_zero()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]);
    credential.Version.ShouldBe(0);
    return Task.CompletedTask;
  }

  public static Task Different_ids_are_not_equal()
  {
    PrincipalId principalId = PrincipalId.New();
    Credential a = Credential.Create(principalId, CredentialType.Passkey, [1], [2]);
    Credential b = Credential.Create(principalId, CredentialType.Passkey, [3], [4]);
    a.ShouldNotBe(b);
    return Task.CompletedTask;
  }

  public static Task Entra_account_type_is_three()
  {
    ((int)CredentialType.None).ShouldBe(0);
    ((int)CredentialType.Passkey).ShouldBe(1);
    ((int)CredentialType.AgentKey).ShouldBe(2);
    ((int)CredentialType.EntraAccount).ShouldBe(3);
    return Task.CompletedTask;
  }

  public static Task Accepts_entra_account_with_issuer_material()
  {
    PrincipalId principalId = PrincipalId.New();
    Guid tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    Guid objectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    byte[] handle = EntraAccountHandle.Encode(tenantId, objectId);
    byte[] material = EntraIssuerMaterial.FromTenantId(tenantId);

    Credential credential = Credential.Create(principalId, CredentialType.EntraAccount, handle, material);

    credential.Type.ShouldBe(CredentialType.EntraAccount);
    credential.Handle.ShouldBe(handle);
    credential.PublicMaterial.ShouldBe(material);
    credential.IsRevoked.ShouldBeFalse();
    Encoding.UTF8.GetString(credential.PublicMaterial)
      .ShouldBe("https://login.microsoftonline.com/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/v2.0");
    return Task.CompletedTask;
  }
}

public class Revoke
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Revoke>();

  public static Task Sets_revoked_at_once()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]);
    DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-1);

    credential.Revoke();

    credential.IsRevoked.ShouldBeTrue();
    credential.RevokedAt.ShouldNotBeNull();
    credential.RevokedAt.Value.ShouldBeInRange(before, DateTimeOffset.UtcNow.AddSeconds(1));
    return Task.CompletedTask;
  }

  public static Task Second_revoke_throws()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.AgentKey, [1], [2]);
    credential.Revoke();
    Should.Throw<InvalidOperationException>(credential.Revoke);
    return Task.CompletedTask;
  }
}

public class Restore
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Restore>();

  public static Task Clears_revoked_at()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]);
    credential.Revoke();

    credential.Restore();

    credential.IsRevoked.ShouldBeFalse();
    credential.RevokedAt.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Throws_if_not_revoked()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.EntraAccount, [1], [2]);
    Should.Throw<InvalidOperationException>(credential.Restore);
    return Task.CompletedTask;
  }

  public static Task Second_restore_throws()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.AgentKey, [1], [2]);
    credential.Revoke();
    credential.Restore();
    Should.Throw<InvalidOperationException>(credential.Restore);
    return Task.CompletedTask;
  }

  public static Task Revoke_after_restore_succeeds()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]);
    credential.Revoke();
    credential.Restore();

    credential.Revoke();

    credential.IsRevoked.ShouldBeTrue();
    credential.RevokedAt.ShouldNotBeNull();
    return Task.CompletedTask;
  }

  public static Task Leaves_identity_fields_unchanged()
  {
    PrincipalId principalId = PrincipalId.New();
    byte[] handle = [9, 8, 7];
    byte[] material = [1, 2, 3];
    Credential credential = Credential.Create(principalId, CredentialType.EntraAccount, handle, material, "entra");
    CredentialId id = credential.Id;
    DateTimeOffset createdAt = credential.CreatedAt;
    credential.Revoke();

    credential.Restore();

    credential.Id.ShouldBe(id);
    credential.PrincipalId.ShouldBe(principalId);
    credential.Type.ShouldBe(CredentialType.EntraAccount);
    credential.Handle.ShouldBe(handle);
    credential.PublicMaterial.ShouldBe(material);
    credential.Label.ShouldBe("entra");
    credential.CreatedAt.ShouldBe(createdAt);
    return Task.CompletedTask;
  }
}

public class ReparentTo
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ReparentTo>();

  public static Task Changes_principal_id_and_keeps_type_handle()
  {
    PrincipalId original = PrincipalId.New();
    PrincipalId target = PrincipalId.New();
    byte[] handle = [1, 2, 3];
    byte[] material = [4, 5, 6];
    Credential credential = Credential.Create(original, CredentialType.Passkey, handle, material, "laptop");
    CredentialId id = credential.Id;

    credential.ReparentTo(target);

    credential.Id.ShouldBe(id);
    credential.PrincipalId.ShouldBe(target);
    credential.Type.ShouldBe(CredentialType.Passkey);
    credential.Handle.ShouldBe(handle);
    credential.PublicMaterial.ShouldBe(material);
    credential.Label.ShouldBe("laptop");
    credential.IsRevoked.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Rejects_empty_target()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]);
    Should.Throw<ArgumentException>(() => credential.ReparentTo(default));
    return Task.CompletedTask;
  }

  public static Task Rejects_same_principal()
  {
    PrincipalId principalId = PrincipalId.New();
    Credential credential = Credential.Create(principalId, CredentialType.Passkey, [1], [2]);
    Should.Throw<InvalidOperationException>(() => credential.ReparentTo(principalId));
    return Task.CompletedTask;
  }
}
