#region Purpose
// Tests for TWA0039–TWA0043: an aggregate root agrees with its [Operation] group's contracts
// (details members, Set-list mutations, query projection, uncovered commands, ambiguous links).
#endregion

#region Design
// Every test builds the three-project layout from ContractAgreementTestFixture (contracts and
// domain as metadata to the application project), so diagnostics carry no location. Drift cases
// edit a copy of the Roles or Profile shape through Edit, which asserts the replaced text is present
// (ShouldContain) so a fixture edit cannot silently turn a test vacuous.
#endregion

// ReSharper disable InconsistentNaming
namespace ContractAgreementAnalyzer_;

using Microsoft.CodeAnalysis.CSharp.Testing;
using TimeWarp.Architecture.Analyzers;
using TimeWarp.Architecture.Analyzers.Tests;
using static TimeWarp.Architecture.Analyzers.Tests.ContractAgreementTestFixture;

public class Should_Check_Contract_Agreement
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Check_Contract_Agreement>();

  private const string RolesGroup = "admin.roles";
  private const string ProfileGroup = "profile";

  private static string Edit(string source, string oldText, string newText)
  {
    source.ShouldContain(oldText);
    return source.Replace(oldText, newText, StringComparison.Ordinal);
  }

  private static string Missing(string root, string member) =>
    $"'{root}' has no public member '{member}'; names match exactly, so rename the side that drifted";

  private static string Mismatch(string root, string member, string rootType, string contractType) =>
    $"'{root}.{member}' is {rootType} but the contract member is {contractType}";

  public static async Task Given_Roles_Shape_IsClean()
  {
    await Test(RoleContracts, RoleDomain).RunAsync();
  }

  public static async Task Given_Profile_Shape_IsClean()
  {
    await Test(ProfileContracts, ProfileDomain).RunAsync();
  }

  public static async Task Given_Both_Shapes_Together_AreClean()
  {
    await Test([RoleContracts, ProfileContracts], [RoleDomain, ProfileDomain]).RunAsync();
  }

  public static async Task Given_Details_Member_Missing_On_Root_Flags_TWA0039()
  {
    // The pre-474 Profile drift: the contract said Alias, the domain says DisplayName.
    string contracts = Edit(ProfileContracts, "string DisplayName { get; set; }\n    string? Email", "string Alias { get; set; }\n    string? Email");
    contracts = Edit(contracts, "public string DisplayName { get; set; }", "public string Alias { get; set; }");

    await Test
    (
      contracts,
      ProfileDomain,
      Flag(ContractAgreementAnalyzer.DetailsMemberId, "Profile", "IProfileDetails", "Alias", ProfileGroup, Missing("Profile", "Alias")),
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Profiles.GetProfile.Response", "Alias", "Profile", ProfileGroup, Missing("Profile", "Alias"))
    ).RunAsync();
  }

  public static async Task Given_Details_Member_Type_Disagrees_Flags_TWA0039()
  {
    string domain = Edit(ProfileDomain, "public bool Notifications { get; private set; }", "public string Notifications { get; private set; } = \"\";");

    await Test
    (
      ProfileContracts,
      domain,
      Flag(ContractAgreementAnalyzer.DetailsMemberId, "Profile", "IProfileDetails", "Notifications", ProfileGroup, Mismatch("Profile", "Notifications", "string", "bool")),
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Profiles.GetProfile.Response", "Notifications", "Profile", ProfileGroup, Mismatch("Profile", "Notifications", "string", "bool"))
    ).RunAsync();
  }

  public static async Task Given_Details_Member_Nullability_Disagrees_Flags_TWA0039()
  {
    // The write side is strict; GetProfile.Response.Email (string? against the root's string) is
    // read widening and passes TWA0041 (task 475).
    string domain = Edit(ProfileDomain, "public string? Email { get; private set; }", "public string Email { get; private set; } = \"\";");

    await Test
    (
      ProfileContracts,
      domain,
      Flag(ContractAgreementAnalyzer.DetailsMemberId, "Profile", "IProfileDetails", "Email", ProfileGroup, Mismatch("Profile", "Email", "string", "string?"))
    ).RunAsync();
  }

  public static async Task Given_Set_Command_Without_Mutation_Flags_TWA0040()
  {
    string domain = Edit(RoleDomain, "public void SetAgentScopes(IEnumerable<string> scopes) { }", "");

    await Test
    (
      RoleContracts,
      domain,
      Flag(ContractAgreementAnalyzer.SetMutationId, "App.Features.Admin.Roles.SetRoleAgentScopes.Command", "Scopes", "string", "Role", "SetAgentScopes", RolesGroup)
    ).RunAsync();
  }

  public static async Task Given_Set_Mutation_With_Wrong_Element_Type_Flags_TWA0040()
  {
    string domain = Edit(RoleDomain, "SetPermissions(IEnumerable<string> permissionIds)", "SetPermissions(IEnumerable<int> permissionIds)");

    await Test
    (
      RoleContracts,
      domain,
      Flag(ContractAgreementAnalyzer.SetMutationId, "App.Features.Admin.Roles.SetRolePermissions.Command", "PermissionIds", "string", "Role", "SetPermissions", RolesGroup)
    ).RunAsync();
  }

  public static async Task Given_Set_Mutation_That_Is_Not_Public_Flags_TWA0040()
  {
    string domain = Edit(RoleDomain, "public void SetPermissions(", "internal void SetPermissions(");

    await Test
    (
      RoleContracts,
      domain,
      Flag(ContractAgreementAnalyzer.SetMutationId, "App.Features.Admin.Roles.SetRolePermissions.Command", "PermissionIds", "string", "Role", "SetPermissions", RolesGroup)
    ).RunAsync();
  }

  public static async Task Given_Query_Response_Member_Drifts_Flags_TWA0041()
  {
    // The pre-474 Roles drift: responses said PermissionIds, the root says Permissions.
    string contracts = Edit(RoleContracts, "public List<string> Permissions { get; set; } = [];", "public List<string> PermissionIds { get; set; } = [];");

    await Test
    (
      contracts,
      RoleDomain,
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Admin.Roles.GetRole.Response", "PermissionIds", "Role", RolesGroup, Missing("Role", "PermissionIds")),
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Admin.Roles.GetRoles.RoleDto", "PermissionIds", "Role", RolesGroup, Missing("Role", "PermissionIds"))
    ).RunAsync();
  }

  public static async Task Given_Query_Response_Element_Type_Disagrees_Flags_TWA0041()
  {
    string domain = Edit(RoleDomain, "public sealed record AgentScopeGrant(string Scope);", "public sealed record AgentScopeGrant(int Scope);");

    await Test
    (
      RoleContracts,
      domain,
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Admin.Roles.GetRole.Response", "AgentScopes", "Role", RolesGroup, Mismatch("Role", "AgentScopes", "IReadOnlyList<AgentScopeGrant>", "List<string>")),
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Admin.Roles.GetRoles.RoleDto", "AgentScopes", "Role", RolesGroup, Mismatch("Role", "AgentScopes", "IReadOnlyList<AgentScopeGrant>", "List<string>"))
    ).RunAsync();
  }

  public static async Task Given_Root_Key_Type_Disagrees_Flags_TWA0041()
  {
    string domain = Edit(RoleDomain, "public readonly record struct RoleId(Guid Value) { public bool IsEmpty => Value == Guid.Empty; }", "public readonly record struct RoleId(long Value) { public bool IsEmpty => Value == 0; }");

    await Test
    (
      RoleContracts,
      domain,
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Admin.Roles.GetRole.Response", "RoleId", "Role", RolesGroup, Mismatch("Role", "Id", "RoleId", "Guid")),
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Admin.Roles.GetRoles.RoleDto", "RoleId", "Role", RolesGroup, Mismatch("Role", "Id", "RoleId", "Guid"))
    ).RunAsync();
  }

  public static async Task Given_Uncomputed_Response_Member_Flags_TWA0041()
  {
    string contracts = Edit(ProfileContracts, "[Computed]\n      public string Avatar", "public string Avatar");

    await Test
    (
      contracts,
      ProfileDomain,
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.Profiles.GetProfile.Response", "Avatar", "Profile", ProfileGroup, Missing("Profile", "Avatar"))
    ).RunAsync();
  }

  public static async Task Given_Computed_Response_Class_IsClean()
  {
    string contracts = Edit(ProfileContracts, "[Computed]\n      public string Avatar", "public string Avatar");
    contracts = Edit(contracts, "public sealed class Response : BaseResponse, IProfileDetails", "[Computed]\n    public sealed class Response : BaseResponse, IProfileDetails");

    await Test(contracts, ProfileDomain).RunAsync();
  }

  public static async Task Given_Command_Matching_No_Check_Flags_TWA0042()
  {
    string contracts = Edit(RoleContracts, "[NotAggregate(\"Removes the role; only the route id travels.\")]", "");

    await Test
    (
      contracts,
      RoleDomain,
      Flag(ContractAgreementAnalyzer.UncoveredCommandId, "App.Features.Admin.Roles.DeleteRole.Command", "roles.delete", RolesGroup, "Role")
    ).RunAsync();
  }

  public static async Task Given_NotAggregate_With_Blank_Reason_Still_Flags_TWA0042()
  {
    string contracts = Edit(RoleContracts, "[NotAggregate(\"Removes the role; only the route id travels.\")]", "[NotAggregate(\"  \")]");

    await Test
    (
      contracts,
      RoleDomain,
      Flag(ContractAgreementAnalyzer.UncoveredCommandId, "App.Features.Admin.Roles.DeleteRole.Command", "roles.delete", RolesGroup, "Role")
    ).RunAsync();
  }

  public static async Task Given_Out_Of_Slice_Operation_Without_Opt_Out_Flags()
  {
    // The xAI key operations live in AgentChats but share the profile group, so they are checked.
    string contracts = Edit(ProfileContracts, "[NotAggregate(\"The key is a protected secret row, not Profile state.\")]", "");
    contracts = Edit(contracts, "[NotAggregate(\"The status reads a protected secret row, not Profile state.\")]", "");

    await Test
    (
      contracts,
      ProfileDomain,
      Flag(ContractAgreementAnalyzer.ProjectionId, "App.Features.AgentChats.GetUserXaiApiKeyStatus.Response", "Configured", "Profile", ProfileGroup, Missing("Profile", "Configured")),
      Flag(ContractAgreementAnalyzer.UncoveredCommandId, "App.Features.AgentChats.SetUserXaiApiKey.Command", "user-xai-api-key.set", ProfileGroup, "Profile")
    ).RunAsync();
  }

  public static async Task Given_Two_Roots_In_One_Slice_Flags_TWA0043()
  {
    const string secondRoot =
      """
      namespace App.Features.Admin.Roles.Domain
      {
        public sealed class RoleTemplate : Entity<Guid>, IAggregateRoot
        {
          private RoleTemplate(Guid id) : base(id) { }
        }
      }
      """;

    await Test
    (
      [RoleContracts],
      [RoleDomain, secondRoot],
      Flag(ContractAgreementAnalyzer.AmbiguousRootId, RolesGroup, "App.Features.Admin.Roles.Domain.Role, App.Features.Admin.Roles.Domain.RoleTemplate")
    ).RunAsync();
  }

  public static async Task Given_A_Reference_That_Joins_Both_Sides_Reports_Once()
  {
    // web-server sees contracts and domain through web-application; only web-application reports.
    string contracts = Edit(RoleContracts, "[NotAggregate(\"Removes the role; only the route id travels.\")]", "");
    CSharpAnalyzerTest<ContractAgreementAnalyzer, RoslynTestVerifier> test = Test
    (
      contracts,
      RoleDomain,
      Flag(ContractAgreementAnalyzer.UncoveredCommandId, "App.Features.Admin.Roles.DeleteRole.Command", "roles.delete", RolesGroup, "Role")
    );
    ProjectState middle = test.TestState.AdditionalProjects["Middle"];
    middle.Sources.Add(("Middle.cs", "namespace App.Middle { public static class Marker { } }"));
    middle.AdditionalProjectReferences.AddRange(["Foundation", "Contracts", "Domain"]);
    test.TestState.AdditionalProjectReferences.Add("Middle");

    await test.RunAsync();
  }

  public static async Task Given_Group_With_No_Root_In_Its_Slice_IsClean()
  {
    // Contracts drift freely when no root lives in the slice namespace: nothing is linked.
    // The same two drifts flag TWA0041 and TWA0042 when the root is in the slice (tests above).
    string contracts = Edit(RoleContracts, "public List<string> Permissions { get; set; } = [];", "public List<string> PermissionIds { get; set; } = [];");
    contracts = Edit(contracts, "[NotAggregate(\"Removes the role; only the route id travels.\")]", "");
    string domain = Edit(RoleDomain, "namespace App.Features.Admin.Roles.Domain", "namespace App.Features.Elsewhere.Domain");

    await Test(contracts, domain).RunAsync();
  }
}
