#region Purpose
// SettlementFundingService: settle → credit + Funded; debit does not demote tier.
#endregion

#region Design
// Library-level Identity ↔ 402 composition (104-013). Uses InMemoryPrincipalStore + InMemoryCreditLedger.
// Metered gate tests cover the pay-then-fund path; this suite owns tier transition and non-demotion.
#endregion

namespace SettlementFundingService_;

using TimeWarp.Identity;
using TimeWarp.X402;

public class ApplyAsync
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<ApplyAsync>();

  public static async Task Credits_ledger_and_promotes_keyed_principal_to_funded()
  {
    (InMemoryPrincipalStore store, Principal principal) = await SeedKeyedAgentAsync();
    InMemoryCreditLedger ledger = new();
    SettlementFundingService funding = new(ledger, store);

    SettlementFundingResult result = await funding.ApplyAsync(
      principal.Id,
      amount: 0.10m,
      receiptId: "0xsettle-fund-1");

    result.BalanceAfterCredit.ShouldBe(0.10m);
    result.PromotedToFunded.ShouldBeTrue();
    (await ledger.GetBalanceAsync(principal.Id)).ShouldBe(0.10m);

    Principal? reloaded = await store.GetPrincipalAsync(principal.Id);
    reloaded.ShouldNotBeNull();
    reloaded.TrustTier.ShouldBe(TrustTier.Funded);
    reloaded.IsFundedAndActive.ShouldBeTrue();
  }

  public static async Task Promotes_provisional_principal_directly_to_funded()
  {
    InMemoryPrincipalStore store = new();
    Principal principal = Principal.Create(PrincipalKind.Agent);
    await store.AddPrincipalAsync(principal);

    SettlementFundingService funding = new(new InMemoryCreditLedger(), store);
    SettlementFundingResult result = await funding.ApplyAsync(
      principal.Id,
      1.00m,
      "0xprovisional-pay");

    result.PromotedToFunded.ShouldBeTrue();
    (await store.GetPrincipalAsync(principal.Id))!.TrustTier.ShouldBe(TrustTier.Funded);
  }

  public static async Task Second_settle_is_idempotent_on_receipt_and_does_not_re_promote()
  {
    (InMemoryPrincipalStore store, Principal principal) = await SeedKeyedAgentAsync();
    InMemoryCreditLedger ledger = new();
    SettlementFundingService funding = new(ledger, store);
    const string Receipt = "0xidempotent-settle";

    SettlementFundingResult first = await funding.ApplyAsync(principal.Id, 0.25m, Receipt);
    SettlementFundingResult second = await funding.ApplyAsync(principal.Id, 0.25m, Receipt);

    first.PromotedToFunded.ShouldBeTrue();
    first.BalanceAfterCredit.ShouldBe(0.25m);
    second.PromotedToFunded.ShouldBeFalse();
    second.BalanceAfterCredit.ShouldBe(0.25m);
    (await ledger.GetBalanceAsync(principal.Id)).ShouldBe(0.25m);
    (await store.GetPrincipalAsync(principal.Id))!.TrustTier.ShouldBe(TrustTier.Funded);
  }

  public static async Task Already_funded_principal_is_credited_without_promotion_flag()
  {
    InMemoryPrincipalStore store = new();
    Principal principal = Principal.Create(PrincipalKind.Agent);
    principal.Promote(TrustTier.Funded);
    await store.AddPrincipalAsync(principal);

    SettlementFundingService funding = new(new InMemoryCreditLedger(), store);
    SettlementFundingResult result = await funding.ApplyAsync(principal.Id, 0.50m, "0xalready-funded");

    result.PromotedToFunded.ShouldBeFalse();
    result.BalanceAfterCredit.ShouldBe(0.50m);
    (await store.GetPrincipalAsync(principal.Id))!.TrustTier.ShouldBe(TrustTier.Funded);
  }

  public static async Task Quarantined_principal_is_credited_but_not_promoted()
  {
    (InMemoryPrincipalStore store, Principal principal) = await SeedKeyedAgentAsync();
    Principal live = (await store.GetPrincipalAsync(principal.Id))!;
    live.Quarantine();
    await store.UpdatePrincipalAsync(live);

    SettlementFundingService funding = new(new InMemoryCreditLedger(), store);
    SettlementFundingResult result = await funding.ApplyAsync(live.Id, 0.10m, "0xquarantine-pay");

    result.PromotedToFunded.ShouldBeFalse();
    result.BalanceAfterCredit.ShouldBe(0.10m);
    Principal? after = await store.GetPrincipalAsync(live.Id);
    after.ShouldNotBeNull();
    after.TrustTier.ShouldBe(TrustTier.Keyed);
    after.IsQuarantined.ShouldBeTrue();
    after.IsFundedAndActive.ShouldBeFalse();
  }

  public static async Task Missing_principal_still_credits_ledger()
  {
    InMemoryCreditLedger ledger = new();
    SettlementFundingService funding = new(ledger, new InMemoryPrincipalStore());
    PrincipalId orphan = PrincipalId.New();

    SettlementFundingResult result = await funding.ApplyAsync(orphan, 0.10m, "0xorphan-settle");

    result.PromotedToFunded.ShouldBeFalse();
    result.BalanceAfterCredit.ShouldBe(0.10m);
    (await ledger.GetBalanceAsync(orphan)).ShouldBe(0.10m);
  }

  public static async Task Debit_after_settle_does_not_demote_trust_tier()
  {
    (InMemoryPrincipalStore store, Principal principal) = await SeedKeyedAgentAsync();
    InMemoryCreditLedger ledger = new();
    SettlementFundingService funding = new(ledger, store);

    await funding.ApplyAsync(principal.Id, 0.10m, "0xthen-debit");
    decimal afterDebit = await ledger.DebitAsync(principal.Id, 0.10m, "metered-use");

    afterDebit.ShouldBe(0m);
    Principal? reloaded = await store.GetPrincipalAsync(principal.Id);
    reloaded.ShouldNotBeNull();
    reloaded.TrustTier.ShouldBe(TrustTier.Funded);
    reloaded.IsFundedAndActive.ShouldBeTrue();
  }

  private static async Task<(InMemoryPrincipalStore Store, Principal Principal)> SeedKeyedAgentAsync()
  {
    InMemoryPrincipalStore store = new();
    Principal principal = Principal.Create(PrincipalKind.Agent);
    await store.AddPrincipalAsync(principal);
    // First credential advances Provisional → Keyed (store side effect).
    Credential credential = Credential.Create(
      principal.Id,
      CredentialType.AgentKey,
      handle: Guid.NewGuid().ToByteArray(),
      publicMaterial: Guid.NewGuid().ToByteArray());
    await store.AddCredentialAsync(credential);

    Principal? keyed = await store.GetPrincipalAsync(principal.Id);
    keyed.ShouldNotBeNull();
    keyed.TrustTier.ShouldBe(TrustTier.Keyed);
    return (store, keyed);
  }
}
