#region Purpose
// The browser model-context seam tests can replace and production fills with JS interop.
#endregion

#region Design
// The host registers the JS implementation as its concrete type, not as this interface, so
// GetService of the interface stays empty unless a test adds one. Apply goes through
// WebMcpRegistration, which treats a null context as an absent API.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Replaces the tools visible to an external browser agent.</summary>
public interface IWebMcpModelContext
{
  ValueTask<WebMcpApplyResult> ReplaceAsync
  (
    IReadOnlyList<WebMcpToolDescriptor> tools,
    CancellationToken cancellationToken
  );
}
