#region Purpose
// Enum of HTTP methods so contracts can declare their verb without depending on System.Net.Http's HttpMethod class.
#endregion

namespace TimeWarp.Foundation.Features;

/// <summary>
/// HTTP methods available on contract <see cref="IApiRequest"/> routes.
/// </summary>
public enum HttpVerb
{
  /// <summary>HTTP GET.</summary>
  Get,
  /// <summary>HTTP POST.</summary>
  Post,
  /// <summary>HTTP DELETE.</summary>
  Delete,
  /// <summary>HTTP PUT.</summary>
  Put,
  /// <summary>HTTP PATCH.</summary>
  Patch,
  /// <summary>HTTP HEAD.</summary>
  Head,
  /// <summary>HTTP OPTIONS.</summary>
  Options
}
