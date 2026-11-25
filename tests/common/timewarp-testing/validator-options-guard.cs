namespace TimeWarp.Architecture.Testing;

using System.Linq.Expressions;
using FluentValidation;

#region Purpose
// Keep FluentValidation's process-wide ValidatorOptions.Global resolvers from leaking out of an
// in-proc server host into the pure validator tests that run after it in the same process.
#endregion

#region Design
// Task 526: FastEndpoints (UseFastEndpoints: camelCase PropertyNameResolver) and CommonServerModule
// (Type:Member DisplayNameResolver) overwrite static ValidatorOptions.Global when a host boots. A
// Jaribu family aggregator runs many test classes in one process, so a validator test after a host
// test saw PropertyName "pagePath" where its standalone run saw "PagePath" (tasks 520 and 521).
// Ref-counted under one lock in a NON-generic class: Web and Api hosts are different closed
// WebApplicationHost<TProgram> types, and a HostGraph keeps several alive at once. The first Enter
// (count 0 -> 1) snapshots both resolvers before any host configures; the last Exit (1 -> 0)
// restores them, so hosts alive together share one snapshot and the pre-host values come back.
// WebApplicationHost calls this; a test that boots a raw WebApplication + UseFastEndpoints calls
// Enter before building it and Exit after disposing it.
#endregion

/// <summary>
/// Ref-counted snapshot and restore of <see cref="ValidatorOptions.Global"/>'s
/// <c>PropertyNameResolver</c> and <c>DisplayNameResolver</c> around in-proc server hosts.
/// </summary>
public static class ValidatorOptionsGuard
{
  private static readonly Lock Gate = new();
  private static int ActiveCount;
  private static Func<Type, MemberInfo, LambdaExpression, string>? SavedPropertyNameResolver;
  private static Func<Type, MemberInfo, LambdaExpression, string>? SavedDisplayNameResolver;

  /// <summary>Number of hosts currently inside the guard.</summary>
  public static int Active
  {
    get
    {
      lock (Gate) return ActiveCount;
    }
  }

  /// <summary>Call before a host configures. The first caller snapshots both resolvers.</summary>
  public static void Enter()
  {
    lock (Gate)
    {
      if (ActiveCount == 0)
      {
        SavedPropertyNameResolver = ValidatorOptions.Global.PropertyNameResolver;
        SavedDisplayNameResolver = ValidatorOptions.Global.DisplayNameResolver;
      }

      ActiveCount++;
    }
  }

  /// <summary>Call once per <see cref="Enter"/> after the host is gone. The last caller restores both resolvers.</summary>
  public static void Exit()
  {
    lock (Gate)
    {
      if (ActiveCount == 0)
        throw new InvalidOperationException($"{nameof(ValidatorOptionsGuard)}.{nameof(Exit)} called without a matching {nameof(Enter)}.");

      ActiveCount--;
      if (ActiveCount > 0) return;

      ValidatorOptions.Global.PropertyNameResolver = SavedPropertyNameResolver!;
      ValidatorOptions.Global.DisplayNameResolver = SavedDisplayNameResolver!;
      SavedPropertyNameResolver = null;
      SavedDisplayNameResolver = null;
    }
  }
}
