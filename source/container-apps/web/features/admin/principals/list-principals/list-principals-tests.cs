#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058

// Co-located Jaribu contract tests for ListPrincipals serialization.

#region Purpose
// Jaribu runfile: ListPrincipals Response/DTO round-trip through ContractSerializationDefaults.
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
  using Shouldly;
  using TimeWarp.Architecture.Features;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Contracts")]
  public class ListPrincipalsResponse_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<ListPrincipalsResponse_Given_>();

    public static Task ValidSummary_Should_RoundTripThroughJson()
    {
      var id = PrincipalId.From(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
      var dto = new ListPrincipals.PrincipalSummaryDto(
        id,
        PrincipalKind.Human,
        TrustTier.Keyed,
        isActive: true,
        isQuarantined: false,
        roleIds: [RoleIds.Member, RoleIds.Administrator]);

      ListPrincipals.Response response = new(1, [dto]);

      string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      ListPrincipals.Response? parsed =
        JsonSerializer.Deserialize<ListPrincipals.Response>(json, ContractSerializationDefaults.Options);

      parsed.ShouldNotBeNull();
      parsed.TotalCount.ShouldBe(1);
      parsed.Items.Length.ShouldBe(1);
      parsed.Items[0].PrincipalId.ShouldBe(id);
      parsed.Items[0].Kind.ShouldBe(PrincipalKind.Human);
      parsed.Items[0].RoleIds.ShouldBe([RoleIds.Member, RoleIds.Administrator]);
      return Task.CompletedTask;
    }

    public static Task MockFactory_Should_ReturnSeededRows()
    {
      ListPrincipals.Response response = ListPrincipals.GetMockResponseFactory()(new ListPrincipals.Query());
      response.TotalCount.ShouldBe(2);
      response.Items.Length.ShouldBe(2);
      return Task.CompletedTask;
    }
  }
}
