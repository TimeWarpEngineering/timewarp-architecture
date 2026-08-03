#region Purpose
// Application port for reading PAYMENT-SIGNATURE and writing PAYMENT-REQUIRED / PAYMENT-RESPONSE headers.
#endregion

#region Design
// BaseFastEndpoint maps OneOf success/problem to status + JSON body but does not set custom headers.
// x402 buyers expect PAYMENT-* headers; paid handlers (tip, metered) therefore need a host-facing port.
// Server implementation (HttpPaymentHttpContext) uses IHttpContextAccessor; the application layer
// stays free of ASP.NET types. Free routes never use this port.
//
// Lives in the Features substrate (not a product slice) so MeteredCapability and Tip can share the
// port without TWA0009 cross-slice references (task 104-009 / 104-011).
#endregion

namespace TimeWarp.Architecture.Features;

/// <summary>Ambient payment header I/O for the current HTTP request/response.</summary>
public interface IPaymentHttpContext
{
  /// <summary>Raw <c>PAYMENT-SIGNATURE</c> header value, or null when absent.</summary>
  string? PaymentSignatureHeader { get; }

  /// <summary>Sets <c>PAYMENT-REQUIRED</c> on the response (402 path).</summary>
  void SetPaymentRequiredHeader(string headerValue);

  /// <summary>Sets <c>PAYMENT-RESPONSE</c> on the response (settled path).</summary>
  void SetPaymentResponseHeader(string headerValue);
}
