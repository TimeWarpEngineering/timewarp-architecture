namespace EntraAccountStore_;

public class RestoreRoundTrip
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RestoreRoundTrip>();

  public static async Task Find_returns_revoked_row_then_restore_reuses_same_id()
  {
    IPrincipalStore store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Guid tenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    Guid objectId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    byte[] handle = EntraAccountHandle.Encode(tenantId, objectId);
    byte[] material = EntraIssuerMaterial.FromTenantId(tenantId);
    Credential credential = Credential.Create(principal.Id, CredentialType.EntraAccount, handle, material);
    CredentialId credentialId = credential.Id;
    await store.AddCredentialAsync(credential);

    Credential? stored = await store.GetCredentialAsync(credentialId);
    stored.ShouldNotBeNull();
    stored.Revoke();
    await store.UpdateCredentialAsync(stored);

    Credential? foundRevoked = await store.FindCredentialByHandleAsync(CredentialType.EntraAccount, handle);
    foundRevoked.ShouldNotBeNull();
    foundRevoked.Id.ShouldBe(credentialId);
    foundRevoked.IsRevoked.ShouldBeTrue();

    foundRevoked.Restore();
    await store.UpdateCredentialAsync(foundRevoked);

    Credential? foundRestored = await store.FindCredentialByHandleAsync(CredentialType.EntraAccount, handle);
    foundRestored.ShouldNotBeNull();
    foundRestored.Id.ShouldBe(credentialId);
    foundRestored.IsRevoked.ShouldBeFalse();
    foundRestored.RevokedAt.ShouldBeNull();
    foundRestored.Type.ShouldBe(CredentialType.EntraAccount);
    foundRestored.PublicMaterial.ShouldBe(material);
  }
}

public class UniqueHandle
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<UniqueHandle>();

  public static async Task Duplicate_entra_handle_fails_even_when_first_is_revoked()
  {
    IPrincipalStore store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Guid tenantId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    Guid objectId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    byte[] handle = EntraAccountHandle.Encode(tenantId, objectId);
    byte[] material = EntraIssuerMaterial.FromTenantId(tenantId);

    Credential first = Credential.Create(principal.Id, CredentialType.EntraAccount, handle, material);
    await store.AddCredentialAsync(first);

    Credential? stored = await store.GetCredentialAsync(first.Id);
    stored.ShouldNotBeNull();
    stored.Revoke();
    await store.UpdateCredentialAsync(stored);

    Credential duplicate = Credential.Create(
      principal.Id,
      CredentialType.EntraAccount,
      EntraAccountHandle.Encode(tenantId, objectId),
      EntraIssuerMaterial.FromTenantId(tenantId));

    await Should.ThrowAsync<InvalidOperationException>(() => store.AddCredentialAsync(duplicate));

    Credential? stillThere = await store.FindCredentialByHandleAsync(CredentialType.EntraAccount, handle);
    stillThere.ShouldNotBeNull();
    stillThere.Id.ShouldBe(first.Id);
    stillThere.IsRevoked.ShouldBeTrue();
  }
}
