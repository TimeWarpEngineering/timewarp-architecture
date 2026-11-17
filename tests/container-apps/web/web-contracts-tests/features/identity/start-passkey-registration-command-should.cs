#region Purpose
// Round-trip tests for the identity feature contract StartPasskeyRegistration_Command (tasks 104-003,
// 104-004, 104-005): the wire shape where serialization can actually diverge.
#endregion

#region Design
// Task 253 pin: neither passkey registration Command carries a PrincipalId/Guid property — the
// pre-allocated account id lives only with the challenge, so there is no wire field a client could
// use to pick the principal Complete mints. The other Start* Commands/Responses are plain
// auto-property POCOs and get no test, per the skill's "trivial auto-property POCOs are
// deliberately not written" guidance.
#endregion

// ReSharper disable InconsistentNaming
namespace IdentityContracts_;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Web.Contracts.Tests;
using TimeWarp.Identity;

public class StartPasskeyRegistration_Command_Should
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<StartPasskeyRegistration_Command_Should>();

  public static Task Carry_No_Principal_Id_On_The_Wire()
  {
    Type[] commandTypes = [typeof(StartPasskeyRegistration.Command), typeof(CompletePasskeyRegistration.Command)];
    foreach (Type commandType in commandTypes)
    {
      commandType.GetProperties()
        .Where(property => property.PropertyType == typeof(PrincipalId)
          || property.PropertyType == typeof(PrincipalId?)
          || property.PropertyType == typeof(Guid)
          || property.PropertyType == typeof(Guid?))
        .ShouldBeEmpty($"{commandType.FullName} must not let a client supply the principal id.");
    }

    return Task.CompletedTask;
  }

  public static Task SerializeAndDeserialize_ForCurrentAccount()
  {
    StartPasskeyRegistration.Command command = new() { ForCurrentAccount = true };

    StartPasskeyRegistration.Command parsed = ContractSerialization.RoundTrip(command);

    parsed.ForCurrentAccount.ShouldBeTrue();
    return Task.CompletedTask;
  }
}
