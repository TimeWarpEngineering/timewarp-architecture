#region Purpose
// Debugging-story evidence (not a pass/fail contract): overdraw the SAME aggregate through each host
// and print what the CALLER observes when the domain invariant rejects the command — exception type
// and the stack frames that cross the actor/grain boundary. Answers the plan's "what do stack traces
// look like / does the domain error survive the boundary" question with real output rather than
// assertions.
#endregion

namespace TimeWarp.Spike.DualActor.Tests;

using TimeWarp.Spike.DualActor.Akka;
using TimeWarp.Spike.DualActor.Orleans;

public class ErrorPropagationDiagnostics
{
  public async Task Akka_overdraw_what_the_caller_sees()
  {
    await using AkkaLedgerHost host = await AkkaLedgerHost.StartAsync();
    await SpikePostgres.EnsureFreshSchemaAsync(host.Services);
    PrincipalId id = PrincipalId.New();
    await LedgerScenario.SeedAsync(host.Services, id); // balance 1000

    try
    {
      await host.DebitAsync(id, LedgerScenario.SeedBalance + 1, TimeSpan.FromSeconds(3)); // overdraw
      Console.WriteLine("[akka-error] NO exception surfaced to caller");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[akka-error] caller caught: {ex.GetType().FullName}: {ex.Message}");
      Console.WriteLine($"[akka-error] mentions Ledger.Debit in trace: {ex.ToString().Contains("Ledger.Debit", StringComparison.Ordinal)}");
    }
  }

  public async Task Orleans_overdraw_what_the_caller_sees()
  {
    await using OrleansLedgerHost host = await OrleansLedgerHost.StartAsync();
    await SpikePostgres.EnsureFreshSchemaAsync(host.Services);
    PrincipalId id = PrincipalId.New();
    await LedgerScenario.SeedAsync(host.Services, id); // balance 1000

    try
    {
      await host.DebitAsync(id, LedgerScenario.SeedBalance + 1); // overdraw
      Console.WriteLine("[orleans-error] NO exception surfaced to caller");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[orleans-error] caller caught: {ex.GetType().FullName}: {ex.Message}");
      Console.WriteLine($"[orleans-error] mentions Ledger.Debit in trace: {ex.ToString().Contains("Ledger.Debit", StringComparison.Ordinal)}");
    }
  }
}
