#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0211

// Co-located Jaribu validator tests for UpdateRole (RoleId NotEmpty + RoleDetails + auth).
// Run standalone:  dotnet run source/container-apps/web/features/admin/roles/update-role/update-role-validator-tests.cs

#region Purpose
// Jaribu runfile proving UpdateRole.Validator rejects empty RoleId, Name, Description, and UserId,
// and that Command.From copies a GetRole response including RoleId.
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
  public class UpdateRoleValidator_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<UpdateRoleValidator_Given_>();

    public static Task ValidCommand_Should_PassValidation()
    {
      UpdateRole.Command command = new()
      {
        RoleId = RoleIds.Member,
        UserId = Guid.NewGuid(),
        Name = "Courier",
        Description = "Delivers things faster."
      };

      ValidationResult result = new UpdateRole.Validator().Validate(command);
      result.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task EmptyRoleId_Should_FailValidation()
    {
      UpdateRole.Command command = new()
      {
        RoleId = Guid.Empty,
        UserId = Guid.NewGuid(),
        Name = "Courier",
        Description = "Delivers things faster."
      };

      ValidationResult result = new UpdateRole.Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateRole.Command.RoleId));
      return Task.CompletedTask;
    }

    public static Task EmptyName_Should_FailValidation()
    {
      UpdateRole.Command command = new()
      {
        RoleId = RoleIds.Member,
        UserId = Guid.NewGuid(),
        Name = "",
        Description = "Delivers things faster."
      };

      ValidationResult result = new UpdateRole.Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateRole.Command.Name));
      return Task.CompletedTask;
    }

    public static Task EmptyDescription_Should_FailValidation()
    {
      UpdateRole.Command command = new()
      {
        RoleId = RoleIds.Member,
        UserId = Guid.NewGuid(),
        Name = "Courier",
        Description = ""
      };

      ValidationResult result = new UpdateRole.Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateRole.Command.Description));
      return Task.CompletedTask;
    }

    public static Task EmptyUserId_Should_FailValidation()
    {
      UpdateRole.Command command = new()
      {
        RoleId = RoleIds.Member,
        UserId = Guid.Empty,
        Name = "Courier",
        Description = "Delivers things faster."
      };

      ValidationResult result = new UpdateRole.Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      result.Errors.ShouldContain(error => error.PropertyName == nameof(UpdateRole.Command.UserId));
      return Task.CompletedTask;
    }

    public static Task From_GetRole_Should_Copy_Name_Description_And_RoleId()
    {
      GetRole.Response role = new(
        roleId: RoleIds.Administrator,
        name: "Administrator",
        description: "Built-in administrator.",
        permissions: [PermissionIds.AdminRolesRead],
        agentScopes: ["scope.read"]);
      // Declared apart from the initializer so the type is not apparent (IDE0007).
      UpdateRole.Command command;
      command = UpdateRole.Command.From(role);
      command.RoleId.ShouldBe(RoleIds.Administrator);
      command.Name.ShouldBe("Administrator");
      command.Description.ShouldBe("Built-in administrator.");
      command.Name = "draft";
      role.Name.ShouldBe("Administrator");
      return Task.CompletedTask;
    }
  }

} // namespace TimeWarp.Architecture.Features.Admin.Roles
