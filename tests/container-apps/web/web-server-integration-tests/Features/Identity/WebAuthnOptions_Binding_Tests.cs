#region Purpose
// Regression pin for round-1 finding M1: AddFluentValidatedOptions binds configuration by the
// options TYPE NAME ("WebAuthnOptions") absent a [ConfigurationKey] attribute — a JSON section
// literally named "WebAuthn" never binds and is silently ignored. This test fails if that
// regresses, because the test appsettings.json's WebAuthnOptions:RpName value differs from
// WebAuthnOptions's C# property-initializer default ("TimeWarp Architecture"), so a
// defaults-mask-the-bug scenario (like the one this finding caught) cannot recur undetected.
#endregion

namespace WebAuthnOptionsBinding_;

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

    // RpId is left unset in the test appsettings.json — partial binding must still leave it at its
    // C# default ("localhost", matching the fixed test host origin every other identity test relies
    // on) rather than being cleared by the section merely existing.
    options.Value.RpId.ShouldBe("localhost");
  }
}
