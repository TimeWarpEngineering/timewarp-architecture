namespace TimeWarp.Architecture.Features;

/// <summary>
/// Constructs a URL-encoded query string from the specified <see cref="NameValueCollection"/>.
/// </summary>
/// <param name="apiRequest">The <see cref="IApiRequest"/> instance on which the extension method is called.</param>
/// <param name="parameters">The collection of name and value pairs to convert into a query string.</param>
/// <returns>A URL-encoded query string.</returns>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="parameters"/> is null.</exception>
/// <remarks>
/// This method iterates through all keys in the <paramref name="parameters"/> collection
/// and adds them to a query string, ignoring any keys that have null or empty values.
/// The resulting query string is properly URL-encoded.
/// </remarks>
public static class ApiRequestExtensions
{
  public static string GetQueryString(this IApiRequest apiRequest, NameValueCollection parameters)
  {
    ArgumentNullException.ThrowIfNull(parameters);

    NameValueCollection queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
    foreach (string? key in parameters.AllKeys)
    {
      // Assuming parameters.AllKeys does not contain null based on NameValueCollection behavior
      string? value = parameters[key];
      if (!string.IsNullOrEmpty(value))
      {
        queryString[key] = value;
      }
    }

    // Check for null before returning.
    // Although in this context, queryString.ToString() should never be null.
    return queryString.ToString() ?? string.Empty;
  }
}
