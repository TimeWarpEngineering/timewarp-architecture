#region Purpose
// Picks the app path a feedback filing records: the page the filer was on when they opened the form.
#endregion

#region Design
// Task 521. Feedback's form is its own route (/Feedback), so "the page the filer was on" is the
// newest visited address outside the feedback pages: NavigationHistory lists visits newest first
// and never truncates (RouteState.Routes does, on a repeat visit), and /Feedback and
// /Feedback/{id} are skipped. A filing
// from anywhere else (the Ask thumbs, an agent call) is the current page, which is the newest
// route already. Only the path is kept: the base URI is removed and the query string and fragment
// are cut, so a token or an email in a query value never leaves the browser. A path the server
// would refuse (SubmitFeedback.IsPagePath, for example too long or containing whitespace) becomes
// null so the filing still succeeds without it. Null too when no route outside feedback was
// visited (the form was opened directly).
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

public static class FeedbackPagePath
{
  /// <summary>
  /// The app path to file: <paramref name="current"/> unless it is a feedback page, else the
  /// newest of <paramref name="visited"/> (absolute addresses, newest first) that is not.
  /// Null when none qualifies.
  /// </summary>
  public static string? From(Uri appBase, Uri current, IEnumerable<string> visited)
  {
    ArgumentNullException.ThrowIfNull(appBase);
    ArgumentNullException.ThrowIfNull(current);
    ArgumentNullException.ThrowIfNull(visited);
    IEnumerable<Uri> candidates = visited
      .Select(static address => Uri.TryCreate(address, UriKind.Absolute, out Uri? parsed) ? parsed : null)
      .OfType<Uri>()
      .Prepend(current);
    foreach (Uri candidate in candidates)
    {
      string? path = PathOf(appBase, candidate);
      if (path is not null && !IsFeedbackPage(path))
      {
        return SubmitFeedback.IsPagePath(path) ? path : null;
      }
    }

    return null;
  }

  /// <summary>
  /// The app-relative path of <paramref name="address"/> (no query or fragment; Uri.AbsolutePath
  /// drops both), or null when it is outside <paramref name="appBase"/>.
  /// </summary>
  public static string? PathOf(Uri appBase, Uri address)
  {
    ArgumentNullException.ThrowIfNull(appBase);
    ArgumentNullException.ThrowIfNull(address);
    if (!appBase.IsBaseOf(address))
    {
      return null;
    }

    string basePath = appBase.AbsolutePath;
    string path = address.AbsolutePath;
    string relative = path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase) ? path[basePath.Length..] : path;
    return "/" + relative.TrimStart('/');
  }

  private static bool IsFeedbackPage(string path)
  {
    string feedback = FeedbackListPage.GetPageUrl();
    return string.Equals(path, feedback, StringComparison.OrdinalIgnoreCase)
      || path.StartsWith(feedback + "/", StringComparison.OrdinalIgnoreCase);
  }
}
