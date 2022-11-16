namespace TimeWarp.Architecture.Features;

/// <summary>
/// Utlimate Base Class for Requests and Responses
/// </summary>
public abstract record BaseMessage
{
  /// <summary>
  /// Unique Identifier to Correlate request and response
  /// </summary>
  public Guid CorrelationId { get; init; } = Guid.NewGuid();
}
