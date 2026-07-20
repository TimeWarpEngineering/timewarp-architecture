#region Purpose
// Allocating decode wrapper over the BCL's buffer-based System.Buffers.Text.Base64Url so the rest
// of the ceremony code can work with plain byte[] instead of pre-sized spans.
#endregion

#region Design
// BCL Base64Url only exposes Try*(source, destination-span, out written) overloads — no
// array-returning convenience method — so this wraps that shape once instead of repeating the
// "size a buffer, decode, trim" dance at every call site (client-data.cs, cose-key parsing is not
// base64 but challenge/handle values are).
// Base64Url.TryDecodeFromChars's "Try" contract only covers destination-buffer sizing — for
// genuinely invalid input characters it still THROWS FormatException rather than returning false
// (confirmed by test: a string containing '!' throws, not returns-false). This wrapper catches that
// so the rest of the ceremony code can treat TryDecode as a true Try* (never throws on adversarial
// input), matching webauthn-registration.cs/webauthn-authentication.cs's "no exceptions on
// non-null adversarial payloads" contract.
#endregion

namespace TimeWarp.Identity;

internal static class Base64UrlHelpers
{
  public static bool TryDecode(string? value, out byte[] result)
  {
    if (string.IsNullOrEmpty(value))
    {
      result = [];
      return false;
    }

    try
    {
      int maxLength = Base64Url.GetMaxDecodedLength(value.Length);
      byte[] buffer = new byte[maxLength];

      if (!Base64Url.TryDecodeFromChars(value, buffer, out int bytesWritten))
      {
        result = [];
        return false;
      }

      result = bytesWritten == buffer.Length ? buffer : buffer[..bytesWritten];
      return true;
    }
    catch (FormatException)
    {
      result = [];
      return false;
    }
  }
}
