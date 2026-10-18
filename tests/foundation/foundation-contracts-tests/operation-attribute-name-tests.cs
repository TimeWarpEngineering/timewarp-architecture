#region Purpose
// The operation attribute's metadata name is the string both generators match.
#endregion

namespace OperationAttribute_;

using TimeWarp.Contracts;

public class FullName_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FullName_Given_>();

  public static Task Type_Should_Be_TimeWarp_Contracts_OperationAttribute()
  {
    typeof(OperationAttribute).FullName.ShouldBe("TimeWarp.Contracts.OperationAttribute");
    typeof(OperationAttribute).Assembly.GetName().Name.ShouldBe("timewarp-contracts");
    return Task.CompletedTask;
  }
}
