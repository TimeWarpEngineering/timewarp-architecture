#region Purpose
// The concurrency rules `dev test` uses to run test projects in parallel: worker count, per-group
// caps, the exclusive AppHost lane, start order, and one in-proc port base per worker.
#endregion

#region Design
// Task 370. `dev test` runs projects concurrently, at most MaxParallel at once. Each worker owns one
// TIMEWARP_TEST_PORT_BASE for as long as it runs a project, so in-proc hosts in two concurrent
// projects never share a port. Closed-box Aspire suites already use dynamic ports (task 319).
//
// Port bases per worker:
//   Derived base (no override) — the checkout's own base, then the next derived slots around the
//   InProcTestPorts ring (NextDerivedBase). That keeps every port in the 20000–32767 range, below
//   the Linux ephemeral range (task 372). The ring has only DerivedSlotCount (8) slots, so worker
//   slots can land on another checkout's base. Two worktrees running `dev test` at the same moment can then collide;
//   HostGraphFactory.EnsurePortIsFree fails loudly with the base and the override variable.
//   Override or fallback base — base, base + DerivedStep, base + 2·DerivedStep … while the whole
//   set stays at or below 65535.
// So the worker count is capped at the slots available (DerivedSlotCount for a derived base).
//
// Default worker count is the CPU count, capped at the slot count. Group caps record which suites
// need less: the integration group (Testcontainers Postgres, Aspire DistributedApplicationTesting,
// in-proc host graphs) defaults to DefaultIntegrationMaxParallel containers-and-hosts at once, so a
// 4-core CI runner and a laptop Docker Desktop are not swamped. The cap is a hint, not a lock:
// `--max-integration` changes it. Fast and playwright have no cap of their own.
//
// AppHost lane (task 372): projects that boot an Aspire AppHost (DistributedApplicationTestingBuilder,
// usually via ClosedBoxAppHost) run one at a time. Concurrent AppHost/DCP startups interfere (the
// `portForServing` substitution failure the AppHost Design region records). Every other project stays
// parallel with them, Testcontainers suites included: in 21 local runs (4 cores, -j 4, Docker
// Desktop) the Testcontainers start failures seen were not tied to an AppHost start (one hit with
// only the two Testcontainers suites running), so PostgresTestAvailability retries the start instead
// of `dev test` serializing Docker. Membership is derived, never listed: BootsAppHost reads the csproj
// and follows its ProjectReferences, looking for an Aspire.Hosting.Testing PackageReference or a
// Compile of the closed-box-app-host.cs helper. Any project that compiles
// DistributedApplicationTestingBuilder needs that package, so the derivation cannot miss one;
// dev-cli-tests guards the monorepo tree.
//
// Start order: playwright first, then integration (AppHost suites first, so the serial lane starts
// at once), then fast. The single playwright project is the longest, so starting it first shortens
// the whole run; fast projects fill free workers.
// Kept free of Amuru/Terminal so tests/tools/dev-cli-tests can Compile-include this file.
#endregion

namespace DevCli.Services;

using System.Text.RegularExpressions;
using TimeWarp.Architecture.Testing;

internal static partial class TestParallelism
{
  /// <summary>Integration-group projects running at once unless <c>--max-integration</c> says otherwise.</summary>
  internal const int DefaultIntegrationMaxParallel = 3;

  private static readonly string[] StartOrder = [TestGroups.Playwright, TestGroups.Integration, TestGroups.Fast];

  /// <summary>Default worker count: the CPU count, at least 1 and at most <paramref name="slotLimit"/>.</summary>
  internal static int DefaultMaxParallel(int processorCount, int slotLimit) =>
    Math.Clamp(processorCount, 1, Math.Max(1, slotLimit));

  /// <summary>How many distinct port bases are available for workers starting from this base.</summary>
  internal static int SlotLimit(int basePort, InProcTestPorts.BaseSource source) =>
    source == InProcTestPorts.BaseSource.Derived
      ? InProcTestPorts.DerivedSlotCount
      : ((65535 - InProcTestPorts.PortSpan - basePort) / InProcTestPorts.DerivedStep) + 1;

  /// <summary>
  /// One port base per worker, <paramref name="count"/> in all, never sharing a port. The first is
  /// <paramref name="basePort"/>. <paramref name="count"/> must not exceed <see cref="SlotLimit"/>.
  /// </summary>
  internal static int[] PortBases(int basePort, InProcTestPorts.BaseSource source, int count)
  {
    if (count < 1 || count > SlotLimit(basePort, source))
    {
      throw new ArgumentOutOfRangeException(
        nameof(count), count, $"Need 1..{SlotLimit(basePort, source)} port bases from base {basePort}.");
    }

    int[] bases = new int[count];
    bases[0] = basePort;
    for (int i = 1; i < count; i++)
    {
      bases[i] = source == InProcTestPorts.BaseSource.Derived
        ? InProcTestPorts.NextDerivedBase(bases[i - 1])
        : bases[i - 1] + InProcTestPorts.DerivedStep;
    }

    return bases;
  }

  /// <summary>The group's own concurrency cap, or null when only the worker count limits it.</summary>
  internal static int? GroupCap(string group, int integrationMaxParallel) =>
    group == TestGroups.Integration ? integrationMaxParallel : null;

  /// <summary>
  /// Projects in start order: playwright, integration, fast; AppHost suites first within a group,
  /// then ordinal path.
  /// </summary>
  internal static (string Project, string Group, bool BootsAppHost)[] Order(
    IEnumerable<(string Project, string Group, bool BootsAppHost)> projects) =>
    [.. projects
      .OrderBy(p => Array.IndexOf(StartOrder, p.Group) is int index and >= 0 ? index : StartOrder.Length)
      .ThenBy(p => p.BootsAppHost ? 0 : 1)
      .ThenBy(p => p.Project, StringComparer.Ordinal)];

  /// <summary>
  /// True when the project at <paramref name="projectPath"/> boots an Aspire AppHost: it, or a project
  /// it references, has an Aspire.Hosting.Testing PackageReference or compiles closed-box-app-host.cs.
  /// <paramref name="readProject"/> returns a csproj's text, or null when the file does not exist.
  /// </summary>
  internal static bool BootsAppHost(string projectPath, Func<string, string?> readProject)
  {
    ArgumentNullException.ThrowIfNull(readProject);
    HashSet<string> visited = new(StringComparer.Ordinal);
    Stack<string> toVisit = new([Path.GetFullPath(projectPath)]);
    while (toVisit.Count > 0)
    {
      string current = toVisit.Pop();
      if (!visited.Add(current) || readProject(current) is not string text) continue;
      if (AppHostTestingPackage().IsMatch(text) || ClosedBoxAppHostCompile().IsMatch(text)) return true;

      string directory = Path.GetDirectoryName(current) ?? string.Empty;
      foreach (Match reference in ProjectReference().Matches(text))
      {
        string include = reference.Groups["path"].Value.Replace('\\', Path.DirectorySeparatorChar);
        toVisit.Push(Path.GetFullPath(Path.Combine(directory, include)));
      }
    }

    return false;
  }

  [GeneratedRegex(@"<PackageReference\s+Include=""Aspire\.Hosting\.Testing""", RegexOptions.CultureInvariant)]
  private static partial Regex AppHostTestingPackage();

  [GeneratedRegex(@"<Compile\s+Include=""[^""]*closed-box-app-host\.cs""", RegexOptions.CultureInvariant)]
  private static partial Regex ClosedBoxAppHostCompile();

  [GeneratedRegex(@"<ProjectReference\s+Include=""(?<path>[^""]+)""", RegexOptions.CultureInvariant)]
  private static partial Regex ProjectReference();
}
