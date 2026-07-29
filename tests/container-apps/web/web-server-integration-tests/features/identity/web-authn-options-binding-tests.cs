#region Purpose
// Regression pin for round-1 finding M1: AddFluentValidatedOptions binds configuration by the
// options TYPE NAME ("WebAuthnOptions") absent a [ConfigurationKey] attribute — a JSON section
// literally named "WebAuthn" never binds and is silently ignored. This test fails if that
// regresses, because the test appsettings.json's WebAuthnOptions:RpName value differs from
// WebAuthnOptions's C# property-initializer default ("TimeWarp Architecture"), so a
// defaults-mask-the-bug scenario (like the one this finding caught) cannot recur undetected.
// Also pins task 104-031: the AllowedRpIds binder APPEND semantics, and the hermetic test host's
// removal of developer user secrets.
#endregion

#region Design
// AllowedRpIds append pin (task 104-031): WebAuthnOptions initializes AllowedRpIds to ["localhost"]
// in C#, and the test appsettings.json adds ["webauthn-second.test"]. The Microsoft configuration
// binder APPENDS onto a pre-initialized List<T> rather than replacing it, so the effective value is
// ["localhost","webauthn-second.test"]. This test locks that behavior: if a framework change flipped
// the binder to replace, the assertion for "localhost" being present would fail — which matters
// because the whole zero-config default (localhost still works after a user secret adds a share host)
// rests on append.
// Hermeticity pin (task 104-031): WebApplicationHost strips every user-secrets JsonConfigurationSource
// (Path == "secrets.json"). The assertion below confirms no bound configuration source path ends with
// secrets.json, so a developer's machine-local WebAuthnOptions secret can never leak into a test host.
#endregion

namespace WebAuthnOptionsBinding_;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Configuration;

public class Returns_
{
  private readonly WebTestServerApplication WebTestServerApplication;

  public Returns_(WebTestServerApplication webTestServerApplication)
  {
    WebTestServerApplication = webTestServerApplication;
  }

  public void ConfiguredValue_Given_AppSettings_Overrides_The_CSharp_Default()
  {
    IOptions<WebAuthnOptions> options =
      WebTestServerApplication.WebApplicationHost.ServiceProvider.GetRequiredService<IOptions<WebAuthnOptions>>();

    // The test appsettings.json sets WebAuthnOptions:RpName to a value distinct from the C# default
    // ("TimeWarp Architecture") specifically so this assertion cannot pass by coincidence — if the
    // section name regresses to something AddFluentValidatedOptions does not bind, this reads back
    // the untouched default and the test fails.
    options.Value.RpName.ShouldBe("Integration Test RP");

    // Binder APPEND semantics (task 104-031): the C# default ["localhost"] plus the test
    // appsettings.json entry ["webauthn-second.test"] must bind to BOTH, in order — proving the
    // binder appends onto the pre-initialized list rather than replacing it. The zero-config
    // "localhost still works after a share host is added" guarantee depends on this.
    options.Value.AllowedRpIds.ShouldBe(new[] { "localhost", "webauthn-second.test" });
  }

  public void NoUserSecrets_Source_Given_HermeticHost()
  {
    // Hermeticity (task 104-031): WebApplicationHost strips the developer's user-secrets file source,
    // so no bound configuration source may point at a secrets.json — otherwise a machine-local
    // WebAuthnOptions secret could silently alter test outcomes.
    IConfigurationRoot configurationRoot =
      (IConfigurationRoot)WebTestServerApplication.WebApplicationHost.Configuration;

    foreach (IConfigurationProvider provider in configurationRoot.Providers)
    {
      if (provider is JsonConfigurationProvider jsonProvider)
      {
        jsonProvider.Source.Path.ShouldNotEndWith("secrets.json");
      }
    }
  }
}
