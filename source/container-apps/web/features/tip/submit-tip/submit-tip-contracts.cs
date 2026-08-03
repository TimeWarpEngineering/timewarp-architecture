#region Purpose
// Endpoint-centric contract for the voluntary x402 tip jar: pay if you want; content stays free.
#endregion

#region Design
// Host: **web-server** (same as metered demo 104-011) so one place teaches Payment-* headers and
// PaymentGate mapping. Route is api/tip only — free/discovery routes never share this path.
// [EndpointAllowAnonymous]: tips are wallet-native; no human session or agent bearer required
// ("No human required if the agent pays" / voluntary gratuity for browsers and agents).
// Distinct from metered capability: no ledger debit, no agent-scope policy, PaymentGate only.
// GET hosted here; POST twin is SubmitTipPost (generator allows one Query|Command per outer type).
// No GetMockResponseFactory — payment headers are ambient, not SPA-mockable.
#endregion

namespace TimeWarp.Architecture.Features.Tip;

[ApiEndpoint]
[EndpointAllowAnonymous("Voluntary tip jar: wallet/x402 only; free site content never requires payment or auth.")]
public static partial class SubmitTip
{
  [ApiRoute("api/tip", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response
  {
    public string Message { get; }
    public string Amount { get; }
    public string Network { get; }
    public bool Tip { get; }

    public Response(string message, string amount, string network)
    {
      Message = Guard.Against.NullOrEmpty(message);
      Amount = Guard.Against.NullOrEmpty(amount);
      Network = Guard.Against.NullOrEmpty(network);
      Tip = true;
    }
  }
}

// POST twin of SubmitTip — same resource, same PaymentGate outcomes (tip.js GET|POST parity).
// Separate outer class because FastEndpoint generation picks one nested Query|Command per
// [ApiEndpoint] type. Nested Response is required by the FastEndpoint generator (must be
// SubmitTipPost.Response, not a cross-type reference to SubmitTip.Response).

[ApiEndpoint]
[EndpointAllowAnonymous("Voluntary tip jar POST twin: same resource as GET api/tip.")]
public static partial class SubmitTipPost
{
  [ApiRoute("api/tip", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Command>;

  /// <summary>Thank-you body — same fields as <see cref="SubmitTip.Response"/>.</summary>
  public sealed class Response
  {
    public string Message { get; }
    public string Amount { get; }
    public string Network { get; }
    public bool Tip { get; }

    public Response(string message, string amount, string network)
    {
      Message = Guard.Against.NullOrEmpty(message);
      Amount = Guard.Against.NullOrEmpty(amount);
      Network = Guard.Against.NullOrEmpty(network);
      Tip = true;
    }
  }
}
