#region Purpose
// Locked 503-vs-402 policy: disabled/misconfigured never become Ready (no challenge emission).
#endregion

namespace PaymentConfigEvaluator_;

public class Evaluate
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Evaluate>();

  private const string ValidPayTo = "0x000000000000000000000000000000000000dEaD";

  public static Task Disabled_when_not_enabled()
  {
    PaymentOptions options = ReadyOptions() with { Enabled = false };
    PaymentConfigEvaluation result = PaymentConfigEvaluator.Evaluate(options);
    result.Status.ShouldBe(PaymentConfigStatus.Disabled);
    result.ErrorCode.ShouldBe(PaymentConfigEvaluator.ErrorDisabled);
    return Task.CompletedTask;
  }

  public static Task Misconfigured_when_pay_to_invalid()
  {
    PaymentOptions options = ReadyOptions() with { PayTo = "0x0000000000000000000000000000000000000000" };
    PaymentConfigEvaluation result = PaymentConfigEvaluator.Evaluate(options);
    result.Status.ShouldBe(PaymentConfigStatus.Misconfigured);
    result.ErrorCode.ShouldBe(PaymentConfigEvaluator.ErrorMisconfigured);
    return Task.CompletedTask;
  }

  public static Task Misconfigured_when_facilitator_auth_required_but_missing()
  {
    PaymentOptions options = ReadyOptions() with
    {
      RequiresFacilitatorAuth = true,
      HasFacilitatorAuth = false,
    };
    PaymentConfigEvaluation result = PaymentConfigEvaluator.Evaluate(options);
    result.Status.ShouldBe(PaymentConfigStatus.Misconfigured);
    return Task.CompletedTask;
  }

  public static Task Ready_for_valid_testnet_options()
  {
    PaymentConfigEvaluation result = PaymentConfigEvaluator.Evaluate(ReadyOptions());
    result.Status.ShouldBe(PaymentConfigStatus.Ready);
    result.ErrorCode.ShouldBeNull();
    return Task.CompletedTask;
  }

  private static PaymentOptions ReadyOptions() =>
    PaymentOptions.CreateTestnetDefaults(ValidPayTo, "/api/tip");
}
