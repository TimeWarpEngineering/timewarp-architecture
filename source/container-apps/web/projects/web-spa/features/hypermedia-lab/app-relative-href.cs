#region Purpose
// Same-origin guard for approach C: only app-relative hrefs ("/api/…") may be followed.
#endregion

#region Design
// A followed link carries the user's bearer token (IWebServerApiService) or the user's browser
// (NAVIGATE), so an href from a payload must not leave this origin. Accept only a path that starts
// with one "/" — reject absolute URIs (any scheme), protocol-relative "//host", the "/\host" form
// browsers normalise to "//host", any backslash, and whitespace/control characters. Fail closed:
// anything this check does not recognise is refused, never "fixed up".
#endregion

namespace TimeWarp.Architecture.Features.HypermediaLab;

public static class AppRelativeHref
{
  public static bool IsAppRelative([NotNullWhen(true)] string? href) =>
    !string.IsNullOrEmpty(href)
    && href[0] == '/'
    && (href.Length == 1 || (href[1] != '/' && href[1] != '\\'))
    && !href.Contains('\\', StringComparison.Ordinal)
    && !href.Any(static character => char.IsWhiteSpace(character) || char.IsControl(character))
    && Uri.TryCreate(href, UriKind.Relative, out _);
}
