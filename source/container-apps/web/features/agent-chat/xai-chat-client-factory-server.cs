#region Purpose
// Builds the xAI chat client for one API key. Tests replace it so a completion proves which key was used.
#endregion

#region Design
// The upstream caches one client per key and sensitive-telemetry flag up to a bound, and disposes
// a client that leaves the cache once no completion is inside GetResponseAsync. The key is an
// argument and is not logged. The OpenAI client itself is not disposed; the IChatClient pipeline is.
// The pipeline's outer stage is Microsoft.Extensions.AI 11.0.1 OpenTelemetryChatClient. sourceName
// is GenAiTelemetry.SourceName, which that package also uses as the meter name. EnableSensitiveData
// follows the flag passed in, which is XaiChatOptions.EnableSensitiveTelemetry at rent time.
#endregion

namespace TimeWarp.Architecture.Features.AgentChats;

using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using OpenAI;

/// <summary>Creates the chat client Ask will call with a resolved key.</summary>
public interface IXaiChatClientFactory
{
  /// <summary>
  /// A client authenticated as <paramref name="apiKey"/>.
  /// <paramref name="enableSensitiveTelemetry"/> records prompt and reply text on the GenAI span.
  /// </summary>
  IChatClient Create(string apiKey, string endpoint, string model, bool enableSensitiveTelemetry);
}

/// <summary>The production factory. Chat completions only.</summary>
internal sealed class OpenAiXaiChatClientFactory : IXaiChatClientFactory
{
  private readonly ILoggerFactory LoggerFactory;

  public OpenAiXaiChatClientFactory(ILoggerFactory loggerFactory)
  {
    ArgumentNullException.ThrowIfNull(loggerFactory);
    LoggerFactory = loggerFactory;
  }

  /// <inheritdoc />
  public IChatClient Create(string apiKey, string endpoint, string model, bool enableSensitiveTelemetry)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
    ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
    ArgumentException.ThrowIfNullOrWhiteSpace(model);
    OpenAIClient client = new(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = new Uri(endpoint) });
    return Instrument(client.GetChatClient(model).AsIChatClient(), LoggerFactory, enableSensitiveTelemetry);
  }

  /// <summary>
  /// Wraps <paramref name="inner"/> with GenAI OpenTelemetry. Tests pass a stub so a completion
  /// does not dial xAI.
  /// </summary>
  internal static IChatClient Instrument(
    IChatClient inner,
    ILoggerFactory loggerFactory,
    bool enableSensitiveTelemetry)
  {
    ArgumentNullException.ThrowIfNull(inner);
    ArgumentNullException.ThrowIfNull(loggerFactory);
    return inner
      .AsBuilder()
      .UseOpenTelemetry
      (
        loggerFactory,
        sourceName: GenAiTelemetry.SourceName,
        configure: client => client.EnableSensitiveData = enableSensitiveTelemetry
      )
      .Build(services: null);
  }
}
