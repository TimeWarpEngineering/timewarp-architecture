#region Purpose
// Decides whether a configured Ask Support url may go into an anchor href.
#endregion

#region Design
// SupportUrl arrives from the chat configuration endpoint and is rendered into href. Only an
// app-relative path ("/x", not the protocol-relative "//host" or "/\host") or an absolute http or
// https url is accepted. Anything else (javascript:, data:, a bare word, empty) falls back to
// XaiChatDefaults.SupportUrl. LoadChatConfiguration applies this once, so the component trusts state.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Scheme check for the Ask Support link.</summary>
public static class AskSupportLink
{
  public static string Normalize(string? configured)
  {
    if (string.IsNullOrWhiteSpace(configured))
    {
      return XaiChatDefaults.SupportUrl;
    }

    string value = configured.Trim();
    if (value.StartsWith('/', StringComparison.Ordinal))
    {
      bool protocolRelative = value.Length > 1 && (value[1] == '/' || value[1] == '\\');
      return protocolRelative ? XaiChatDefaults.SupportUrl : value;
    }

    if (Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
      && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp))
    {
      return value;
    }

    return XaiChatDefaults.SupportUrl;
  }
}
