#region Purpose
// Outcome of WebAuthnAuthentication.Verify: whether the assertion signature verified, or why it
// did not.
#endregion

#region Design
// No payload beyond IsValid/FailureReason — authentication proves possession of an already-known
// credential; the handler already has the Credential/Principal from FindCredentialByHandleAsync
// before calling Verify, so there is nothing new to hand back on success.
#endregion

namespace TimeWarp.Identity;

public sealed class WebAuthnAssertionResult
{
  private WebAuthnAssertionResult(bool isValid, WebAuthnFailureReason failureReason)
  {
    IsValid = isValid;
    FailureReason = failureReason;
  }

  public bool IsValid { get; }

  public WebAuthnFailureReason FailureReason { get; }

  internal static WebAuthnAssertionResult Success() => new(true, WebAuthnFailureReason.None);

  internal static WebAuthnAssertionResult Failure(WebAuthnFailureReason reason) => new(false, reason);
}
