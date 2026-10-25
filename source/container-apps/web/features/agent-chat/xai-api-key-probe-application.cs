#region Purpose
// Checks an xAI API key with a cheap call before it is stored.
#endregion

#region Design
// A failure does not save. The problem detail is safe to show: it never contains the key.
// The server implementation calls GET {endpoint}/models. Tests substitute this port.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats.Application;

/// <summary>Confirms a key with xAI. The key is an argument and is not logged.</summary>
public interface IXaiApiKeyProbe
{
  /// <summary>True when xAI accepted the key. A problem means do not store it.</summary>
  Task<OneOf<bool, SharedProblemDetails>> ValidateAsync(string apiKey, CancellationToken cancellationToken);
}
