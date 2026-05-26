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
