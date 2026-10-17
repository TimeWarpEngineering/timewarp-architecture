#region Purpose
// SharedProblemDetails resolves through the foundation type forward into TimeWarp.Contracts.
#endregion

namespace SharedProblemDetails_;

public class Assembly_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Assembly_Given_>();

  public static Task Type_Should_Resolve_From_The_TimeWarp_Contracts_Assembly()
  {
    typeof(SharedProblemDetails).Assembly.GetName().Name.ShouldBe("timewarp-contracts");
    return Task.CompletedTask;
  }
}
