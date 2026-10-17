#region Purpose
// Optional text rendering a CLI or MCP host consults before it writes JSON.
#endregion

#region Design
// Registration is the switch: a host looks up a renderer for the concrete response type.
// HTTP hosts ignore the interface. The renderer returns text; it does not choose a content type.
#endregion

namespace TimeWarp.Contracts;

/// <summary>
/// Text form of <typeparamref name="TResponse"/> for a CLI or MCP host.
/// HTTP hosts do not consult it. With no renderer registered, the host writes JSON.
/// </summary>
/// <typeparam name="TResponse">Response type the renderer accepts.</typeparam>
public interface IResponseRenderer<TResponse>
{
  /// <summary>Renders <paramref name="response"/> as text.</summary>
  /// <param name="response">Response to render.</param>
  /// <returns>Text the host writes for the response.</returns>
  string Render(TResponse response);
}
