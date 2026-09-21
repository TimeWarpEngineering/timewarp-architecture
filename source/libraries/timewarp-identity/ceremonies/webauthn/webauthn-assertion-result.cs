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

/// <summary>
/// Outcome of <see cref="WebAuthnAuthentication.Verify"/> — assertion verified, or a typed failure reason.
/// </summary>
public sealed class WebAuthnAssertionResult
{
  private WebAuthnAssertionResult(bool isValid, WebAuthnFailureReason failureReason)
  {
    IsValid = isValid;
    FailureReason = failureReason;
  }

  /// <summary>True when the assertion signature verified against the stored COSE public key.</summary>
  public bool IsValid { get; }

  /// <summary><see cref="WebAuthnFailureReason.None"/> on success; otherwise the reject cause.</summary>
  public WebAuthnFailureReason FailureReason { get; }

  internal static WebAuthnAssertionResult Success() => new(true, WebAuthnFailureReason.None);

  internal static WebAuthnAssertionResult Failure(WebAuthnFailureReason reason) => new(false, reason);
}
