#region Purpose
// Host-free tests for WebAuthnRelyingPartySelection.Select (task 104-031): per-request RP-ID
// selection from the request host against the allowlist, fail-closed, canonical casing.
#endregion

namespace WebAuthnRelyingPartySelection_;

using TimeWarp.Architecture.Features.Identity.Application;
using TimeWarp.Identity;

public class Select_Should
{
  public void Return_Canonical_Allowlist_Entry_Given_Case_Insensitive_Match()
  {
    var options = new WebAuthnOptions
    {
      AllowedRpIds = ["localhost", "WebAuthn-Second.Test"],
      RpName = "Test RP",
      AllowedOrigins = ["https://webauthn-second.test"]
    };

    OneOf<WebAuthnRelyingParty, SharedProblemDetails> result = WebAuthnRelyingPartySelection.Select("webauthn-second.test", options);

    result.IsT0.ShouldBeTrue();
    // Canonical allowlist casing, not the request's — see the selection's Design region.
    result.AsT0.Id.ShouldBe("WebAuthn-Second.Test");
    // RpName and AllowedOrigins flow straight through onto the selected relying party.
    result.AsT0.Name.ShouldBe("Test RP");
    result.AsT0.Origins.ShouldBe(new[] { "https://webauthn-second.test" });
  }

  public void Return_Problem_Given_Null_Host()
  {
    var options = new WebAuthnOptions { AllowedRpIds = ["localhost"] };

    OneOf<WebAuthnRelyingParty, SharedProblemDetails> result = WebAuthnRelyingPartySelection.Select(null, options);

    result.IsT1.ShouldBeTrue();
    result.AsT1.Status.ShouldBe(400);
    result.AsT1.Title.ShouldBe("Host not allowed");
  }

  public void Return_Problem_Given_Unlisted_Host()
  {
    var options = new WebAuthnOptions { AllowedRpIds = ["localhost"] };

    OneOf<WebAuthnRelyingParty, SharedProblemDetails> result = WebAuthnRelyingPartySelection.Select("not-allowed.example", options);

    result.IsT1.ShouldBeTrue();
    result.AsT1.Status.ShouldBe(400);
    // Never echoes the requested host into the response body.
    result.AsT1.Detail!.ShouldNotContain("not-allowed.example");
  }
}
