#region Purpose
// Credit / debit / balance / idempotent receipt application for InMemoryCreditLedger.
#endregion

namespace CreditLedger_;

using TimeWarp.Identity;

public class InMemoryCreditLedgerTests
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<InMemoryCreditLedgerTests>();

  public static async Task Credit_increases_balance()
  {
    InMemoryCreditLedger ledger = new();
    PrincipalId id = PrincipalId.New();

    decimal balance = await ledger.CreditAsync(id, 1.25m, "receipt-1");
    balance.ShouldBe(1.25m);
    (await ledger.GetBalanceAsync(id)).ShouldBe(1.25m);
  }

  public static async Task Credit_is_idempotent_on_receipt_id()
  {
    InMemoryCreditLedger ledger = new();
    PrincipalId id = PrincipalId.New();

    await ledger.CreditAsync(id, 1.00m, "same-receipt");
    decimal second = await ledger.CreditAsync(id, 1.00m, "same-receipt");

    second.ShouldBe(1.00m);
    (await ledger.GetBalanceAsync(id)).ShouldBe(1.00m);
  }

  public static async Task Debit_reduces_balance()
  {
    InMemoryCreditLedger ledger = new();
    PrincipalId id = PrincipalId.New();
    await ledger.CreditAsync(id, 2.00m, "r1");

    decimal after = await ledger.DebitAsync(id, 0.75m, "op-1");
    after.ShouldBe(1.25m);
  }

  public static async Task Debit_fails_closed_when_insufficient()
  {
    InMemoryCreditLedger ledger = new();
    PrincipalId id = PrincipalId.New();
    await ledger.CreditAsync(id, 0.10m, "r1");

    InsufficientCreditException ex = await Should.ThrowAsync<InsufficientCreditException>(
      () => ledger.DebitAsync(id, 1.00m));

    ex.Requested.ShouldBe(1.00m);
    ex.Available.ShouldBe(0.10m);
    (await ledger.GetBalanceAsync(id)).ShouldBe(0.10m);
  }

  public static async Task Unknown_principal_balance_is_zero()
  {
    InMemoryCreditLedger ledger = new();
    (await ledger.GetBalanceAsync(PrincipalId.New())).ShouldBe(0m);
  }
}
