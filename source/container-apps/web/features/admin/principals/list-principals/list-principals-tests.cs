#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0005;IDE0007;IDE0008

// Co-located Jaribu tests for ListPrincipals: serialization, validation, and the task 328 handler
// behavior (profile columns gated on admin.profiles.read, search, sort).

#region Purpose
// Jaribu runfile: ListPrincipals Response/DTO round-trip through ContractSerializationDefaults,
// the Query validator and query string, and the handler over in-memory stores with the real
// PermissionEvaluator: PII gating (null display name and email without admin.profiles.read, 403
// for search or profile sorts), search by name or email, each sort column both ways, and the
// endpoint policy plus the Administrator-only seed. HTTP 401/403 proofs live in
// web-server-integration-tests (principals-authorization-tests).
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Admin.Principals
{

  using System.Reflection;
  using System.Text.Json;
  using System.Threading.Tasks;
  using Microsoft.Extensions.Options;
  using Shouldly;
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Architecture.Authorization;
  using TimeWarp.Architecture.Features;
  using TimeWarp.Architecture.Features.Profiles.Application;
  using TimeWarp.Architecture.Features.Profiles.Domain;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using ListHandler = TimeWarp.Architecture.Features.Admin.Principals.Application.ListPrincipals.Handler;
  using DomainProfile = TimeWarp.Architecture.Features.Profiles.Domain.Profile;

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
    public static Task Dto_Should_CarryProfileFieldsThroughJson()
    {
      var dto = new ListPrincipals.PrincipalSummaryDto(
        PrincipalId.New(),
        PrincipalKind.Human,
        TrustTier.Keyed,
        isActive: true,
        isQuarantined: false,
        roleIds: [RoleIds.Member],
        displayName: "Ada",
        email: "ada@example.com");
      string json = JsonSerializer.Serialize(new ListPrincipals.Response(1, [dto]), ContractSerializationDefaults.Options);
      ListPrincipals.Response parsed = JsonSerializer
        .Deserialize<ListPrincipals.Response>(json, ContractSerializationDefaults.Options)
        .ShouldNotBeNull();
      parsed.Items[0].DisplayName.ShouldBe("Ada");
      parsed.Items[0].Email.ShouldBe("ada@example.com");
      return Task.CompletedTask;
    }

    public static Task Validator_Should_RejectUndefinedSortAndLongSearch()
    {
      ListPrincipals.Validator validator = new();
      Guid userId = Guid.NewGuid(); // AuthApiRequestValidator requires the session user id.
      validator.Validate(new ListPrincipals.Query { UserId = userId }).IsValid.ShouldBeTrue();
      validator.Validate(new ListPrincipals.Query { UserId = userId, SortBy = (PrincipalListSortColumn)99 }).IsValid.ShouldBeFalse();
      validator.Validate(new ListPrincipals.Query { UserId = userId, Search = new string('x', 254) }).IsValid.ShouldBeTrue();
      validator.Validate(new ListPrincipals.Query { UserId = userId, Search = new string('x', 255) }).IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task Query_Should_CarrySearchAndSortInTheQueryString()
    {
      string route = new ListPrincipals.Query
      {
        Search = " ada ",
        SortBy = PrincipalListSortColumn.Email,
        SortDescending = true,
      }.GetRouteWithQueryString();
      route.ShouldStartWith("api/admin/principals?");
      route.ShouldContain("Search=ada");
      route.ShouldContain("SortBy=Email");
      route.ShouldContain("SortDescending=true");
      return Task.CompletedTask;
    }

    public static Task Contract_Should_RequireAdminPrincipalsRead()
    {
      Attribute attribute = typeof(ListPrincipals).GetCustomAttributes()
        .Single(candidate => candidate.GetType().Name == "EndpointAuthorizeAttribute");
      ((string?)attribute.GetType().GetProperty("Policy")!.GetValue(attribute)).ShouldBe(PermissionIds.AdminPrincipalsRead);
      return Task.CompletedTask;
    }

    public static Task Seed_Should_GrantProfilesReadToAdministratorOnly()
    {
      PermissionIds.All.ShouldContain(PermissionIds.AdminProfilesRead);
      RolePermissionSeed.AdminPermissions.ShouldContain(PermissionIds.AdminProfilesRead);
      RolePermissionSeed.DefaultGrants[RoleIds.Administrator].ShouldContain(PermissionIds.AdminProfilesRead);
      RolePermissionSeed.DefaultGrants[RoleIds.Member].ShouldNotContain(PermissionIds.AdminProfilesRead);
      RolePermissionSeed.DefaultGrants[RoleIds.Developer].ShouldNotContain(PermissionIds.AdminProfilesRead);
      RolePermissionSeed.DefaultGrants[RoleIds.Operator].ShouldNotContain(PermissionIds.AdminProfilesRead);
      return Task.CompletedTask;
    }
  }

  [TestTag("Handler")]
  public class ListPrincipalsHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<ListPrincipalsHandler_Given_>();

    public static async Task Admin_Should_SeeProfileColumnsAndNullsForAgentsAndProfilelessRows()
    {
      ListWorld world = new();
      Principal ada = await world.AddHumanAsync("Ada Lovelace", "ada@example.com");
      Principal agent = await world.AddAsync(PrincipalKind.Agent);
      Principal noProfile = await world.AddAsync(PrincipalKind.Human);

      ListPrincipals.Response response = await world.ListAsync(new ListPrincipals.Query());

      response.Items.Select(item => item.PrincipalId.Value).ShouldContain(agent.Id.Value);
      Row(response, ada).DisplayName.ShouldBe("Ada Lovelace");
      Row(response, ada).Email.ShouldBe("ada@example.com");
      Row(response, agent).DisplayName.ShouldBeNull();
      Row(response, agent).Email.ShouldBeNull();
      Row(response, noProfile).DisplayName.ShouldBeNull();
    }

    public static async Task CallerWithoutProfilesRead_Should_GetRowsWithoutPii()
    {
      ListWorld world = new(grantProfilesRead: false);
      Principal ada = await world.AddHumanAsync("Ada Lovelace", "ada@example.com");

      ListPrincipals.Response response = await world.ListAsync(new ListPrincipals.Query());

      response.Items.Length.ShouldBe(2, "the caller row plus Ada: listing itself is still allowed");
      Row(response, ada).DisplayName.ShouldBeNull();
      Row(response, ada).Email.ShouldBeNull();
      response.Items.ShouldAllBe(item => item.DisplayName == null && item.Email == null);
    }

    public static async Task AnonymousCaller_Should_GetRowsWithoutPii()
    {
      ListWorld world = new(isAnonymous: true);
      await world.AddHumanAsync("Ada Lovelace", "ada@example.com");

      ListPrincipals.Response response = await world.ListAsync(new ListPrincipals.Query());

      response.Items.ShouldAllBe(item => item.DisplayName == null && item.Email == null);
    }

    public static async Task Search_Should_MatchNameOrEmailIgnoringCase()
    {
      ListWorld world = new();
      Principal ada = await world.AddHumanAsync("Ada Lovelace", "ada@example.com");
      Principal grace = await world.AddHumanAsync("Grace Hopper", "navy@example.com");
      await world.AddAsync(PrincipalKind.Agent);

      (await world.IdsAsync(new ListPrincipals.Query { Search = "LOVELACE" })).ShouldBe([ada.Id.Value]);
      (await world.IdsAsync(new ListPrincipals.Query { Search = "NAVY@" })).ShouldBe([grace.Id.Value]);
      (await world.IdsAsync(new ListPrincipals.Query { Search = "  hopper " })).ShouldBe([grace.Id.Value]);
      (await world.IdsAsync(new ListPrincipals.Query { Search = "@example.com" })).ShouldBe([ada.Id.Value, grace.Id.Value]);
      (await world.IdsAsync(new ListPrincipals.Query { Search = "nobody" })).ShouldBeEmpty();
      (await world.ListAsync(new ListPrincipals.Query { Search = "   " })).Items.Length.ShouldBe(4);
    }

    public static async Task SearchOrProfileSort_WithoutProfilesRead_Should_Be403()
    {
      ListWorld world = new(grantProfilesRead: false);
      await world.AddHumanAsync("Ada Lovelace", "ada@example.com");

      foreach (ListPrincipals.Query query in new[]
      {
        new ListPrincipals.Query { Search = "ada" },
        new ListPrincipals.Query { SortBy = PrincipalListSortColumn.DisplayName },
        new ListPrincipals.Query { SortBy = PrincipalListSortColumn.Email, SortDescending = true },
      })
      {
        SharedProblemDetails problem = (await world.HandleAsync(query)).AsT1;
        problem.Status.ShouldBe(403);
        problem.Title.ShouldBe("Profile access required");
      }

      // Sorting by non-profile columns stays allowed.
      (await world.HandleAsync(new ListPrincipals.Query { SortBy = PrincipalListSortColumn.Kind })).IsT0.ShouldBeTrue();
      (await world.HandleAsync(new ListPrincipals.Query { SortBy = PrincipalListSortColumn.CreatedAt, SortDescending = true })).IsT0.ShouldBeTrue();
    }

    public static async Task Sort_Should_OrderEachColumnBothWaysWithNullsLast()
    {
      ListWorld world = new();
      Principal bob = await world.AddHumanAsync("bob", "zed@example.com");
      Principal agent = await world.AddAsync(PrincipalKind.Agent);
      Principal alice = await world.AddHumanAsync("Alice", "amy@example.com");
      Principal noProfile = await world.AddAsync(PrincipalKind.Human);
      Guid caller = world.Caller!.Value.Value;
      Guid b = bob.Id.Value, g = agent.Id.Value, a = alice.Id.Value, n = noProfile.Id.Value;

      // Store order: caller, bob, agent, alice, noProfile. Profile-less rows (caller, agent, noProfile) sort last.
      (await world.IdsAsync(new ListPrincipals.Query())).ShouldBe([caller, b, g, a, n]);
      (await world.IdsAsync(new ListPrincipals.Query { SortDescending = true })).ShouldBe([n, a, g, b, caller]);
      (await world.IdsAsync(new ListPrincipals.Query { SortBy = PrincipalListSortColumn.DisplayName })).ShouldBe([a, b, caller, g, n]);
      (await world.IdsAsync(new ListPrincipals.Query { SortBy = PrincipalListSortColumn.DisplayName, SortDescending = true }))
        .ShouldBe([b, a, caller, g, n]);
      (await world.IdsAsync(new ListPrincipals.Query { SortBy = PrincipalListSortColumn.Email })).ShouldBe([a, b, caller, g, n]);
      (await world.IdsAsync(new ListPrincipals.Query { SortBy = PrincipalListSortColumn.Email, SortDescending = true }))
        .ShouldBe([b, a, caller, g, n]);
      // Kind by name: Agent < Human; ties keep store order.
      (await world.IdsAsync(new ListPrincipals.Query { SortBy = PrincipalListSortColumn.Kind })).ShouldBe([g, caller, b, a, n]);
      (await world.IdsAsync(new ListPrincipals.Query { SortBy = PrincipalListSortColumn.Kind, SortDescending = true }))
        .ShouldBe([caller, b, a, n, g]);
    }

    private static ListPrincipals.PrincipalSummaryDto Row(ListPrincipals.Response response, Principal principal) =>
      response.Items.Single(item => item.PrincipalId.Value == principal.Id.Value);
  }

  internal sealed class ListWorld
  {
    private readonly InMemoryPrincipalStore Principals = new();
    private readonly InMemoryProfileStore Profiles = new();
    private readonly ListWorldCaller CallerAccessor;
    private readonly ListHandler Handler;

    /// <summary>An administrator caller; pass null for an anonymous caller (no ambient principal).</summary>
    public ListWorld(bool grantProfilesRead = true, bool isAnonymous = false)
    {
      PrincipalId? caller = isAnonymous ? null : PrincipalId.New();
      Caller = caller;
      CallerAccessor = new ListWorldCaller(caller);
      InMemoryPrincipalRoleStore roles = new();
      InMemoryRolePermissionStore permissions = new();
      if (caller is { } callerId)
      {
        roles.SetRoleIdsAsync(callerId, [RoleIds.Administrator]).GetAwaiter().GetResult();
        Principals.AddPrincipalAsync(Principal.Create(PrincipalKind.Human, callerId)).GetAwaiter().GetResult();
      }

      if (!grantProfilesRead)
      {
        permissions.SetPermissionIdsForRoleAsync(
          RoleIds.Administrator,
          [.. RolePermissionSeed.DefaultGrants[RoleIds.Administrator].Where(static id => id != PermissionIds.AdminProfilesRead)])
          .GetAwaiter().GetResult();
      }

      EffectiveRolesResolver resolver = new(
        roles,
        Options.Create(new BootstrapAdministratorOptions { BootstrapAdministratorPrincipalIds = [] }));
      PermissionEvaluator evaluator = new(resolver, permissions, new NoAgentScopes());
      Handler = new ListHandler(Principals, resolver, CallerAccessor, evaluator, new ProfileDirectory(Profiles));
    }

    public PrincipalId? Caller { get; }

    public async Task<Principal> AddAsync(PrincipalKind kind)
    {
      // CreatedAt is the store order; keep it strictly increasing.
      await Task.Delay(3);
      Principal principal = Principal.Create(kind);
      await Principals.AddPrincipalAsync(principal);
      return principal;
    }

    public async Task<Principal> AddHumanAsync(string displayName, string email)
    {
      Principal principal = await AddAsync(PrincipalKind.Human);
      DomainProfile profile = DomainProfile.Create(ProfileId.From(principal.Id.Value), displayName, "en-US", "US", "system");
      profile.SetEmail(email);
      await Profiles.AddAsync(profile);
      return principal;
    }

    public Task<OneOf.OneOf<ListPrincipals.Response, SharedProblemDetails>> HandleAsync(ListPrincipals.Query query) =>
      Handler.Handle(query, CancellationToken.None);

    public async Task<ListPrincipals.Response> ListAsync(ListPrincipals.Query query) =>
      (await HandleAsync(query)).AsT0;

    public async Task<Guid[]> IdsAsync(ListPrincipals.Query query) =>
      [.. (await ListAsync(query)).Items.Select(item => item.PrincipalId.Value)];

    private sealed class ListWorldCaller : ICurrentPrincipalAccessor
    {
      private readonly PrincipalId? Id;

      public ListWorldCaller(PrincipalId? id) => Id = id;

      public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Id);
    }

    private sealed class NoAgentScopes : IAgentPermissionScopeSource
    {
      public IReadOnlyList<string>? GetHeldScopesFor(PrincipalId principalId) => null;
    }
  }
}
