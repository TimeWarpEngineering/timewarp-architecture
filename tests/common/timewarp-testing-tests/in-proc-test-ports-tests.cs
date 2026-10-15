#!/usr/bin/env -S dotnet --
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0211

// Run: dotnet run tests/common/timewarp-testing-tests/in-proc-test-ports-tests.cs

#region Purpose
// Prove InProcTestPorts' per-checkout base (task 330): stable per path, different across paths,
// in range and aligned, override wins, checkout-root discovery, and the collision message.
#endregion

#region Design
// Derivation tests use fixture paths only — no sockets. The collision test binds one OS-assigned
// loopback port (port 0) so it never contends with a real in-proc host base.
// Lives in timewarp-testing-tests (dual-mode, JARIBU_MULTI) beside the HostGraphFactory smoke.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Testing.InProcTestPortsTests
{

  using System;
  using System.IO;
  using System.Linq;
  using System.Net;
  using System.Net.Sockets;
  using System.Threading.Tasks;
  using Shouldly;
  using static TimeWarp.Jaribu.TestRunner;

  public class InProcTestPorts_DeriveBase_Given_
  {
    private static readonly string FixtureA = Path.Combine(Path.GetTempPath(), "tw-330-fixture", "worktree-a");
    private static readonly string FixtureB = Path.Combine(Path.GetTempPath(), "tw-330-fixture", "worktree-b");

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<InProcTestPorts_DeriveBase_Given_>();

    public static Task Should_Be_Stable_For_One_Path()
    {
      InProcTestPorts.DeriveBase(FixtureA).ShouldBe(InProcTestPorts.DeriveBase(FixtureA));
      InProcTestPorts.DeriveBase(FixtureA + Path.DirectorySeparatorChar).ShouldBe(InProcTestPorts.DeriveBase(FixtureA));
      return Task.CompletedTask;
    }

    public static Task Should_Differ_Across_Two_Paths()
    {
      InProcTestPorts.DeriveBase(FixtureA).ShouldNotBe(InProcTestPorts.DeriveBase(FixtureB));
      return Task.CompletedTask;
    }

    public static Task Should_Land_In_Range_And_Aligned()
    {
      int[] bases = Enumerable.Range(0, 500)
        .Select(i => InProcTestPorts.DeriveBase(Path.Combine(Path.GetTempPath(), "tw-330-fixture", $"checkout-{i}")))
        .ToArray();

      foreach (int basePort in bases)
      {
        basePort.ShouldBeGreaterThanOrEqualTo(InProcTestPorts.DerivedRangeStart);
        (basePort + InProcTestPorts.PortSpan).ShouldBeLessThan(InProcTestPorts.DerivedRangeEnd);
        ((basePort - InProcTestPorts.DerivedRangeStart) % InProcTestPorts.DerivedStep).ShouldBe(0);
      }

      InProcTestPorts.DerivedStep.ShouldBeGreaterThan(InProcTestPorts.PortSpan);
      // Task 372: below the Linux default ephemeral range (32768–60999), so the OS never hands a
      // derived port to another socket.
      InProcTestPorts.DerivedRangeEnd.ShouldBeLessThanOrEqualTo(32768);
      bases.Distinct().Count().ShouldBe(InProcTestPorts.DerivedSlotCount);
      return Task.CompletedTask;
    }

    public static Task Should_Wrap_NextDerivedBase_Within_Slots()
    {
      const int last = InProcTestPorts.DerivedRangeStart + ((InProcTestPorts.DerivedSlotCount - 1) * InProcTestPorts.DerivedStep);
      InProcTestPorts.NextDerivedBase(InProcTestPorts.DerivedRangeStart)
        .ShouldBe(InProcTestPorts.DerivedRangeStart + InProcTestPorts.DerivedStep);
      InProcTestPorts.NextDerivedBase(last).ShouldBe(InProcTestPorts.DerivedRangeStart);
      return Task.CompletedTask;
    }
  }

  public class InProcTestPorts_ResolveBase_Given_
  {
    private static readonly string Fixture = Path.Combine(Path.GetTempPath(), "tw-330-fixture", "worktree-a");

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<InProcTestPorts_ResolveBase_Given_>();

    public static Task Should_Use_Override_When_Set()
    {
      InProcTestPorts.ResolveBase("47000", Fixture)
        .ShouldBe((47000, InProcTestPorts.BaseSource.Override));
      return Task.CompletedTask;
    }

    public static Task Should_Derive_When_Override_Unset()
    {
      InProcTestPorts.ResolveBase(null, Fixture)
        .ShouldBe((InProcTestPorts.DeriveBase(Fixture), InProcTestPorts.BaseSource.Derived));
      InProcTestPorts.ResolveBase("  ", Fixture).Source.ShouldBe(InProcTestPorts.BaseSource.Derived);
      return Task.CompletedTask;
    }

    public static Task Should_Fall_Back_Without_Checkout_Root()
    {
      InProcTestPorts.ResolveBase(null, null)
        .ShouldBe((InProcTestPorts.LegacyBase, InProcTestPorts.BaseSource.Fallback));
      return Task.CompletedTask;
    }

    public static Task Should_Reject_Override_Out_Of_Range()
    {
      Should.Throw<InvalidOperationException>(() => InProcTestPorts.ResolveBase("65000", Fixture))
        .Message.ShouldContain(InProcTestPorts.EnvironmentVariableName);
      Should.Throw<InvalidOperationException>(() => InProcTestPorts.ResolveBase("abc", Fixture));
      return Task.CompletedTask;
    }

    public static Task Should_Find_Nearest_Slnx_Ancestor()
    {
      string root = Path.Combine(Path.GetTempPath(), $"tw-330-root-{Guid.NewGuid():N}");
      string nested = Path.Combine(root, "tests", "suite");
      Directory.CreateDirectory(nested);
      try
      {
        InProcTestPorts.FindCheckoutRoot([nested]).ShouldBeNull();
        File.WriteAllText(Path.Combine(root, "app.slnx"), "<Solution />");
        InProcTestPorts.FindCheckoutRoot([null, nested]).ShouldBe(root);
      }
      finally
      {
        Directory.Delete(root, recursive: true);
      }

      return Task.CompletedTask;
    }
  }

  public class HostGraphFactory_EnsurePortIsFree_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<HostGraphFactory_EnsurePortIsFree_Given_>();

    public static Task Should_Name_Port_Base_Checkout_And_Override_When_Taken()
    {
      using TcpListener holder = new(IPAddress.Loopback, 0);
      holder.Start();
      int port = ((IPEndPoint)holder.LocalEndpoint).Port;

      InvalidOperationException exception = Should.Throw<InvalidOperationException>(
        () => HostGraphFactory.EnsurePortIsFree(port, "Web.Server (fixture)"));

      exception.Message.ShouldContain($"port {port}");
      exception.Message.ShouldContain("Web.Server (fixture)");
      exception.Message.ShouldContain(InProcTestPorts.Describe());
      exception.Message.ShouldContain(InProcTestPorts.EnvironmentVariableName);
      return Task.CompletedTask;
    }

    public static Task Should_Describe_Derived_Base_With_Checkout_Path()
    {
      string checkout = Path.Combine(Path.GetTempPath(), "tw-330-fixture", "worktree-a");
      string message = HostGraphFactory.PortInUseMessage(
        23000,
        "Web.Server",
        InProcTestPorts.Describe(23000, InProcTestPorts.BaseSource.Derived, checkout));

      message.ShouldContain("port 23000");
      message.ShouldContain("base 23000 derived from checkout " + checkout);
      message.ShouldContain(InProcTestPorts.EnvironmentVariableName);
      return Task.CompletedTask;
    }
  }

} // namespace TimeWarp.Architecture.Testing.InProcTestPortsTests
