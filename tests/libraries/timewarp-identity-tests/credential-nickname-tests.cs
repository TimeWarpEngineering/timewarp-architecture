namespace CredentialNickname_;

using System.Security.Cryptography;

public class Create_And_Rename
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Create_And_Rename>();

  public static Task Keeps_provider_label_and_nickname_separate()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2], "Proton Pass", " Work laptop ");

    credential.Label.ShouldBe("Proton Pass");
    credential.Nickname.ShouldBe("Work laptop");
    return Task.CompletedTask;
  }

  public static Task Nickname_defaults_to_null_and_whitespace_normalizes_to_null()
  {
    Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]).Nickname.ShouldBeNull();
    Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2], nickname: "   ").Nickname.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Rename_trims_and_replaces()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2], "Proton Pass");

    credential.Rename("  Phone  ");

    credential.Nickname.ShouldBe("Phone");
    credential.Label.ShouldBe("Proton Pass");
    return Task.CompletedTask;
  }

  public static Task Rename_rejects_whitespace_null_and_oversize()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2]);

    Should.Throw<ArgumentException>(() => credential.Rename("   "));
    Should.Throw<ArgumentNullException>(() => credential.Rename(null!));
    Should.Throw<ArgumentException>(() => credential.Rename(new string('x', Credential.MaxNicknameLength + 1)));
    credential.Rename(new string('x', Credential.MaxNicknameLength));
    credential.Nickname!.Length.ShouldBe(Credential.MaxNicknameLength);
    return Task.CompletedTask;
  }

  public static Task Create_rejects_oversize_nickname()
  {
    Should.Throw<ArgumentException>(() =>
      Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2], nickname: new string('x', Credential.MaxNicknameLength + 1)));
    return Task.CompletedTask;
  }
}

public class RegisteredWith_
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RegisteredWith_>();

  public static Task Defaults_to_unknown()
  {
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.AgentKey, [1], [2]);

    credential.RegisteredWith.ShouldBe(RegisteredWith.Unknown);
    credential.RegisteredWith.IsUnknown.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Is_captured_at_create_and_normalized()
  {
    var context = new RegisteredWith(AuthenticatorAttachment.Platform, " Chrome ", new string('o', 40));
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2], registeredWith: context);

    credential.RegisteredWith.Attachment.ShouldBe(AuthenticatorAttachment.Platform);
    credential.RegisteredWith.Browser.ShouldBe("Chrome");
    credential.RegisteredWith.Os!.Length.ShouldBe(RegisteredWith.MaxFamilyLength);
    credential.RegisteredWith.IsUnknown.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Empty_family_strings_normalize_to_null_and_undefined_attachment_throws()
  {
    new RegisteredWith(AuthenticatorAttachment.CrossPlatform, "", "  ").Browser.ShouldBeNull();
    new RegisteredWith(AuthenticatorAttachment.CrossPlatform, "", "  ").Os.ShouldBeNull();
    Should.Throw<ArgumentOutOfRangeException>(() => new RegisteredWith((AuthenticatorAttachment)42, null, null));
    return Task.CompletedTask;
  }

  public static Task Snapshot_copies_nickname_and_registered_with()
  {
    var context = new RegisteredWith(AuthenticatorAttachment.CrossPlatform, "Safari", "iOS");
    Credential credential = Credential.Create(PrincipalId.New(), CredentialType.Passkey, [1], [2], "1Password", "Phone", context);

    var store = new InMemoryPrincipalStore();
    return RoundTripAsync(store, credential);

    static async Task RoundTripAsync(InMemoryPrincipalStore store, Credential credential)
    {
      var principal = Principal.Create(PrincipalKind.Human);
      Credential owned = Credential.Create(principal.Id, credential.Type, credential.Handle, credential.PublicMaterial, credential.Label, credential.Nickname, credential.RegisteredWith);
      await store.AddPrincipalAsync(principal);
      await store.AddCredentialAsync(owned);

      Credential? loaded = await store.GetCredentialAsync(owned.Id);
      loaded.ShouldNotBeNull();
      loaded.Nickname.ShouldBe("Phone");
      loaded.RegisteredWith.ShouldBe(credential.RegisteredWith);
      loaded.Fingerprint.ShouldBe(owned.Fingerprint);

      loaded.Rename("Old phone");
      await store.UpdateCredentialAsync(loaded);
      (await store.GetCredentialAsync(owned.Id))!.Nickname.ShouldBe("Old phone");
    }
  }
}

public class Fingerprint_
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Fingerprint_>();

  public static Task Is_last_eight_hex_of_sha256_of_handle()
  {
    byte[] handle = [1, 2, 3, 4, 5];
    string expected = Convert.ToHexStringLower(SHA256.HashData(handle))[^8..];

    CredentialFingerprint.Compute(handle).ShouldBe(expected);
    Credential.Create(PrincipalId.New(), CredentialType.Passkey, handle, [9]).Fingerprint.ShouldBe(expected);
    return Task.CompletedTask;
  }

  public static Task Is_stable_and_differs_per_handle()
  {
    CredentialFingerprint.Compute([7, 7, 7]).ShouldBe(CredentialFingerprint.Compute([7, 7, 7]));
    CredentialFingerprint.Compute([7, 7, 7]).ShouldNotBe(CredentialFingerprint.Compute([7, 7, 8]));
    CredentialFingerprint.Compute([7, 7, 7]).Length.ShouldBe(CredentialFingerprint.Length);
    return Task.CompletedTask;
  }

  public static Task Rejects_empty_handle()
  {
    Should.Throw<ArgumentException>(() => CredentialFingerprint.Compute([]));
    return Task.CompletedTask;
  }
}
