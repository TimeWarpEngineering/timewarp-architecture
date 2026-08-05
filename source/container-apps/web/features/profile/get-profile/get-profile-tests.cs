#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Co-located Jaribu tests for GetProfile (task 148 D10): contract round-trip, mock factory,
// in-memory store, and handler create-if-missing with stub HTTP (avatar fallback).
// Run standalone:  dotnet run source/container-apps/web/features/profile/get-profile/get-profile-tests.cs

#region Purpose
// Jaribu runfile: GetProfile contract + store + handler coverage (task 148).
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Profiles
{

  using System;
  using System.Net.Http;
  using System.Text.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using Microsoft.Extensions.Logging.Abstractions;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Features.Profiles.Application;
  using TimeWarp.Architecture.Features.Profiles.Domain;
  using TimeWarp.Foundation.Abstractions;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.Profiles.GetProfile;
  using ProfileHandler = TimeWarp.Architecture.Features.Profiles.Application.GetProfile.Handler;

  [TestTag("Contracts")]
  public class GetProfileResponse_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetProfileResponse_Given_>();

    public static Task ValidResponse_Should_RoundTripThroughJson()
    {
      var response = new Response(
        alias: "Ada",
        language: "en-US",
        region: "US",
        theme: "system",
        notifications: true,
        avatar: "data:image/svg+xml;base64,abc");

      string json = JsonSerializer.Serialize(response, ContractSerializationDefaults.Options);
      Response? parsed = JsonSerializer.Deserialize<Response>(json, ContractSerializationDefaults.Options);

      parsed.ShouldNotBeNull();
      parsed.Alias.ShouldBe("Ada");
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
      response.Language.ShouldBe("en-US");
      response.Region.ShouldBe("US");
      response.Theme.ShouldBe("system");
      response.Notifications.ShouldBeFalse();
      response.Avatar.ShouldNotBeNullOrEmpty();
      return Task.CompletedTask;
    }

    public static Task EmptyAliasJson_Should_ThrowDuringDeserialization()
    {
      string json =
        """{"alias":"","language":"en-US","region":"US","theme":"system","notifications":false,"avatar":"x"}""";

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
      var store = new InMemoryProfileStore();
      Profile? found = await store.FindAsync(ProfileId.From(Guid.NewGuid()));
      found.ShouldBeNull();
    }

    public static async Task Add_then_Find_Should_ReturnSameId()
    {
      var store = new InMemoryProfileStore();
      var id = ProfileId.From(Guid.Parse("11111111-2222-3333-4444-555555555555"));
      Profile profile = Profile.Create(id, "Member", "en-US", "US", "system");

      await store.AddAsync(profile);
      Profile? found = await store.FindAsync(id);

      found.ShouldNotBeNull();
      found.Id.ShouldBe(id);
      found.DisplayName.ShouldBe("Member");
    }

    public static async Task Add_duplicate_Should_Throw()
    {
      var store = new InMemoryProfileStore();
      var id = ProfileId.From(Guid.NewGuid());
      await store.AddAsync(Profile.Create(id, "A", "en-US", "US", "system"));

      await Should.ThrowAsync<InvalidOperationException>(async () =>
        await store.AddAsync(Profile.Create(id, "B", "en-US", "US", "system")));
    }
  }

  [TestTag("Handler")]
  public class GetProfileHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetProfileHandler_Given_>();

    public static async Task Anonymous_Should_ReturnMock()
    {
      var store = new InMemoryProfileStore();
      using var avatarHandler = new FailingAvatarHandler();
      using var httpClient = new HttpClient(avatarHandler);
      var handler = CreateHandler(userId: null, store, httpClient);

      OneOf<Response, SharedProblemDetails> result =
        await handler.Handle(new Query(), CancellationToken.None);

      Response response = result.Match(
        ok => ok,
        _ => throw new InvalidOperationException("Expected Response"));

      response.Alias.ShouldBe("alias");
      response.Language.ShouldBe("en-US");
    }

    public static async Task Authenticated_missing_profile_Should_CreateDefaults()
    {
      var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
      var store = new InMemoryProfileStore();
      using var avatarHandler = new FailingAvatarHandler();
      using var httpClient = new HttpClient(avatarHandler);
      var handler = CreateHandler(userId, store, httpClient);

      OneOf<Response, SharedProblemDetails> result =
        await handler.Handle(new Query(), CancellationToken.None);

      Response response = result.Match(
        ok => ok,
        _ => throw new InvalidOperationException("Expected Response"));

      response.Alias.ShouldBe("Member");
      response.Language.ShouldBe("en-US");
      response.Region.ShouldBe("US");
      response.Theme.ShouldBe("system");
      response.Notifications.ShouldBeFalse();
      response.Avatar.ShouldNotBeNullOrEmpty();
      response.Avatar.ShouldStartWith("data:image/svg+xml;base64,");

      Profile? stored = await store.FindAsync(ProfileId.From(userId));
      stored.ShouldNotBeNull();
      stored.DisplayName.ShouldBe("Member");
    }

    public static async Task Authenticated_existing_profile_Should_ReturnStoredFields()
    {
      var userId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
      var id = ProfileId.From(userId);
      var store = new InMemoryProfileStore();
      Profile seeded = Profile.Create(id, "Grace", "fr-FR", "FR", "dark");
      seeded.EnableNotifications();
      await store.AddAsync(seeded);

      using var avatarHandler = new FailingAvatarHandler();
      using var httpClient = new HttpClient(avatarHandler);
      var handler = CreateHandler(userId, store, httpClient);
      Response response = (await handler.Handle(new Query(), CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected Response"));

      response.Alias.ShouldBe("Grace");
      response.Language.ShouldBe("fr-FR");
      response.Region.ShouldBe("FR");
      response.Theme.ShouldBe("dark");
      response.Notifications.ShouldBeTrue();
    }

    public static async Task Avatar_network_failure_Should_UseFallback_not_throw()
    {
      var userId = Guid.NewGuid();
      var store = new InMemoryProfileStore();
      using var avatarHandler = new FailingAvatarHandler();
      using var httpClient = new HttpClient(avatarHandler);
      var handler = CreateHandler(userId, store, httpClient);

      Response response = (await handler.Handle(new Query(), CancellationToken.None))
        .Match(ok => ok, _ => throw new InvalidOperationException("Expected Response"));

      // Multiavatar failed → resilient SVG data URI (never throws).
      response.Avatar.ShouldNotBeNullOrEmpty();
      response.Avatar.ShouldStartWith("data:image/svg+xml;base64,");
    }

    private static ProfileHandler CreateHandler(
      Guid? userId,
      IProfileStore store,
      HttpClient httpClient)
    {
      return new ProfileHandler(
        new StubCurrentUserService(userId),
        store,
        httpClient,
        NullLogger<ProfileHandler>.Instance);
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
      public StubCurrentUserService(Guid? userId) => UserId = userId;
      public Guid? UserId { get; }
    }

    /// <summary>Always fails multiavatar so tests exercise the resilient fallback path.</summary>
    private sealed class FailingAvatarHandler : HttpMessageHandler
    {
      protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
      {
        throw new HttpRequestException("multiavatar unreachable (test stub)");
      }
    }
  }

} // namespace TimeWarp.Architecture.Features.Profiles
