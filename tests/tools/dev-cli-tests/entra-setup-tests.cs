// ReSharper disable InconsistentNaming
namespace EntraSetup_;

public class UnionRedirectUris_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<UnionRedirectUris_Given_>();

  public static Task ExistingPlusDesired_Should_PreserveFirstSeenOrderAndSkipDuplicates()
  {
    IReadOnlyList<string> union = EntraSetup.UnionRedirectUris(
      ["https://localhost:63611/signin-oidc", "https://already.example/signin-oidc"],
      ["https://localhost:63611/signin-oidc", "https://localhost:63610/signin-oidc", ""]);

    union.ShouldBe(
    [
      "https://localhost:63611/signin-oidc",
      "https://already.example/signin-oidc",
      "https://localhost:63610/signin-oidc"
    ]);
    return Task.CompletedTask;
  }

  public static Task CaseInsensitiveDuplicate_Should_KeepFirstCasing()
  {
    IReadOnlyList<string> union = EntraSetup.UnionRedirectUris(
      ["https://LocalHost:63611/signin-oidc"],
      ["https://localhost:63611/signin-oidc"]);

    union.ShouldBe(["https://LocalHost:63611/signin-oidc"]);
    return Task.CompletedTask;
  }
}

public class BuildDesiredRedirectUris_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<BuildDesiredRedirectUris_Given_>();

  public static Task Defaults_Should_IncludeWebServerAndIngress()
  {
    IReadOnlyList<string> uris = EntraSetup.BuildDesiredRedirectUris(null, []);
    uris.ShouldBe(
    [
      EntraSetup.WebServerHttpsRedirect,
      EntraSetup.IngressHttpsRedirect
    ]);
    return Task.CompletedTask;
  }

  public static Task PublicOrigin_Should_AppendSigninOidcWithoutDoubling()
  {
    IReadOnlyList<string> fromHost = EntraSetup.BuildDesiredRedirectUris("https://arch.timewarp.work/", []);
    fromHost.ShouldContain("https://arch.timewarp.work/signin-oidc");

    IReadOnlyList<string> already = EntraSetup.BuildDesiredRedirectUris(
      "https://arch.timewarp.work/signin-oidc",
      []);
    already.ShouldContain("https://arch.timewarp.work/signin-oidc");
    already.Count(uri => uri.Contains("arch.timewarp.work", StringComparison.Ordinal)).ShouldBe(1);
    return Task.CompletedTask;
  }
}

public class MaskSecret_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<MaskSecret_Given_>();

  public static Task NonEmpty_Should_NotContainPlaintext()
  {
    const string secret = "super-secret-value";
    string masked = EntraSetup.MaskSecret(secret);
    masked.ShouldBe(EntraSetup.MaskedSecret);
    masked.ShouldNotContain(secret);
    EntraSetup.MaskSecret(null).ShouldBe("(not set)");
    EntraSetup.MaskSecret("").ShouldBe("(not set)");
    return Task.CompletedTask;
  }
}

public class ReseedSiteSettingsKey_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ReseedSiteSettingsKey_Should_>();

  public static Task Be_The_Authentication_Entra_Flag()
  {
    EntraSetup.ReseedSiteSettingsKey.ShouldBe("Authentication:Entra:ReseedSiteSettings");
    EntraSetup.ObsoleteTrustedTenants0Key.ShouldBe("Authentication:Entra:TrustedTenants:0");
    return Task.CompletedTask;
  }
}

public class ParseUserSecretsList_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ParseUserSecretsList_Given_>();

  public static Task KeyEqualsValue_Should_SplitOnFirstSeparator()
  {
    const string stdout = """
      Authentication:Entra:Enabled = True
      Authentication:Entra:ClientSecret = a=b=c
      junk line
      """;
    Dictionary<string, string> secrets = EntraSetup.ParseUserSecretsList(stdout);
    secrets[EntraSetup.EnabledKey].ShouldBe("True");
    secrets[EntraSetup.ClientSecretKey].ShouldBe("a=b=c");
    EntraSetup.HasClientSecret(secrets).ShouldBeTrue();
    return Task.CompletedTask;
  }
}

public class FormatInvocation_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FormatInvocation_Given_>();

  public static Task DisplayNameWithSpaces_Should_BeQuoted()
  {
    string line = EntraSetup.FormatInvocation(
      "az",
      ["ad", "app", "list", "--display-name", "TimeWarp Architecture Dev"]);
    line.ShouldBe("az ad app list --display-name \"TimeWarp Architecture Dev\"");
    line.ShouldNotContain("password");
    return Task.CompletedTask;
  }
}

public class TryFindExactAppIds_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TryFindExactAppIds_Given_>();

  public static Task ExactDisplayName_Should_ReturnAppId()
  {
    const string json = """
      [{"appId":"11111111-1111-1111-1111-111111111111","displayName":"TimeWarp Architecture Dev"}]
      """;
    EntraSetup.TryFindExactAppIds(json, EntraSetup.DefaultDisplayName, out IReadOnlyList<string> ids)
      .ShouldBeTrue();
    ids.ShouldBe(["11111111-1111-1111-1111-111111111111"]);
    return Task.CompletedTask;
  }

  public static Task EmptyArray_Should_ReturnNoIds()
  {
    EntraSetup.TryFindExactAppIds("[]", EntraSetup.DefaultDisplayName, out IReadOnlyList<string> ids)
      .ShouldBeTrue();
    ids.ShouldBeEmpty();
    return Task.CompletedTask;
  }
}

public class TryReadAccount_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TryReadAccount_Given_>();

  public static Task TenantAndUser_Should_Parse()
  {
    const string json = """{"tenantId":"30f3971f-4719-4f20-9b6f-88916e0b95bd","user":"dev@example.com"}""";
    EntraSetup.TryReadAccount(json, out string tenantId, out string user).ShouldBeTrue();
    tenantId.ShouldBe("30f3971f-4719-4f20-9b6f-88916e0b95bd");
    user.ShouldBe("dev@example.com");
    return Task.CompletedTask;
  }
}

public class CredentialDisplayName_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CredentialDisplayName_Given_>();

  public static Task UtcTimestamp_Should_UseDevYyyyMMdd()
  {
    DateTimeOffset timestamp = new(2026, 9, 15, 23, 0, 0, TimeSpan.Zero);
    EntraSetup.CredentialDisplayName(timestamp).ShouldBe("dev-20260915");
    return Task.CompletedTask;
  }
}

public class TryDecideMintClientSecret_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<TryDecideMintClientSecret_Given_>();

  public static Task NewSecret_Should_MintEvenWhenListFailed()
  {
    EntraSetup.TryDecideMintClientSecret(
      newSecret: true,
      listSucceeded: false,
      hasExistingClientSecret: true,
      out bool mint).ShouldBeTrue();
    mint.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task ListFailedWithoutNewSecret_Should_AbortWithoutMint()
  {
    EntraSetup.TryDecideMintClientSecret(
      newSecret: false,
      listSucceeded: false,
      hasExistingClientSecret: false,
      out bool mint).ShouldBeFalse();
    mint.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task ListSucceededWithoutSecret_Should_Mint()
  {
    EntraSetup.TryDecideMintClientSecret(
      newSecret: false,
      listSucceeded: true,
      hasExistingClientSecret: false,
      out bool mint).ShouldBeTrue();
    mint.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task ListSucceededWithSecret_Should_Skip()
  {
    EntraSetup.TryDecideMintClientSecret(
      newSecret: false,
      listSucceeded: true,
      hasExistingClientSecret: true,
      out bool mint).ShouldBeTrue();
    mint.ShouldBeFalse();
    return Task.CompletedTask;
  }
}
