#region Purpose
// ASP.NET implementation of IPaymentHttpContext using the ambient HttpContext.
#endregion

#region Design
// Scoped: ties to the request HttpContext from IHttpContextAccessor. Header names come from
// TimeWarp.X402.PaymentHeaders so hosts stay aligned with the protocol constants.
#endregion

namespace TimeWarp.Architecture.Features.MeteredCapability.Server;

using TimeWarp.Architecture.Features.MeteredCapability.Application;
using TimeWarp.X402;

public sealed class HttpPaymentHttpContext : IPaymentHttpContext
{
  private readonly IHttpContextAccessor Accessor;

  public HttpPaymentHttpContext(IHttpContextAccessor accessor)
  {
    Accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
  }

  public string? PaymentSignatureHeader
  {
    get
    {
      HttpContext? httpContext = Accessor.HttpContext;
      if (httpContext is null)
      {
        return null;
      }

      return httpContext.Request.Headers.TryGetValue(PaymentHeaders.PaymentSignature, out Microsoft.Extensions.Primitives.StringValues values)
        ? values.ToString()
        : null;
    }
  }

  public void SetPaymentRequiredHeader(string headerValue)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(headerValue);
    HttpContext httpContext = Accessor.HttpContext
      ?? throw new InvalidOperationException("No ambient HttpContext for payment headers.");
    httpContext.Response.Headers[PaymentHeaders.PaymentRequired] = headerValue;
  }

  public void SetPaymentResponseHeader(string headerValue)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(headerValue);
    HttpContext httpContext = Accessor.HttpContext
      ?? throw new InvalidOperationException("No ambient HttpContext for payment headers.");
    httpContext.Response.Headers[PaymentHeaders.PaymentResponse] = headerValue;
  }
}
