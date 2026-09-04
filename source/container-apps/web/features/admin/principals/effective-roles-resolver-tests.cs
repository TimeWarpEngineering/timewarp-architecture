#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:package Microsoft.Extensions.Options
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Co-located Jaribu tests for EffectiveRolesResolver SSOT algorithm (147-004).

#region Purpose
// Host-free coverage of EffectiveRolesResolver + InMemoryPrincipalRoleStore first-admin claim
// and fail-closed wrapping of role-store read failures (task 160).
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features
{

  using System.Threading.Tasks;
  using Microsoft.Extensions.Options;
  using Shouldly;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Application")]
  public class EffectiveRolesResolver_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<EffectiveRolesResolver_Given_>();

    public static async Task EmptyStore_Should_YieldMemberOnly()
    {
      EffectiveRolesResolver resolver = CreateResolver(
        store: new InMemoryPrincipalRoleStore(),
        bootstrap: []);
      PrincipalId id = PrincipalId.New();

      IReadOnlyList<Guid> roles = await resolver.GetEffectiveRoleIdsAsync(id);

      roles.ShouldBe([RoleIds.Member]);
    }

    public static async Task NonEmptyStore_Should_UseExactlyStoredRoles()
    {
      InMemoryPrincipalRoleStore store = new();
      PrincipalId id = PrincipalId.New();
      await store.SetRoleIdsAsync(id, [RoleIds.Administrator, RoleIds.Developer]);

      EffectiveRolesResolver resolver = CreateResolver(store, bootstrap: []);
      IReadOnlyList<Guid> roles = await resolver.GetEffectiveRoleIdsAsync(id);

      roles.ShouldBe([RoleIds.Administrator, RoleIds.Developer]);
      roles.ShouldNotContain(RoleIds.Member);
    }

    public static async Task Bootstrap_Should_UnionAdministratorAndMember()
    {
      PrincipalId id = PrincipalId.New();
      InMemoryPrincipalRoleStore store = new();
      await store.SetRoleIdsAsync(id, [RoleIds.Developer]);

      EffectiveRolesResolver resolver = CreateResolver(store, bootstrap: [id.Value.ToString()]);
      IReadOnlyList<Guid> roles = await resolver.GetEffectiveRoleIdsAsync(id);

      roles.ShouldBe([RoleIds.Member, RoleIds.Administrator, RoleIds.Developer]);
    }

    public static async Task BootstrapEmptyStore_Should_YieldMemberAndAdministrator()
    {
      PrincipalId id = PrincipalId.New();
      EffectiveRolesResolver resolver = CreateResolver(
        new InMemoryPrincipalRoleStore(),
        bootstrap: [id.Value.ToString()]);

      IReadOnlyList<Guid> roles = await resolver.GetEffectiveRoleIdsAsync(id);

      roles.ShouldBe([RoleIds.Member, RoleIds.Administrator]);
    }

    public static async Task InvalidBootstrapGuid_Should_BeIgnored()
    {
      PrincipalId id = PrincipalId.New();
      EffectiveRolesResolver resolver = CreateResolver(
        new InMemoryPrincipalRoleStore(),
        bootstrap: ["not-a-guid", Guid.Empty.ToString()]);

      IReadOnlyList<Guid> roles = await resolver.GetEffectiveRoleIdsAsync(id);

      roles.ShouldBe([RoleIds.Member]);
    }

    public static async Task Ordering_Should_FollowRoleIdsAll()
    {
      InMemoryPrincipalRoleStore store = new();
      PrincipalId id = PrincipalId.New();
      await store.SetRoleIdsAsync(
        id,
        [RoleIds.Developer, RoleIds.Member, RoleIds.Operator, RoleIds.Administrator]);

      EffectiveRolesResolver resolver = CreateResolver(store, bootstrap: []);
      IReadOnlyList<Guid> roles = await resolver.GetEffectiveRoleIdsAsync(id);

      roles.ShouldBe(RoleIds.All);
    }

    public static async Task StoreReadFailure_Should_ThrowRoleResolutionFailedException()
    {
      EffectiveRolesResolver resolver = CreateResolver(
        store: new ThrowingGetPrincipalRoleStore(),
        bootstrap: []);
      PrincipalId id = PrincipalId.New();

      RoleResolutionFailedException exception = await Should.ThrowAsync<RoleResolutionFailedException>(
        () => resolver.GetEffectiveRoleIdsAsync(id));

      exception.InnerException.ShouldBeOfType<InvalidOperationException>();
      exception.InnerException!.Message.ShouldBe(ThrowingGetPrincipalRoleStore.FailureMessage);
    }

    public static async Task Cancellation_Should_NotWrapAsRoleResolutionFailed()
    {
      EffectiveRolesResolver resolver = CreateResolver(
        store: new CancelledGetPrincipalRoleStore(),
        bootstrap: []);
      using CancellationTokenSource cancellationTokenSource = new();
      cancellationTokenSource.Cancel();

      await Should.ThrowAsync<OperationCanceledException>(
        () => resolver.GetEffectiveRoleIdsAsync(PrincipalId.New(), cancellationTokenSource.Token));
    }

    private static EffectiveRolesResolver CreateResolver(
      IPrincipalRoleStore store,
      string[] bootstrap)
    {
      IOptions<BootstrapAdministratorOptions> options = Options.Create(
        new BootstrapAdministratorOptions
        {
          BootstrapAdministratorPrincipalIds = bootstrap
        });
      return new EffectiveRolesResolver(store, options);
    }

    private sealed class ThrowingGetPrincipalRoleStore : IPrincipalRoleStore
    {
      public const string FailureMessage = "simulated role-store failure";

      public Task<IReadOnlyList<Guid>> GetRoleIdsAsync(
        PrincipalId principalId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(FailureMessage);

      public Task SetRoleIdsAsync(
        PrincipalId principalId,
        IReadOnlyList<Guid> roleIds,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

      public Task<bool> TryClaimFirstAdministratorAsync(
        PrincipalId principalId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
    }

    private sealed class CancelledGetPrincipalRoleStore : IPrincipalRoleStore
    {
      public Task<IReadOnlyList<Guid>> GetRoleIdsAsync(
        PrincipalId principalId,
        CancellationToken cancellationToken = default)
      {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<Guid>>([]);
      }

      public Task SetRoleIdsAsync(
        PrincipalId principalId,
        IReadOnlyList<Guid> roleIds,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

      public Task<bool> TryClaimFirstAdministratorAsync(
        PrincipalId principalId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
    }
  }

  [TestTag("Application")]
  public class InMemoryPrincipalRoleStore_FirstAdministrator_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<InMemoryPrincipalRoleStore_FirstAdministrator_Given_>();

    public static async Task EmptyStore_FirstClaim_Should_AssignAdministratorAndMember()
    {
      InMemoryPrincipalRoleStore store = new();
      PrincipalId first = PrincipalId.New();

      bool claimed = await store.TryClaimFirstAdministratorAsync(first);

      claimed.ShouldBeTrue();
      IReadOnlyList<Guid> roles = await store.GetRoleIdsAsync(first);
      roles.ShouldBe([RoleIds.Administrator, RoleIds.Member], ignoreOrder: true);
    }

    public static async Task SecondClaim_Should_NotBecomeAdministrator()
    {
      InMemoryPrincipalRoleStore store = new();
      PrincipalId first = PrincipalId.New();
      PrincipalId second = PrincipalId.New();

      (await store.TryClaimFirstAdministratorAsync(first)).ShouldBeTrue();
      (await store.TryClaimFirstAdministratorAsync(second)).ShouldBeFalse();

      (await store.GetRoleIdsAsync(second)).ShouldBeEmpty();
      EffectiveRolesResolver resolver = new(
        store,
        Options.Create(new BootstrapAdministratorOptions()));
      IReadOnlyList<Guid> secondEffective = await resolver.GetEffectiveRoleIdsAsync(second);
      secondEffective.ShouldBe([RoleIds.Member]);
    }
  }
}
