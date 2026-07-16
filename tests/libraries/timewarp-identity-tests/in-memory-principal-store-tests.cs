namespace InMemoryPrincipalStore_;

public class Principals
{
  public async Task Add_and_get_round_trips()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);

    await store.AddPrincipalAsync(principal);
    Principal? loaded = await store.GetPrincipalAsync(principal.Id);

    loaded.ShouldNotBeNull();
    loaded.Id.ShouldBe(principal.Id);
    loaded.Kind.ShouldBe(PrincipalKind.Human);
  }

  public async Task Duplicate_principal_id_fails()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    await Should.ThrowAsync<InvalidOperationException>(() => store.AddPrincipalAsync(principal));
  }

  public async Task Update_persists_display_name_and_tier()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Agent);
    await store.AddPrincipalAsync(principal);

    principal.SetDisplayName("bot");
    principal.SetTrustTier(TrustTier.Funded);
    await store.UpdatePrincipalAsync(principal);

    Principal? loaded = await store.GetPrincipalAsync(principal.Id);
    loaded.ShouldNotBeNull();
    loaded.DisplayName.ShouldBe("bot");
    loaded.TrustTier.ShouldBe(TrustTier.Funded);
  }

  public async Task Update_missing_principal_fails()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Service);
    await Should.ThrowAsync<InvalidOperationException>(() => store.UpdatePrincipalAsync(principal));
  }
}

public class Credentials
{
  public async Task Multi_credential_per_principal_is_allowed()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Credential passkey = Credential.Create(principal.Id, CredentialType.Passkey, [1, 1], [2, 2], "passkey");
    Credential agentKey = Credential.Create(principal.Id, CredentialType.AgentKey, [3, 3], [4, 4], "agent");

    await store.AddCredentialAsync(passkey);
    await store.AddCredentialAsync(agentKey);

    IReadOnlyList<Credential> list = await store.ListCredentialsAsync(principal.Id);
    list.Count.ShouldBe(2);
  }

  public async Task Find_by_handle_returns_match()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Agent);
    await store.AddPrincipalAsync(principal);

    byte[] handle = [10, 20, 30];
    Credential credential = Credential.Create(principal.Id, CredentialType.AgentKey, handle, [40]);
    await store.AddCredentialAsync(credential);

    Credential? found = await store.FindCredentialByHandleAsync(CredentialType.AgentKey, [10, 20, 30]);
    found.ShouldNotBeNull();
    found.Id.ShouldBe(credential.Id);
  }

  public async Task Duplicate_type_and_handle_fails()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    byte[] handle = [7, 7];
    await store.AddCredentialAsync(Credential.Create(principal.Id, CredentialType.Passkey, handle, [1]));

    Credential duplicate = Credential.Create(principal.Id, CredentialType.Passkey, handle.ToArray(), [2]);
    await Should.ThrowAsync<InvalidOperationException>(() => store.AddCredentialAsync(duplicate));
  }

  public async Task Same_handle_different_type_is_allowed()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    byte[] handle = [8, 8];
    await store.AddCredentialAsync(Credential.Create(principal.Id, CredentialType.Passkey, handle, [1]));
    await store.AddCredentialAsync(Credential.Create(principal.Id, CredentialType.AgentKey, handle.ToArray(), [2]));

    (await store.ListCredentialsAsync(principal.Id)).Count.ShouldBe(2);
  }

  public async Task Missing_principal_fails_credential_add()
  {
    var store = new InMemoryPrincipalStore();
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]);
    await Should.ThrowAsync<InvalidOperationException>(() => store.AddCredentialAsync(credential));
  }

  public async Task List_excludes_revoked_by_default()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Human);
    await store.AddPrincipalAsync(principal);

    Credential active = Credential.Create(principal.Id, CredentialType.Passkey, [1], [2]);
    Credential revoked = Credential.Create(principal.Id, CredentialType.Passkey, [3], [4]);
    await store.AddCredentialAsync(active);
    await store.AddCredentialAsync(revoked);
    revoked.Revoke();
    await store.UpdateCredentialAsync(revoked);

    IReadOnlyList<Credential> activeOnly = await store.ListCredentialsAsync(principal.Id);
    activeOnly.Count.ShouldBe(1);
    activeOnly[0].Id.ShouldBe(active.Id);

    IReadOnlyList<Credential> all = await store.ListCredentialsAsync(principal.Id, includeRevoked: true);
    all.Count.ShouldBe(2);
  }

  public async Task Get_credential_by_id()
  {
    var store = new InMemoryPrincipalStore();
    Principal principal = Principal.Create(PrincipalKind.Service);
    await store.AddPrincipalAsync(principal);

    Credential credential = Credential.Create(principal.Id, CredentialType.AgentKey, [5], [6]);
    await store.AddCredentialAsync(credential);

    Credential? loaded = await store.GetCredentialAsync(credential.Id);
    loaded.ShouldNotBeNull();
    loaded.PrincipalId.ShouldBe(principal.Id);
  }
}
