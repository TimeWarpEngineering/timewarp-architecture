#region Purpose
// Builds x402 v2 PAYMENT-REQUIRED header values (Base64 JSON) for configured paid resources.
#endregion

#region Design
// Challenge emission is gated by PaymentConfigEvaluator: callers must not build a challenge when
// status is not Ready (that would 402 a misconfigured surface). Encoding is standard Base64 of
// UTF-8 JSON — matches @x402/fetch buyer expectations used by the timewarp-software tip jar.
// Free routes must never call this builder.
#endregion

namespace TimeWarp.X402;

using System.Text;
using System.Text.Json;
/// <summary>Builds payment-required challenges from <see cref="PaymentOptions"/>.</summary>
public static class PaymentChallengeBuilder
{
  private static readonly JsonSerializerOptions SerializerOptions = new()
  {
    PropertyNamingPolicy = null,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
  };

  /// <summary>
  /// Builds the payment-required payload and its Base64 header value.
  /// Throws <see cref="InvalidOperationException"/> if options are not <see cref="PaymentConfigStatus.Ready"/>.
  /// </summary>
  public static (PaymentRequiredPayload Payload, string HeaderValue) Build(PaymentOptions options)
  {
    ArgumentNullException.ThrowIfNull(options);

    PaymentConfigEvaluation evaluation = PaymentConfigEvaluator.Evaluate(options);
    if (evaluation.Status != PaymentConfigStatus.Ready)
    {
      throw new InvalidOperationException(
        $"Cannot build payment challenge when config is {evaluation.Status}: {evaluation.Message}");
    }

    PaymentRequiredPayload payload = new()
    {
      X402Version = options.X402Version,
      Accepts =
      [
        new PaymentAccept
        {
          Scheme = options.Scheme,
          Network = options.Network,
          PayTo = options.PayTo,
          Price = options.Price,
          Description = options.Description,
          MimeType = options.MimeType,
          Asset = options.Asset,
        },
      ],
      Resource = new PaymentResource
      {
        Path = options.Resource,
        Description = options.Description,
      },
    };

    string json = JsonSerializer.Serialize(payload, SerializerOptions);
    string header = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    return (payload, header);
  }

  /// <summary>Encodes an arbitrary settlement/response object as a Base64 JSON header value.</summary>
  public static string EncodeHeaderPayload<T>(T value)
  {
    string json = JsonSerializer.Serialize(value, SerializerOptions);
    return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
  }

  /// <summary>Decodes a Base64 JSON header into UTF-8 JSON text. Returns null if malformed.</summary>
  public static string? TryDecodeHeaderPayload(string? headerValue)
  {
    if (string.IsNullOrWhiteSpace(headerValue))
    {
      return null;
    }

    try
    {
      byte[] bytes = Convert.FromBase64String(headerValue.Trim());
      return Encoding.UTF8.GetString(bytes);
    }
    catch (FormatException)
    {
      return null;
    }
  }
}
