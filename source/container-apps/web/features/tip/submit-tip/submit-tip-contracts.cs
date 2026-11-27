#region Purpose
// Endpoint-centric contract for the voluntary x402 tip jar: pay if you want; content stays free.
#endregion

#region Design
// Host: **web-server** (same as metered demo 104-011) so one place teaches Payment-* headers and
// PaymentGate mapping. Canonical route api/tip; bare /api is a discovery alias rewrite only
// (TipDiscoveryAlias, 104-020). Free/discovery content routes never share this path.
// [EndpointAllowAnonymous]: tips are wallet-native; no human session or agent bearer required
// ("No human required if the agent pays" / voluntary gratuity for browsers and agents).
// Distinct from metered capability: no ledger debit, no agent-scope policy, PaymentGate only.
// GET hosted here; POST twin is SubmitTipPost (generator allows one Query|Command per outer type).
// No GetMockResponseFactory — payment headers are ambient, not SPA-mockable.
// SubmitTip.Query stays bare IRequest, not IQuery: the GET takes a payment, so it is not read-only.
#endregion

namespace TimeWarp.Architecture.Features.Tip;

[ApiEndpoint]
[EndpointAllowAnonymous("Voluntary tip jar: wallet/x402 only; free site content never requires payment or auth.")]
public static partial class SubmitTip
{
  [ApiRoute("api/tip", HttpVerb.Get)]
  [Operation("tip.get", Description = "Read the voluntary tip jar thank-you after payment.", Group = "tip")]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response
  {
    /// <summary>The greeting.</summary>
    public string Message { get; }
    /// <summary>Amount of the tip.</summary>
    public string Amount { get; }
    /// <summary>Payment network.</summary>
    public string Network { get; }
    /// <summary>Always true; marks a tip response.</summary>
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
  [Operation("tip.submit", Description = "Submit a voluntary tip and receive the thank-you.", Group = "tip")]
  public sealed partial class Command : IApiRequest, ICommand<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Command>;

  /// <summary>Thank-you body — same fields as <see cref="SubmitTip.Response"/>.</summary>
  public sealed class Response
  {
    /// <summary>The greeting.</summary>
    public string Message { get; }
    /// <summary>Amount of the tip.</summary>
    public string Amount { get; }
    /// <summary>Payment network.</summary>
    public string Network { get; }
    /// <summary>Always true; marks a tip response.</summary>
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
