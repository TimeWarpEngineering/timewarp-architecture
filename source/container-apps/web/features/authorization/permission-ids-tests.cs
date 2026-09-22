#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Co-located Jaribu: PermissionIds prefix grouping + protected-core UI lock (task 206).

#region Purpose
// Host-free coverage that GroupsByPrefix is derived from All and that Administrator admin.*
// grants lock when selected (honest SPA disable matching server protected-core).
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features
{

  using System.Collections.Generic;
  using System.Linq;
  using System.Threading.Tasks;
  using Shouldly;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Contracts")]
  public class PermissionIds_GroupsByPrefix_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<PermissionIds_GroupsByPrefix_Given_>();

    public static Task Catalog_Should_Partition_Every_Id_Exactly_Once()
    {
      List<string> grouped = [.. PermissionIds.GroupsByPrefix.SelectMany(group => group.PermissionIds)];
      grouped.ShouldBe(PermissionIds.All);
      return Task.CompletedTask;
    }

    public static Task Prefix_Should_Be_First_Dotted_Segment()
    {
      PermissionIds.Prefix(PermissionIds.AdminRolesManage).ShouldBe("admin");
      PermissionIds.Prefix(PermissionIds.CredentialManageSelf).ShouldBe("credential");
      PermissionIds.Prefix(PermissionIds.DemoInvoke).ShouldBe("demo");
      return Task.CompletedTask;
    }

    public static Task Admin_Group_Should_Contain_Every_Admin_Star_Id()
    {
      PermissionIds.PermissionGroup adminGroup = PermissionIds.GroupsByPrefix
        .Single(group => group.Prefix == "admin");

      adminGroup.PermissionIds.ShouldBe(
      [
        PermissionIds.AdminAccess,
        PermissionIds.AdminRolesRead,
        PermissionIds.AdminRolesManage,
        PermissionIds.AdminPrincipalsRead,
        PermissionIds.AdminPrincipalsManage,
      ]);
      return Task.CompletedTask;
    }

    public static Task PrefixesOf_Should_Return_Distinct_Ordered_Prefixes()
    {
      IReadOnlyList<string> prefixes = PermissionIds.PrefixesOf(
      [
        PermissionIds.ProfileRead,
        PermissionIds.AdminAccess,
        PermissionIds.AdminRolesRead,
        PermissionIds.DeveloperAccess,
      ]);

      prefixes.ShouldBe(["admin", "developer", "profile"]);
      PermissionIds.PrefixesOf([]).ShouldBeEmpty();
      return Task.CompletedTask;
    }

    public static Task CheckStateFor_Should_Be_False_True_Or_Mixed()
    {
      PermissionIds.PermissionGroup adminGroup = PermissionIds.GroupsByPrefix
        .Single(group => group.Prefix == "admin");

      adminGroup.CheckStateFor([]).ShouldBe(false);
      adminGroup.CheckStateFor(adminGroup.PermissionIds.ToHashSet(StringComparer.Ordinal)).ShouldBe(true);
      adminGroup.CheckStateFor(new HashSet<string>(StringComparer.Ordinal) { PermissionIds.AdminAccess })
        .ShouldBe((bool?)null);
      return Task.CompletedTask;
    }
  }

  [TestTag("Contracts")]
  public class PermissionIds_ProtectedCore_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<PermissionIds_ProtectedCore_Given_>();

    public static Task Administrator_Admin_Prefix_Should_Be_Protected_Core()
    {
      PermissionIds.IsProtectedCore(RoleIds.Administrator, PermissionIds.AdminRolesManage)
        .ShouldBeTrue();
      PermissionIds.IsProtectedCore(RoleIds.Administrator, PermissionIds.ProfileRead)
        .ShouldBeFalse();
      PermissionIds.IsProtectedCore(RoleIds.Member, PermissionIds.AdminAccess)
        .ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task Selected_Core_Should_Lock_Uncheck_Missing_Core_Should_Stay_Editable()
    {
      PermissionIds.IsProtectedCoreLocked(
        RoleIds.Administrator,
        PermissionIds.AdminRolesManage,
        isSelected: true).ShouldBeTrue();

      PermissionIds.IsProtectedCoreLocked(
        RoleIds.Administrator,
        PermissionIds.AdminRolesManage,
        isSelected: false).ShouldBeFalse();

      PermissionIds.IsProtectedCoreLocked(
        RoleIds.Administrator,
        PermissionIds.ProfileRead,
        isSelected: true).ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task Group_Toggle_Off_Should_Keep_Selected_Protected_Core()
    {
      PermissionIds.PermissionGroup adminGroup = PermissionIds.GroupsByPrefix
        .Single(group => group.Prefix == "admin");

      HashSet<string> selected = new(adminGroup.PermissionIds, StringComparer.Ordinal)
      {
        PermissionIds.ProfileRead
      };

      foreach (string permissionId in adminGroup.PermissionIds)
      {
        if (PermissionIds.IsProtectedCoreLocked(RoleIds.Administrator, permissionId, selected.Contains(permissionId)))
        {
          continue;
        }

        selected.Remove(permissionId);
      }

      foreach (string permissionId in adminGroup.PermissionIds)
      {
        selected.ShouldContain(permissionId);
      }

      selected.ShouldContain(PermissionIds.ProfileRead);
      return Task.CompletedTask;
    }
  }
}
