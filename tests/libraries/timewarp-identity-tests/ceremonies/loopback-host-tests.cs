#region Purpose
// Tests for LoopbackHost.IsLoopback, the shared "http allowed only on loopback" predicate.
#endregion

// ReSharper disable InconsistentNaming
namespace LoopbackHost_;

public class IsLoopback
{

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<IsLoopback>();

  public static Task Accepts_canonical_loopback_hosts()
  {
    LoopbackHost.IsLoopback("localhost").ShouldBeTrue();
    LoopbackHost.IsLoopback("LOCALHOST").ShouldBeTrue();
    LoopbackHost.IsLoopback("127.0.0.1").ShouldBeTrue();
    LoopbackHost.IsLoopback(new Uri("http://[::1]:5000").Host).ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Rejects_other_hosts()
  {
    LoopbackHost.IsLoopback("arch.timewarp.work").ShouldBeFalse();
    LoopbackHost.IsLoopback("localhost.evil.example").ShouldBeFalse();
    LoopbackHost.IsLoopback("").ShouldBeFalse();
    LoopbackHost.IsLoopback(null).ShouldBeFalse();
    return Task.CompletedTask;
  }

}
