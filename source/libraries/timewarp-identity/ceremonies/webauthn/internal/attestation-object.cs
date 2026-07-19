#region Purpose
// Parses the top-level attestationObject CBOR map { fmt, attStmt, authData } produced during
// registration, extracting only fmt (unused — kept for structural completeness) and authData.
#endregion

#region Design
// attStmt is deliberately never decoded: this template requests attestation "none" and, per the
// CENTRAL DECISION in webauthn-registration.cs, ignores attStmt regardless of what fmt an
// authenticator actually sent (including "packed" with garbage attStmt content) — SkipValue
// consumes whatever CBOR value is there (map, byte string, anything) without interpreting it.
// fmt itself is still required to be present as a basic structural sanity check (a well-formed
// attestationObject always has one), even though its value is discarded.
#endregion

namespace TimeWarp.Identity;

internal static class AttestationObject
{
  private const string FmtKey = "fmt";
  private const string AuthDataKey = "authData";

  public static bool TryParse(byte[] attestationObject, out byte[]? authenticatorData)
  {
    authenticatorData = null;

    try
    {
      var reader = new CborReader(attestationObject);
      reader.ReadStartMap();

      string? fmt = null;
      byte[]? authData = null;

      while (reader.PeekState() != CborReaderState.EndMap)
      {
        string key = reader.ReadTextString();
        switch (key)
        {
          case FmtKey:
            fmt = reader.ReadTextString();
            break;
          case AuthDataKey:
            authData = reader.ReadByteString();
            break;
          default:
            reader.SkipValue();
            break;
        }
      }

      reader.ReadEndMap();

      if (fmt is null || authData is null) return false;

      authenticatorData = authData;
      return true;
    }
    catch (Exception ex) when (ex is CborContentException or InvalidOperationException)
    {
      authenticatorData = null;
      return false;
    }
  }
}
