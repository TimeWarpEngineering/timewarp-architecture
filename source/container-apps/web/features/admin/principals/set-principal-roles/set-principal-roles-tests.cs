#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058

// Co-located Jaribu contract tests for SetPrincipalRoles serialization + validation.

#region Purpose
// Jaribu runfile: SetPrincipalRoles Command/Response round-trip and product-role validation.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Admin.Principals
{

  using System.Text.Json;
  using System.Threading.Tasks;
  using FluentValidation.Results;
  using Shouldly;
  using TimeWarp.Architecture.Features;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Contracts")]
  public class SetPrincipalRolesCommand_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<SetPrincipalRolesCommand_Given_>();

    public static Task ValidCommand_Should_RoundTripThroughJson()
    {
      SetPrincipalRoles.Command command = new()
      {
        UserId = Guid.NewGuid(),
        RoleIds = [RoleIds.Member, RoleIds.Developer]
      };

      string json = JsonSerializer.Serialize(command, ContractSerializationDefaults.Options);
      SetPrincipalRoles.Command? parsed =
        JsonSerializer.Deserialize<SetPrincipalRoles.Command>(json, ContractSerializationDefaults.Options);

      parsed.ShouldNotBeNull();
      parsed.UserId.ShouldBe(command.UserId);
      parsed.RoleIds.ShouldBe(command.RoleIds);
      return Task.CompletedTask;
    }

    public static Task EmptyRoleIds_Should_PassValidation()
    {
      SetPrincipalRoles.Command command = new()
      {
        UserId = Guid.NewGuid(),
        PrincipalId = Guid.NewGuid(),
        RoleIds = []
      };

      ValidationResult result = new SetPrincipalRoles.Validator().Validate(command);
      result.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task UnknownRoleId_Should_FailValidation()
    {
      SetPrincipalRoles.Command command = new()
      {
        UserId = Guid.NewGuid(),
        PrincipalId = Guid.NewGuid(),
        RoleIds = [Guid.Parse("99999999-9999-9999-9999-999999999999")]
      };

      ValidationResult result = new SetPrincipalRoles.Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }
  }

  [TestTag("Contracts")]
  public class SetPrincipalRolesResponse_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<SetPrincipalRolesResponse_Given_>();

    public static Task ValidResponse_Should_RoundTripThroughJson()
    {
      SetPrincipalRoles.Response response = new([RoleIds.Administrator]);

      string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      SetPrincipalRoles.Response? parsed =
        JsonSerializer.Deserialize<SetPrincipalRoles.Response>(json, ContractSerializationDefaults.Options);

      parsed.ShouldNotBeNull();
      parsed.RoleIds.ShouldBe([RoleIds.Administrator]);
      return Task.CompletedTask;
    }
  }
}
