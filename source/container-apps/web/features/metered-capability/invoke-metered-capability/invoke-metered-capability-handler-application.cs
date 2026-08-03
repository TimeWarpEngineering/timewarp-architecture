#region Purpose
// Server-side handler for the metered capability demo: bill credit or x402, then return a demo payload.
#endregion

#region Design
// Maps MeteredCapabilityGate outcomes to OneOf + payment headers via IPaymentHttpContext:
//   Granted → 200 Response (optional PAYMENT-RESPONSE)
//   Challenge / Rejected → 402 SharedProblemDetails + PAYMENT-REQUIRED
//   Unavailable → 503 SharedProblemDetails with payment error body fields in Extensions
// IAgentCallerContext null is defense-in-depth (endpoint is [EndpointAuthorize] demo:invoke).
// Free routes never reach this handler. Distinct from tip: always debits on success.
#endregion

namespace TimeWarp.Architecture.Features.MeteredCapability.Application;

using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Abstractions;
using TimeWarp.Architecture.Features;
using TimeWarp.X402;
using static TimeWarp.Architecture.Features.MeteredCapability.InvokeMeteredCapability;

public sealed partial class InvokeMeteredCapability
{
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IAgentCallerContext CallerContext;
    private readonly MeteredCapabilityGate Gate;
    private readonly IOptions<MeteredCapabilityOptions> Options;
    private readonly IPaymentHttpContext PaymentHttp;

    public Handler(
      IAgentCallerContext callerContext,
      MeteredCapabilityGate gate,
      IOptions<MeteredCapabilityOptions> options,
      IPaymentHttpContext paymentHttp)
    {
      CallerContext = callerContext;
      Gate = gate;
      Options = options;
      PaymentHttp = paymentHttp;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query query,
      CancellationToken cancellationToken)
    {
      AgentCaller? caller = CallerContext.GetCurrentCaller();
      if (caller is null)
      {
        // Defense-in-depth: endpoint policy should have rejected already (IdentityProblems is
        // identity-slice-internal — do not cross-slice-reference it).
        return new SharedProblemDetails
        {
          Title = "Unauthorized",
          Status = 401,
          Detail = "A valid agent bearer token is required.",
        };
      }

      var paymentOptions = Options.Value.ToPaymentOptions();
      MeteredCapabilityOutcome outcome = await Gate
        .EvaluateAsync(
          caller.PrincipalId,
          paymentOptions,
          PaymentHttp.PaymentSignatureHeader,
          cancellationToken)
        .ConfigureAwait(false);

      return outcome switch
      {
        MeteredCapabilityGranted granted => MapGranted(granted),
        MeteredCapabilityChallenge challenge => MapChallenge(challenge, invalidReason: null),
        MeteredCapabilityRejected rejected => MapChallenge(
          new MeteredCapabilityChallenge(rejected.Payload, rejected.PaymentRequiredHeader),
          rejected.Reason),
        MeteredCapabilityUnavailable unavailable => MapUnavailable(unavailable),
        _ => new SharedProblemDetails
        {
          Title = "Unexpected payment outcome",
          Status = 500,
          Detail = $"Unhandled outcome type: {outcome.GetType().Name}",
        },
      };
    }

    private Response MapGranted(MeteredCapabilityGranted granted)
    {
      if (!string.IsNullOrWhiteSpace(granted.PaymentResponseHeader))
      {
        PaymentHttp.SetPaymentResponseHeader(granted.PaymentResponseHeader);
      }

      return new Response(
        "Metered capability delivered.",
        granted.BalanceAfterDebit,
        granted.FundingSource);
    }

    private SharedProblemDetails MapChallenge(MeteredCapabilityChallenge challenge, string? invalidReason)
    {
      PaymentHttp.SetPaymentRequiredHeader(challenge.PaymentRequiredHeader);

      SharedProblemDetails problem = new()
      {
        Title = "Payment required",
        Status = 402,
        Detail = string.IsNullOrWhiteSpace(invalidReason)
          ? "This capability requires prepaid credit or a valid x402 payment."
          : $"Payment rejected: {invalidReason}. Present a valid PAYMENT-SIGNATURE or fund credit.",
      };
      problem.Extensions["payment"] = true;
      if (!string.IsNullOrWhiteSpace(invalidReason))
      {
        problem.Extensions["reason"] = invalidReason;
      }

      return problem;
    }

    private static SharedProblemDetails MapUnavailable(MeteredCapabilityUnavailable unavailable)
    {
      PaymentErrorPayload payload = unavailable.ToErrorPayload();
      SharedProblemDetails problem = new()
      {
        Title = "Payment unavailable",
        Status = 503,
        Detail = payload.Message,
      };
      problem.Extensions["payment"] = payload.Payment;
      problem.Extensions["error"] = payload.Error;
      problem.Extensions["ok"] = payload.Ok;
      return problem;
    }
  }
}
