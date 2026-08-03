#region Purpose
// Server-side handlers for the voluntary tip jar: PaymentGate only — no ledger, no agent auth.
#endregion

#region Design
// Maps PaymentGate outcomes to OneOf + payment headers via IPaymentHttpContext:
//   Settled     → 200 thank-you (+ PAYMENT-RESPONSE)
//   Challenge / Rejected → 402 SharedProblemDetails + PAYMENT-REQUIRED
//   Unavailable → 503 SharedProblemDetails (tips_disabled / payment_misconfigured — never 402)
// Free routes never reach this handler. Distinct from metered: never debits ICreditLedger.
// GET (SubmitTip.Query) and POST (SubmitTipPost.Command) share TipPaymentMapper.
//
// Local testnet run:
//   TipOptions:Enabled=true (or TIP_ENABLED=true), TipOptions:PayTo / TIP_PAY_TO = your Base
//   Sepolia receive address, Network eip155:84532, FacilitatorBase https://x402.org/facilitator.
//   curl -si https://localhost:7000/api/tip → 402 + PAYMENT-REQUIRED when enabled unpaid.
//   Free routes (/, /llms.txt, etc.) never 402 from this feature.
//   Optional settle: fund a Sepolia wallet with USDC, present PAYMENT-SIGNATURE via an x402
//   buyer (see timewarp-software tools/tip-buyer) against this host's /api/tip.
#endregion

namespace TimeWarp.Architecture.Features.Tip.Application;

using Microsoft.Extensions.Options;
using TimeWarp.Architecture.Features;
using TimeWarp.X402;
using static TimeWarp.Architecture.Features.Tip.SubmitTip;

public sealed partial class SubmitTip
{
  public class Handler : IRequestHandler<Query, OneOf<Response, SharedProblemDetails>>
  {
    private readonly PaymentGate Gate;
    private readonly IOptions<TipOptions> Options;
    private readonly IPaymentHttpContext PaymentHttp;

    public Handler(
      PaymentGate gate,
      IOptions<TipOptions> options,
      IPaymentHttpContext paymentHttp)
    {
      Gate = gate;
      Options = options;
      PaymentHttp = paymentHttp;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(
      Query query,
      CancellationToken cancellationToken)
    {
      return await TipPaymentMapper
        .EvaluateAsync(Gate, Options.Value, PaymentHttp, cancellationToken)
        .ConfigureAwait(false);
    }
  }
}

public sealed partial class SubmitTipPost
{
  public class Handler : IRequestHandler<
    TimeWarp.Architecture.Features.Tip.SubmitTipPost.Command,
    OneOf<TimeWarp.Architecture.Features.Tip.SubmitTipPost.Response, SharedProblemDetails>>
  {
    private readonly PaymentGate Gate;
    private readonly IOptions<TipOptions> Options;
    private readonly IPaymentHttpContext PaymentHttp;

    public Handler(
      PaymentGate gate,
      IOptions<TipOptions> options,
      IPaymentHttpContext paymentHttp)
    {
      Gate = gate;
      Options = options;
      PaymentHttp = paymentHttp;
    }

    public async Task<OneOf<TimeWarp.Architecture.Features.Tip.SubmitTipPost.Response, SharedProblemDetails>> Handle(
      TimeWarp.Architecture.Features.Tip.SubmitTipPost.Command command,
      CancellationToken cancellationToken)
    {
      OneOf<Response, SharedProblemDetails> outcome = await TipPaymentMapper
        .EvaluateAsync(Gate, Options.Value, PaymentHttp, cancellationToken)
        .ConfigureAwait(false);

      return outcome.Match<OneOf<TimeWarp.Architecture.Features.Tip.SubmitTipPost.Response, SharedProblemDetails>>(
        thankYou => new TimeWarp.Architecture.Features.Tip.SubmitTipPost.Response(
          thankYou.Message, thankYou.Amount, thankYou.Network),
        problem => problem);
    }
  }
}

/// <summary>Shared PaymentGate → HTTP outcome mapping for GET and POST tip handlers.</summary>
internal static class TipPaymentMapper
{
  public static async Task<OneOf<Response, SharedProblemDetails>> EvaluateAsync(
    PaymentGate gate,
    TipOptions tipOptions,
    IPaymentHttpContext paymentHttp,
    CancellationToken cancellationToken)
  {
    var paymentOptions = tipOptions.ToPaymentOptions();
    PaymentGateOutcome outcome = await gate
      .EvaluateAsync(paymentOptions, paymentHttp.PaymentSignatureHeader, cancellationToken)
      .ConfigureAwait(false);

    return outcome switch
    {
      PaymentSettled settled => MapSettled(settled, tipOptions, paymentHttp),
      PaymentChallenge challenge => MapChallenge(challenge, paymentHttp, invalidReason: null),
      PaymentRejected rejected => MapChallenge(
        new PaymentChallenge(rejected.Payload, rejected.PaymentRequiredHeader),
        paymentHttp,
        rejected.Reason),
      PaymentUnavailable unavailable => MapUnavailable(unavailable),
      _ => new SharedProblemDetails
      {
        Title = "Unexpected payment outcome",
        Status = 500,
        Detail = $"Unhandled outcome type: {outcome.GetType().Name}",
      },
    };
  }

  private static Response MapSettled(
    PaymentSettled settled,
    TipOptions tipOptions,
    IPaymentHttpContext paymentHttp)
  {
    if (!string.IsNullOrWhiteSpace(settled.PaymentResponseHeader))
    {
      paymentHttp.SetPaymentResponseHeader(settled.PaymentResponseHeader);
    }

    return new Response(
      "Thank you for supporting TimeWarp Engineering.",
      tipOptions.Price,
      tipOptions.Network);
  }

  private static SharedProblemDetails MapChallenge(
    PaymentChallenge challenge,
    IPaymentHttpContext paymentHttp,
    string? invalidReason)
  {
    paymentHttp.SetPaymentRequiredHeader(challenge.PaymentRequiredHeader);

    SharedProblemDetails problem = new()
    {
      Title = "Payment required",
      Status = 402,
      Detail = string.IsNullOrWhiteSpace(invalidReason)
        ? "Voluntary tip: present a valid PAYMENT-SIGNATURE to tip. Site content remains free."
        : $"Payment rejected: {invalidReason}. Present a valid PAYMENT-SIGNATURE to tip.",
    };
    problem.Extensions["tip"] = true;
    problem.Extensions["payment"] = true;
    if (!string.IsNullOrWhiteSpace(invalidReason))
    {
      problem.Extensions["reason"] = invalidReason;
    }

    return problem;
  }

  private static SharedProblemDetails MapUnavailable(PaymentUnavailable unavailable)
  {
    PaymentErrorPayload payload = unavailable.ToErrorPayload();
    // tip.js used tips_disabled / tips_misconfigured; library codes are payment_*.
    // Prefer tip-flavored codes when disabled so buyers match the software spike docs.
    string error = unavailable.Status == PaymentConfigStatus.Disabled
      ? "tips_disabled"
      : payload.Error;

    SharedProblemDetails problem = new()
    {
      Title = "Tips unavailable",
      Status = 503,
      Detail = unavailable.Status == PaymentConfigStatus.Disabled
        ? "Voluntary tips are not enabled on this deployment."
        : payload.Message,
    };
    problem.Extensions["tip"] = true;
    problem.Extensions["payment"] = payload.Payment;
    problem.Extensions["error"] = error;
    problem.Extensions["ok"] = payload.Ok;
    return problem;
  }
}
