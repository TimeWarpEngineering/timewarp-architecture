#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Co-located Jaribu: SetRolePermissions contracts + protected-core guard (182-004).

#region Purpose
// Host-free round-trip/validation for SetRolePermissions plus ProtectedCoreConflict unit coverage.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Admin.Roles
{

  using System.Collections.Generic;
  using System.Text.Json;
  using System.Threading.Tasks;
  using FluentValidation.Results;
  using Shouldly;
  using TimeWarp.Architecture.Features;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Contracts")]
  public class SetRolePermissionsCommand_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<SetRolePermissionsCommand_Given_>();

    public static Task ValidCommand_Should_RoundTripThroughJson()
    {
      SetRolePermissions.Command command = new()
      {
        UserId = Guid.NewGuid(),
        PermissionIds = [PermissionIds.AdminAccess, PermissionIds.ProfileRead]
      };

      string json = JsonSerializer.Serialize(command, ContractSerializationDefaults.Options);
      SetRolePermissions.Command? parsed =
        JsonSerializer.Deserialize<SetRolePermissions.Command>(json, ContractSerializationDefaults.Options);

      parsed.ShouldNotBeNull();
      parsed.UserId.ShouldBe(command.UserId);
      parsed.PermissionIds.ShouldBe(command.PermissionIds);
      return Task.CompletedTask;
    }

    public static Task EmptyPermissionIds_Should_PassValidation()
    {
      SetRolePermissions.Command command = new()
      {
        UserId = Guid.NewGuid(),
        RoleId = RoleIds.Member,
        PermissionIds = []
      };

      ValidationResult result = new SetRolePermissions.Validator().Validate(command);
      result.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task UnknownPermissionId_Should_FailValidation()
    {
      SetRolePermissions.Command command = new()
      {
        UserId = Guid.NewGuid(),
        RoleId = RoleIds.Member,
        PermissionIds = ["not.a.permission"]
      };

      ValidationResult result = new SetRolePermissions.Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task EmptyRoleId_Should_FailValidation()
    {
      SetRolePermissions.Command command = new()
      {
        UserId = Guid.NewGuid(),
        RoleId = Guid.Empty,
        PermissionIds = [PermissionIds.ProfileRead]
      };

      ValidationResult result = new SetRolePermissions.Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }
  }

  [TestTag("Contracts")]
  public class SetRolePermissionsResponse_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<SetRolePermissionsResponse_Given_>();

    public static Task ValidResponse_Should_RoundTripThroughJson()
    {
      SetRolePermissions.Response response = new([PermissionIds.AdminAccess, PermissionIds.AdminRolesManage]);

      string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      SetRolePermissions.Response? parsed =
        JsonSerializer.Deserialize<SetRolePermissions.Response>(json, ContractSerializationDefaults.Options);

      parsed.ShouldNotBeNull();
      parsed.PermissionIds.ShouldBe([PermissionIds.AdminAccess, PermissionIds.AdminRolesManage]);
      return Task.CompletedTask;
    }
  }

  [TestTag("Application")]
  public class AdminLockoutGuards_ProtectedCore_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<AdminLockoutGuards_ProtectedCore_Given_>();

    public static Task Administrator_Stripping_RolesManage_Should_Conflict()
    {
      List<string> withoutManage =
      [
        PermissionIds.AdminAccess,
        PermissionIds.AdminRolesRead,
        // AdminRolesManage intentionally omitted
        PermissionIds.AdminPrincipalsRead,
        PermissionIds.AdminPrincipalsManage,
        PermissionIds.ProfileRead,
        PermissionIds.SettingsRead,
      ];

      SharedProblemDetails? problem = AdminLockoutGuards.ProtectedCoreConflict(
        RoleIds.Administrator,
        withoutManage);

      problem.ShouldNotBeNull();
      problem.Status.ShouldBe(409);
      problem.Title.ShouldBe("Protected core permissions");
      problem.Detail.ShouldNotBeNull();
      problem.Detail.ShouldContain(PermissionIds.AdminRolesManage);
      return Task.CompletedTask;
    }

    public static Task Administrator_With_Full_AdminPermissions_Should_Allow()
    {
      List<string> full = [.. RolePermissionSeed.DefaultGrants[RoleIds.Administrator]];

      SharedProblemDetails? problem = AdminLockoutGuards.ProtectedCoreConflict(
        RoleIds.Administrator,
        full);

      problem.ShouldBeNull();
      return Task.CompletedTask;
    }

    public static Task Member_Empty_Permissions_Should_Allow()
    {
      SharedProblemDetails? problem = AdminLockoutGuards.ProtectedCoreConflict(
        RoleIds.Member,
        []);

      problem.ShouldBeNull();
      return Task.CompletedTask;
    }
  }

  [TestTag("Application")]
  public class AdminLockoutGuards_LastAdmin_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<AdminLockoutGuards_LastAdmin_Given_>();

    public static async Task Administrator_Role_Should_Grant_PrincipalsManage()
    {
      InMemoryRolePermissionStore store = new();
      bool granted = await AdminLockoutGuards.RolesGrantPermissionAsync(
        [RoleIds.Administrator],
        PermissionIds.AdminPrincipalsManage,
        store);

      granted.ShouldBeTrue();
    }

    public static async Task Member_Only_Should_Not_Grant_PrincipalsManage()
    {
      InMemoryRolePermissionStore store = new();
      bool granted = await AdminLockoutGuards.RolesGrantPermissionAsync(
        [RoleIds.Member],
        PermissionIds.AdminPrincipalsManage,
        store);

      granted.ShouldBeFalse();
    }

    public static Task SimulateEffective_Empty_Should_Be_Member()
    {
      PrincipalId id = PrincipalId.New();
      IReadOnlyList<Guid> effective = AdminLockoutGuards.SimulateEffectiveRoles(
        id,
        storedRoleIds: [],
        bootstrapPrincipalIds: new HashSet<PrincipalId>());

      effective.ShouldBe([RoleIds.Member]);
      return Task.CompletedTask;
    }

    public static Task LastAdministratorConflict_Should_Be_409()
    {
      SharedProblemDetails problem = AdminLockoutGuards.LastAdministratorConflict();
      problem.Status.ShouldBe(409);
      problem.Title.ShouldBe("Last administrator");
      return Task.CompletedTask;
    }
  }
}
