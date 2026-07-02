#region Purpose
// Lets links build a page's href via static GetPageUrl() instead of hard-coded route strings; implemented on pages by the [Page] source generator.
#endregion

namespace TimeWarp.Architecture.Common.Interfaces;
public interface IStaticRoute
{
  [SuppressMessage
  (
    "Design",
    "CA1055:URI-like return values should not be strings",
    Justification = "Blazor route hrefs are relative route strings, not URI values."
  )]
  static abstract string GetPageUrl();
}
