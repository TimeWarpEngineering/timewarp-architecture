#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Co-located Jaribu tests for GetProfile (tasks 148/149): contract round-trip, mock factory,
// in-memory store, create-if-missing, and deterministic local Multiavatar.
// Run standalone:  dotnet run source/container-apps/web/features/profile/get-profile/get-profile-tests.cs

#region Purpose
// Jaribu runfile: GetProfile contract + store + handler coverage (tasks 148/149).
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Profiles
{

  using System;
  using System.Text.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Architecture.Features.Profiles.Application;
  using TimeWarp.Architecture.Features.Profiles.Domain;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.Profiles.GetProfile;
  using ProfileHandler = TimeWarp.Architecture.Features.Profiles.Application.GetProfile.Handler;
  // Aggregator safety (task 164): under JARIBU_MULTI the compilation references the full host
  // closure including Web.Spa, whose Profile.razor component is ALSO
  // TimeWarp.Architecture.Features.Profiles.Profile — and namespace members beat usings, so an
  // unqualified "Profile" flips to the component there (and aliasing the same name trips CS0576).
  // DomainProfile pins the domain entity unambiguously in both compile modes.
  using DomainProfile = TimeWarp.Architecture.Features.Profiles.Domain.Profile;

  [TestTag("Contracts")]
  public class GetProfileResponse_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetProfileResponse_Given_>();

    public static Task ValidResponse_Should_RoundTripThroughJson()
    {
      Response response = new Response(
        alias: "Ada",
        email: "ada@example.com",
        language: "en-US",
        region: "US",
        theme: "system",
        notifications: true,
        avatar: "data:image/svg+xml;base64,abc");

      string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      Response? parsed = JsonSerializer.Deserialize<Response>(json, ContractSerializationDefaults.Options);

      parsed.ShouldNotBeNull();
      parsed.Alias.ShouldBe("Ada");
      parsed.Email.ShouldBe("ada@example.com");
      parsed.Language.ShouldBe("en-US");
      parsed.Region.ShouldBe("US");
      parsed.Theme.ShouldBe("system");
      parsed.Notifications.ShouldBeTrue();
      parsed.Avatar.ShouldBe("data:image/svg+xml;base64,abc");
      return Task.CompletedTask;
    }

    public static Task MockFactory_Should_ReturnDefaults()
    {
      Response response = GetMockResponseFactory()(new Query());
      response.Alias.ShouldBe("alias");
      response.Email.ShouldBeNull();
      response.Language.ShouldBe("en-US");
      response.Region.ShouldBe("US");
      response.Theme.ShouldBe("system");
      response.Notifications.ShouldBeFalse();
      response.Avatar.ShouldNotBeNullOrEmpty();
      return Task.CompletedTask;
    }

    public static Task EmptyAliasJson_Should_ThrowDuringDeserialization()
    {
      const string json =
        """{"alias":"","email":null,"language":"en-US","region":"US","theme":"system","notifications":false,"avatar":"x"}""";

      Should.Throw<Exception>(() =>
        JsonSerializer.Deserialize<Response>(json, ContractSerializationDefaults.Options));
      return Task.CompletedTask;
    }
  }

  [TestTag("Store")]
  public class InMemoryProfileStore_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<InMemoryProfileStore_Given_>();

    public static async Task Find_missing_Should_ReturnNull()
    {
      InMemoryProfileStore store = new InMemoryProfileStore();
      DomainProfile? found = await store.FindAsync(ProfileId.From(Guid.NewGuid()));
      found.ShouldBeNull();
    }

    public static async Task Add_then_Find_Should_ReturnSameId()
    {
      InMemoryProfileStore store = new InMemoryProfileStore();
      ProfileId id = ProfileId.From(Guid.Parse("11111111-2222-3333-4444-555555555555"));
      DomainProfile profile = DomainProfile.Create(id, "Member", "en-US", "US", "system");

      await store.AddAsync(profile);
      DomainProfile? found = await store.FindAsync(id);

      found.ShouldNotBeNull();
      found.Id.ShouldBe(id);
      found.DisplayName.ShouldBe("Member");
    }

    public static async Task Add_duplicate_Should_Throw()
    {
      InMemoryProfileStore store = new InMemoryProfileStore();
      ProfileId id = ProfileId.From(Guid.NewGuid());
      await store.AddAsync(DomainProfile.Create(id, "A", "en-US", "US", "system"));

      await Should.ThrowAsync<InvalidOperationException>(async () =>
        await store.AddAsync(DomainProfile.Create(id, "B", "en-US", "US", "system")));
    }
  }

  [TestTag("Handler")]
  public class GetProfileHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetProfileHandler_Given_>();

    public static async Task Anonymous_Should_ReturnMock()
    {
      InMemoryProfileStore store = new InMemoryProfileStore();
      ProfileHandler handler = CreateHandler(userId: null, store);

      OneOf<Response, SharedProblemDetails> result =
        await handler.Handle(new Query(), CancellationToken.None);

      Response response = result.Match(
        ok => ok,
        _ => throw new InvalidOperationException("Expected Response"));

      response.Alias.ShouldBe("alias");
      response.Language.ShouldBe("en-US");
      response.Avatar.ShouldStartWith("data:image/svg+xml;base64,");
      response.Avatar.Length.ShouldBeGreaterThan(500);
    }

    public static async Task Authenticated_missing_profile_Should_CreateDefaults()
    {
      Guid userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
      InMemoryProfileStore store = new InMemoryProfileStore();
      ProfileHandler handler = CreateHandler(userId, store);

      OneOf<Response, SharedProblemDetails> result =
        await handler.Handle(new Query(), CancellationToken.None);

      Response response = result.Match(
        ok => ok,
        _ => throw new InvalidOperationException("Expected Response"));

      response.Alias.ShouldBe("Member");
      response.Email.ShouldBeNull();
      response.Language.ShouldBe("en-US");
      response.Region.ShouldBe("US");
      response.Theme.ShouldBe("system");
      response.Notifications.ShouldBeFalse();
      response.Avatar.ShouldNotBeNullOrEmpty();
      response.Avatar.ShouldStartWith("data:image/svg+xml;base64,");

      DomainProfile? stored = await store.FindAsync(ProfileId.From(userId));
      stored.ShouldNotBeNull();
      stored.DisplayName.ShouldBe("Member");
      // Task 150: the same principal Guid must always resolve to the store row keyed ProfileId.From(guid).
      stored.Id.ShouldBe(ProfileId.From(userId));
    }

    public static async Task Authenticated_existing_profile_Should_ReturnStoredFields()
    {
      Guid userId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
      ProfileId id = ProfileId.From(userId);
      InMemoryProfileStore store = new InMemoryProfileStore();
      DomainProfile seeded = DomainProfile.Create(id, "Grace", "fr-FR", "FR", "dark");
      seeded.SetEmail("grace@example.com");
      seeded.EnableNotifications();
      await store.AddAsync(seeded);

      ProfileHandler handler = CreateHandler(userId, store);
      Response response = (await handler.Handle(new Query(), CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected Response"));

      response.Alias.ShouldBe("Grace");
      response.Email.ShouldBe("grace@example.com");
      response.Language.ShouldBe("fr-FR");
      response.Region.ShouldBe("FR");
      response.Theme.ShouldBe("dark");
      response.Notifications.ShouldBeTrue();
    }

    public static async Task Avatar_same_userId_Should_Be_Deterministic()
    {
      Guid userId = Guid.Parse("cccccccc-dddd-eeee-ffff-000000000001");
      InMemoryProfileStore store = new InMemoryProfileStore();
      ProfileHandler handler = CreateHandler(userId, store);

      Response first = (await handler.Handle(new Query(), CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected Response"));
      Response second = (await handler.Handle(new Query(), CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected Response"));

      first.Avatar.ShouldStartWith("data:image/svg+xml;base64,");
      first.Avatar.ShouldBe(second.Avatar);
      first.Avatar.Length.ShouldBeGreaterThan(200);
    }

    private static ProfileHandler CreateHandler(Guid? userId, IProfileStore store)
    {
      PrincipalId? principalId = userId is null ? null : PrincipalId.From(userId.Value);
      return new ProfileHandler(
        new StubCurrentPrincipalAccessor(principalId),
        store);
    }

    private sealed class StubCurrentPrincipalAccessor : ICurrentPrincipalAccessor
    {
      private readonly PrincipalId? PrincipalId;

      public StubCurrentPrincipalAccessor(PrincipalId? principalId) => PrincipalId = principalId;

      public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
        Task.FromResult(PrincipalId);
    }
  }

} // namespace TimeWarp.Architecture.Features.Profiles
