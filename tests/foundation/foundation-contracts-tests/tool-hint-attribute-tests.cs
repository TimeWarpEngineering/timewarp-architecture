#region Purpose
// Destructive and open-world hints are tri-state attributes. Absence is unset, not false.
#endregion

namespace ToolHintAttribute_;

using System.Reflection;
using TimeWarp.Contracts;

public class Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Given_>();

  [Destructive]
  [OpenWorld(false)]
  private sealed class Marked;

  [Destructive(false)]
  [OpenWorld]
  private sealed class Explicit;

  private sealed class Unmarked;

  public static Task Destructive_Without_An_Argument_Should_Be_True()
  {
    DestructiveAttribute hint = new();

    hint.IsDestructive.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task Destructive_False_Should_Stay_False()
  {
    DestructiveAttribute hint = new(false);

    hint.IsDestructive.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task OpenWorld_Without_An_Argument_Should_Be_True()
  {
    OpenWorldAttribute hint = new();

    hint.IsOpenWorld.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task OpenWorld_False_Should_Stay_False()
  {
    OpenWorldAttribute hint = new(false);

    hint.IsOpenWorld.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Metadata_Names_Should_Be_TimeWarp_Contracts()
  {
    typeof(DestructiveAttribute).FullName.ShouldBe("TimeWarp.Contracts.DestructiveAttribute");
    typeof(OpenWorldAttribute).FullName.ShouldBe("TimeWarp.Contracts.OpenWorldAttribute");

    AttributeUsageAttribute destructiveUsage = typeof(DestructiveAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
    AttributeUsageAttribute openWorldUsage = typeof(OpenWorldAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
    destructiveUsage.ValidOn.ShouldBe(AttributeTargets.Class);
    destructiveUsage.AllowMultiple.ShouldBeFalse();
    destructiveUsage.Inherited.ShouldBeFalse();
    openWorldUsage.ValidOn.ShouldBe(AttributeTargets.Class);
    openWorldUsage.AllowMultiple.ShouldBeFalse();
    openWorldUsage.Inherited.ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task A_Marked_Type_Should_Expose_The_Hints_To_Reflection()
  {
    typeof(Marked).GetCustomAttribute<DestructiveAttribute>()!.IsDestructive.ShouldBeTrue();
    typeof(Marked).GetCustomAttribute<OpenWorldAttribute>()!.IsOpenWorld.ShouldBeFalse();
    typeof(Explicit).GetCustomAttribute<DestructiveAttribute>()!.IsDestructive.ShouldBeFalse();
    typeof(Explicit).GetCustomAttribute<OpenWorldAttribute>()!.IsOpenWorld.ShouldBeTrue();
    return Task.CompletedTask;
  }

  public static Task An_Unmarked_Type_Should_Have_Neither_Hint()
  {
    typeof(Unmarked).GetCustomAttribute<DestructiveAttribute>().ShouldBeNull();
    typeof(Unmarked).GetCustomAttribute<OpenWorldAttribute>().ShouldBeNull();
    return Task.CompletedTask;
  }
}
