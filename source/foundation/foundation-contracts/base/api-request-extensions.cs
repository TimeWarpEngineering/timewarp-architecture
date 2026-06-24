namespace TimeWarp.Architecture.Features;

public static class ApiRequestExtensions
{
  /// <summary>
  /// Constructs a URL-encoded query string from the specified <see cref="NameValueCollection"/>.
  /// </summary>
  /// <returns>A URL-encoded query string.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="parameters"/> is null.</exception>
  /// <remarks>
  /// This method iterates through all keys in the <paramref name="parameters"/> collection
  /// and adds them to a query string, ignoring any keys that have null or empty values.
  /// The resulting query string is properly URL-encoded.
  /// </remarks>
  public static string GetQueryString(this IApiRequest _, NameValueCollection parameters)
  {
    ArgumentNullException.ThrowIfNull(parameters);

    List<string> queryString = [];
    foreach (string? key in parameters.AllKeys)
    {
      if (key == null) continue;
      string[] values = parameters.GetValues(key) ?? Array.Empty<string>();
      IEnumerable<string> encodedValues = values
        .SelectMany(v => v.Split(','))
        .Where(v => !string.IsNullOrEmpty(v))
        .Select(Uri.EscapeDataString);
      IEnumerable<string> enumerable = encodedValues as string[] ?? encodedValues.ToArray();
      if (enumerable.Any())
      {
        queryString.Add($"{Uri.EscapeDataString(key)}={string.Join(separator: ",", enumerable)}");
      }
    }

    // Check for null before returning.
    // Although in this context, queryString.ToString() should never be null.
    return string.Join(separator: "&", queryString);
  }
}
