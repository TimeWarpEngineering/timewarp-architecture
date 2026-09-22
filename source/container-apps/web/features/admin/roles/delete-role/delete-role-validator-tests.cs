#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058

// Co-located Jaribu validator tests for DeleteRole (RoleId NotEmpty + auth).
// Run standalone:  dotnet run source/container-apps/web/features/admin/roles/delete-role/delete-role-validator-tests.cs

#region Purpose
// Jaribu runfile proving DeleteRole.Validator rejects empty RoleId and UserId.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Admin.Roles
{

  using System.Threading.Tasks;
  using FluentValidation.Results;
  using Shouldly;
  using TimeWarp.Architecture.Features;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Validation")]
  public class DeleteRoleValidator_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<DeleteRoleValidator_Given_>();

    public static Task ValidCommand_Should_PassValidation()
    {
      DeleteRole.Command command = new()
      {
        RoleId = RoleIds.Member,
        UserId = Guid.NewGuid()
      };

      ValidationResult result = new DeleteRole.Validator().Validate(command);
      result.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task EmptyRoleId_Should_FailValidation()
    {
      DeleteRole.Command command = new()
      {
        RoleId = Guid.Empty,
        UserId = Guid.NewGuid()
      };

      ValidationResult result = new DeleteRole.Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      result.Errors.ShouldContain(error => error.PropertyName == nameof(DeleteRole.Command.RoleId));
      return Task.CompletedTask;
    }

    public static Task EmptyUserId_Should_FailValidation()
    {
      DeleteRole.Command command = new()
      {
        RoleId = RoleIds.Member,
        UserId = Guid.Empty
      };

      ValidationResult result = new DeleteRole.Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      result.Errors.ShouldContain(error => error.PropertyName == nameof(DeleteRole.Command.UserId));
      return Task.CompletedTask;
    }
  }

} // namespace TimeWarp.Architecture.Features.Admin.Roles
