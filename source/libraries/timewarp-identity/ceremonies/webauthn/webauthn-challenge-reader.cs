#region Purpose
// Minimal public wrapper so a handler can extract the challenge embedded in clientDataJSON and
// consume it from IWebAuthnChallengeStore BEFORE calling WebAuthnRegistration/WebAuthnAuthentication
// .Verify — the replay-safe ordering the port contract requires.
#endregion

#region Design
// Deliberately separate from Verify: the challenge must be consumed (one-time) even when the rest
// of the ceremony later fails verification, so a caller cannot retry the SAME challenge against a
// tampered payload after a first rejected attempt. Splitting "read the challenge" from "verify
// everything else" is what lets handlers consume-then-verify instead of verify-then-consume.
#endregion

namespace TimeWarp.Identity;

public static class WebAuthnChallengeReader
{
  public static bool TryReadChallenge(byte[] clientDataJson, out byte[] challenge)
  {
    if (ClientData.TryParse(clientDataJson, out ClientData? clientData) && clientData is not null
      && Base64UrlHelpers.TryDecode(clientData.Challenge, out byte[] decoded))
    {
      challenge = decoded;
      return true;
    }

    challenge = [];
    return false;
  }
}
