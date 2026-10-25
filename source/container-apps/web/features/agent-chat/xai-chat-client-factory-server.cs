#region Purpose
// Builds the xAI chat client for one API key. Tests replace it so a completion proves which key was used.
#endregion

#region Design
// The upstream caches one client per key up to a bound and disposes a client that leaves the
// cache once no completion is inside GetResponseAsync. The key is an argument and is not logged.
// The OpenAI client itself is not disposed; the IChatClient pipeline is.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;

/// <summary>Creates the chat client Ask will call with a resolved key.</summary>
public interface IXaiChatClientFactory
{
  /// <summary>A client authenticated as <paramref name="apiKey"/>.</summary>
  IChatClient Create(string apiKey, string endpoint, string model);
}

/// <summary>The production factory. Chat completions only.</summary>
internal sealed class OpenAiXaiChatClientFactory : IXaiChatClientFactory
{
  /// <inheritdoc />
  public IChatClient Create(string apiKey, string endpoint, string model)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
    ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
    ArgumentException.ThrowIfNullOrWhiteSpace(model);
    OpenAIClient client = new(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(endpoint) });
    return client.GetChatClient(model).AsIChatClient();
  }
}
