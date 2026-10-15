#region Purpose
// Pins `dev test` parallelism: worker counts, distinct per-worker port bases, group caps, start order.
#endregion

namespace TestParallelism_;

using TimeWarp.Architecture.Testing;

public class PortBases_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PortBases_Given_>();

  public static Task DerivedBase_Should_WalkTheRingWithoutSharingAPort()
  {
    const int start = InProcTestPorts.DerivedRangeStart + (5 * InProcTestPorts.DerivedStep);

    int[] bases = TestParallelism.PortBases(start, InProcTestPorts.BaseSource.Derived, InProcTestPorts.DerivedSlotCount);

    bases[0].ShouldBe(start);
    bases.Distinct().Count().ShouldBe(InProcTestPorts.DerivedSlotCount);
    foreach (int basePort in bases)
    {
      basePort.ShouldBeGreaterThanOrEqualTo(InProcTestPorts.DerivedRangeStart);
      (basePort + InProcTestPorts.PortSpan).ShouldBeLessThan(InProcTestPorts.DerivedRangeEnd);
    }

    return Task.CompletedTask;
  }

  public static Task OverrideBase_Should_StepUpByDerivedStep()
  {
    TestParallelism.PortBases(7000, InProcTestPorts.BaseSource.Override, 3)
      .ShouldBe([7000, 7000 + InProcTestPorts.DerivedStep, 7000 + (2 * InProcTestPorts.DerivedStep)]);
    return Task.CompletedTask;
  }

  public static Task OverrideNearTop_Should_LimitSlotsToValidPorts()
  {
    const int basePort = 65535 - InProcTestPorts.PortSpan - InProcTestPorts.DerivedStep;

    TestParallelism.SlotLimit(basePort, InProcTestPorts.BaseSource.Override).ShouldBe(2);
    int[] bases = TestParallelism.PortBases(basePort, InProcTestPorts.BaseSource.Override, 2);
    (bases[^1] + InProcTestPorts.PortSpan).ShouldBeLessThanOrEqualTo(65535);
    Should.Throw<ArgumentOutOfRangeException>(
      () => TestParallelism.PortBases(basePort, InProcTestPorts.BaseSource.Override, 3));
    return Task.CompletedTask;
  }
}

public class Limits_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Limits_Given_>();

  public static Task DefaultMaxParallel_Should_BeCpuCountCappedBySlots()
  {
    TestParallelism.DefaultMaxParallel(processorCount: 4, slotLimit: 13).ShouldBe(4);
    TestParallelism.DefaultMaxParallel(processorCount: 32, slotLimit: 13).ShouldBe(13);
    TestParallelism.DefaultMaxParallel(processorCount: 0, slotLimit: 13).ShouldBe(1);
    return Task.CompletedTask;
  }

  public static Task OnlyIntegration_Should_HaveAGroupCap()
  {
    TestParallelism.GroupCap(TestGroups.Integration, 3).ShouldBe(3);
    TestParallelism.GroupCap(TestGroups.Fast, 3).ShouldBeNull();
    TestParallelism.GroupCap(TestGroups.Playwright, 3).ShouldBeNull();
    return Task.CompletedTask;
  }

  public static Task Order_Should_StartPlaywrightThenIntegrationThenFast()
  {
    (string, string)[] ordered = TestParallelism.Order(
      [
        ("b-fast", TestGroups.Fast),
        ("a-fast", TestGroups.Fast),
        ("z-integration", TestGroups.Integration),
        ("web-spa-playwright-tests", TestGroups.Playwright),
      ]);

    ordered.Select(p => p.Item1).ShouldBe(["web-spa-playwright-tests", "z-integration", "a-fast", "b-fast"]);
    return Task.CompletedTask;
  }
}
