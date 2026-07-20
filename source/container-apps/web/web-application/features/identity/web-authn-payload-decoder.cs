#region Purpose
// Base64url decode helper shared by the identity ceremony completion handlers — contract fields
// arrive as base64url strings; WebAuthnRegistration/WebAuthnAuthentication.Verify take byte[].
#endregion

#region Design
// Duplicates timewarp-identity's internal Base64UrlHelpers.TryDecode (an internal type, not visible
// across the assembly boundary — no InternalsVisibleTo is declared, deliberately: the library
// should not need to know this handler assembly exists). This is purely a
// System.Buffers.Text.Base64Url wrapper — small, stable, deliberate duplication rather than growing
// the library's public API surface for a one-shot handler convenience.
// Base64Url.TryDecodeFromChars's "Try" contract only covers destination-buffer sizing — for
// genuinely invalid input characters it throws FormatException rather than returning false, so this
// wrapper catches that to give callers a true Try* (never throws on adversarial contract input).
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using System.Buffers.Text;

internal static class WebAuthnPayloadDecoder
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
