#region Purpose
// Applies a WebMCP tool list, including the absent-API case.
#endregion

#region Design
// A null context registers nothing and does not call a replacement. Feature detection of
// registerTool and provideContext lives in the JS module; this method is the C# contract both
// the publisher and the tests share.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Registers page tools with a model context when the browser has one.</summary>
public static class WebMcpRegistration
{
  public static async ValueTask<WebMcpApplyResult> ApplyAsync
  (
    IWebMcpModelContext? context,
    IReadOnlyList<WebMcpToolDescriptor> tools,
    CancellationToken cancellationToken
  )
  {
    if (context is null)
    {
      return new WebMcpApplyResult(Available: false, Registered: 0);
    }

    ArgumentNullException.ThrowIfNull(tools);
    return await context.ReplaceAsync(tools, cancellationToken);
  }
}
