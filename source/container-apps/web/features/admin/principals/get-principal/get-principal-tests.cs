#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0005;IDE0007;IDE0008

// Co-located Jaribu tests for GetPrincipal (task 328).
// Run standalone:  dotnet run source/container-apps/web/features/admin/principals/get-principal/get-principal-tests.cs

#region Purpose
// Jaribu runfile: GetPrincipal contract round-trip, validator, endpoint policy, and handler
// behavior over in-memory stores with the real PermissionEvaluator: the profile appears only
// for a caller holding admin.profiles.read, is null for a principal with no profile row (and
// no row is created), and a missing principal is 404. HTTP 401/403 proofs live in
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
  using GetHandler = TimeWarp.Architecture.Features.Admin.Principals.Application.GetPrincipal.Handler;
  using DomainProfile = TimeWarp.Architecture.Features.Profiles.Domain.Profile;

  [TestTag("Contracts")]
  public class GetPrincipalContract_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetPrincipalContract_Given_>();

    public static Task Response_Should_RoundTripThroughJson()
    {
      PrincipalId id = PrincipalId.New();
      GetPrincipal.Response response = new(new GetPrincipal.PrincipalDetailDto(
        id,
        PrincipalKind.Human,
        TrustTier.Keyed,
        isActive: true,
        isQuarantined: false,
        roleIds: [RoleIds.Member, RoleIds.Developer],
        profile: new GetPrincipal.PrincipalProfileDto("Ada", "ada@example.com", "en-US", "US", "dark", notifications: true)));

      string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      GetPrincipal.Response parsed = JsonSerializer
        .Deserialize<GetPrincipal.Response>(json, ContractSerializationDefaults.Options)
        .ShouldNotBeNull();

      parsed.Principal.PrincipalId.ShouldBe(id);
      parsed.Principal.Kind.ShouldBe(PrincipalKind.Human);
      parsed.Principal.RoleIds.ShouldBe([RoleIds.Member, RoleIds.Developer]);
      parsed.Principal.Profile.ShouldNotBeNull();
      parsed.Principal.Profile.DisplayName.ShouldBe("Ada");
      parsed.Principal.Profile.Email.ShouldBe("ada@example.com");
      parsed.Principal.Profile.Theme.ShouldBe("dark");
      parsed.Principal.Profile.Notifications.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task Response_WithNoProfile_Should_RoundTripNull()
    {
      GetPrincipal.Response response = new(new GetPrincipal.PrincipalDetailDto(
        PrincipalId.New(), PrincipalKind.Agent, TrustTier.Provisional, true, false, [RoleIds.Member]));
      string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      JsonSerializer.Deserialize<GetPrincipal.Response>(json, ContractSerializationDefaults.Options)
        .ShouldNotBeNull().Principal.Profile.ShouldBeNull();
      return Task.CompletedTask;
    }

    public static Task Validator_Should_RejectEmptyPrincipalId()
    {
      GetPrincipal.Validator validator = new();
      validator.Validate(new GetPrincipal.Query { PrincipalId = Guid.Empty }).IsValid.ShouldBeFalse();
      validator.Validate(new GetPrincipal.Query { PrincipalId = Guid.NewGuid() }).IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task Route_Should_CarryThePrincipalId()
    {
      var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
      new GetPrincipal.Query { PrincipalId = id }.GetRoute().ShouldEndWith($"api/admin/principals/{id}");
      return Task.CompletedTask;
    }

    public static Task MockFactory_Should_EchoTheRequestedId()
    {
      Guid id = Guid.NewGuid();
      GetPrincipal.GetMockResponseFactory()(new GetPrincipal.Query { PrincipalId = id })
        .Principal.PrincipalId.Value.ShouldBe(id);
      return Task.CompletedTask;
    }

    public static Task Contract_Should_RequireAdminPrincipalsRead()
    {
      Attribute attribute = typeof(GetPrincipal).GetCustomAttributes()
        .Single(candidate => candidate.GetType().Name == "EndpointAuthorizeAttribute");
      ((string?)attribute.GetType().GetProperty("Policy")!.GetValue(attribute)).ShouldBe(PermissionIds.AdminPrincipalsRead);
      return Task.CompletedTask;
    }
  }

  [TestTag("Handler")]
  public class GetPrincipalHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetPrincipalHandler_Given_>();

    public static async Task Admin_Should_SeeRolesStatusAndFullProfile()
    {
      GetWorld world = new();
      Principal ada = await world.AddAsync(PrincipalKind.Human);
      await world.AddProfileAsync(ada, "Ada Lovelace", "ada@example.com");

      GetPrincipal.PrincipalDetailDto detail = (await world.GetAsync(ada.Id)).AsT0.Principal;

      detail.PrincipalId.Value.ShouldBe(ada.Id.Value);
      detail.Kind.ShouldBe(PrincipalKind.Human);
      detail.IsActive.ShouldBeTrue();
      detail.IsQuarantined.ShouldBeFalse();
      detail.RoleIds.ShouldBe([RoleIds.Member]);
      detail.Profile.ShouldNotBeNull();
      detail.Profile.DisplayName.ShouldBe("Ada Lovelace");
      detail.Profile.Email.ShouldBe("ada@example.com");
      detail.Profile.Language.ShouldBe("en-US");
      detail.Profile.Region.ShouldBe("US");
      detail.Profile.Theme.ShouldBe("system");
      detail.Profile.Notifications.ShouldBeFalse();
    }

    public static async Task PrincipalWithoutProfile_Should_HaveNullProfileAndCreateNone()
    {
      GetWorld world = new();
      Principal agent = await world.AddAsync(PrincipalKind.Agent);

      (await world.GetAsync(agent.Id)).AsT0.Principal.Profile.ShouldBeNull();
      (await world.Profiles.FindAsync(ProfileId.From(agent.Id.Value))).ShouldBeNull("opening a principal never creates a profile");
    }

    public static async Task CallerWithoutProfilesRead_Should_GetNullProfile()
    {
      GetWorld world = new(grantProfilesRead: false);
      Principal ada = await world.AddAsync(PrincipalKind.Human);
      await world.AddProfileAsync(ada, "Ada Lovelace", "ada@example.com");

      GetPrincipal.PrincipalDetailDto detail = (await world.GetAsync(ada.Id)).AsT0.Principal;

      detail.PrincipalId.Value.ShouldBe(ada.Id.Value);
      detail.Profile.ShouldBeNull();
    }

    public static async Task MissingPrincipal_Should_Be404()
    {
      GetWorld world = new();

      SharedProblemDetails problem = (await world.GetAsync(PrincipalId.New())).AsT1;

      problem.Status.ShouldBe(404);
    }
  }

  internal sealed class GetWorld
  {
    private readonly InMemoryPrincipalStore Principals = new();
    private readonly GetHandler Handler;

    public GetWorld(bool grantProfilesRead = true)
    {
      var caller = PrincipalId.New();
      InMemoryPrincipalRoleStore roles = new();
      roles.SetRoleIdsAsync(caller, [RoleIds.Administrator]).GetAwaiter().GetResult();
      InMemoryRolePermissionStore permissions = new();
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
      Handler = new GetHandler(Principals, resolver, new FixedCaller(caller), evaluator, new ProfileDirectory(Profiles));
    }

    public InMemoryProfileStore Profiles { get; } = new();

    public async Task<Principal> AddAsync(PrincipalKind kind)
    {
      Principal principal = Principal.Create(kind);
      await Principals.AddPrincipalAsync(principal);
      return principal;
    }

    public async Task AddProfileAsync(Principal principal, string displayName, string email)
    {
      DomainProfile profile = DomainProfile.Create(ProfileId.From(principal.Id.Value), displayName, "en-US", "US", "system");
      profile.SetEmail(email);
      await Profiles.AddAsync(profile);
    }

    public Task<OneOf.OneOf<GetPrincipal.Response, SharedProblemDetails>> GetAsync(PrincipalId id) =>
      Handler.Handle(new GetPrincipal.Query { PrincipalId = id.Value }, CancellationToken.None);

    private sealed class FixedCaller : ICurrentPrincipalAccessor
    {
      private readonly PrincipalId Id;

      public FixedCaller(PrincipalId id) => Id = id;

      public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
        Task.FromResult<PrincipalId?>(Id);
    }

    private sealed class NoAgentScopes : IAgentPermissionScopeSource
    {
      public IReadOnlyList<string>? GetHeldScopesFor(PrincipalId principalId) => null;
    }
  }
}
