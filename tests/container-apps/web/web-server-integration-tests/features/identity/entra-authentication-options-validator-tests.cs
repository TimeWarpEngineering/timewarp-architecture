#region Purpose
// Host-free validator and PublicOrigin helper tests for Authentication:Entra.
#endregion

#region Design
// PublicOrigin is optional. Empty/null stays valid (direct launch). Non-https non-localhost
// values must fail so a proxied http origin cannot reach Entra. TryGetPublicRedirectUri composes
// origin + CallbackPath and ignores trailing slashes on the origin.
#endregion

namespace EntraAuthenticationOptionsValidator_;

public class Validate_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Validate_Should>();

  public static Task Accept_Empty_Public_Origin()
  {
    EntraAuthenticationOptions options = EnabledOptions();
    options.PublicOrigin = "";

    new EntraAuthenticationOptionsValidator().Validate(options).IsValid.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Accept_Https_Public_Origin()
  {
    EntraAuthenticationOptions options = EnabledOptions();
    options.PublicOrigin = "https://arch.timewarp.work";

    new EntraAuthenticationOptionsValidator().Validate(options).IsValid.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Accept_Https_Localhost_With_Port()
  {
    EntraAuthenticationOptions options = EnabledOptions();
    options.PublicOrigin = "https://localhost:63610";

    new EntraAuthenticationOptionsValidator().Validate(options).IsValid.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Accept_Http_Localhost()
  {
    EntraAuthenticationOptions options = EnabledOptions();
    options.PublicOrigin = "http://localhost:63620";

    new EntraAuthenticationOptionsValidator().Validate(options).IsValid.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Reject_Http_Non_Localhost_Public_Origin()
  {
    EntraAuthenticationOptions options = EnabledOptions();
    options.PublicOrigin = "http://arch.timewarp.work";

    new EntraAuthenticationOptionsValidator().Validate(options).IsValid.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Reject_Relative_Public_Origin()
  {
    EntraAuthenticationOptions options = EnabledOptions();
    options.PublicOrigin = "arch.timewarp.work";

    new EntraAuthenticationOptionsValidator().Validate(options).IsValid.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Reject_Public_Origin_With_Path()
  {
    EntraAuthenticationOptions options = EnabledOptions();
    options.PublicOrigin = "https://arch.timewarp.work/signin-oidc";

    new EntraAuthenticationOptionsValidator().Validate(options).IsValid.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Compose_Public_Redirect_Uri_When_Set()
  {
    EntraAuthenticationOptions options = EnabledOptions();
    options.PublicOrigin = "https://arch.timewarp.work/";
    options.CallbackPath = "/signin-oidc";

    options.TryGetPublicRedirectUri(out string? redirectUri).ShouldBeTrue();
    redirectUri.ShouldBe("https://arch.timewarp.work/signin-oidc");
    return Task.CompletedTask;
  }

  public static Task Skip_Public_Redirect_Uri_When_Unset()
  {
    EntraAuthenticationOptions options = EnabledOptions();
    options.PublicOrigin = null;

    options.TryGetPublicRedirectUri(out string? redirectUri).ShouldBeFalse();
    redirectUri.ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Reject_Obsolete_TrustedTenants_Key()
  {
    IConfiguration configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(
        new Dictionary<string, string?>
        {
          ["Authentication:Entra:TrustedTenants:0"] = "30f3971f-4719-4f20-9b6f-88916e0b95bd"
        })
      .Build();

    FluentValidation.Results.ValidationResult result =
      new EntraAuthenticationOptionsValidator(configuration).Validate(EnabledOptions());
    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(error =>
      error.ErrorMessage == EntraAuthenticationOptionsValidator.TrustedTenantsRemovedMessage);
    return Task.CompletedTask;
  }

  private static EntraAuthenticationOptions EnabledOptions() =>
    new()
    {
      Enabled = true,
      Instance = "https://login.microsoftonline.com/",
      TenantId = "organizations",
      ClientId = "11111111-1111-1111-1111-111111111111",
      CallbackPath = "/signin-oidc"
    };
}
