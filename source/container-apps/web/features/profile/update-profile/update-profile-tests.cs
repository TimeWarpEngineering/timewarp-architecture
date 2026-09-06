#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Co-located Jaribu tests for UpdateProfile (task 205): contract round-trip, validator rejection,
// create-if-missing, and named mutations. Never a register/session gate.
// Run standalone:  dotnet run source/container-apps/web/features/profile/update-profile/update-profile-tests.cs

#region Purpose
// Jaribu runfile: UpdateProfile contract + handler coverage (task 205 progressive profile).
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
  using FluentValidation.Results;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Architecture.Features.Profiles.Application;
  using TimeWarp.Architecture.Features.Profiles.Domain;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.Profiles.UpdateProfile;
  using UpdateHandler = TimeWarp.Architecture.Features.Profiles.Application.UpdateProfile.Handler;
  using DomainProfile = TimeWarp.Architecture.Features.Profiles.Domain.Profile;

  [TestTag("Contracts")]
  public class UpdateProfileCommand_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<UpdateProfileCommand_Given_>();

    public static Task ValidCommand_Should_RoundTripThroughJson()
    {
      Command command = new()
      {
        Alias = "Ada",
        Email = "ada@example.com",
        Language = "en-US",
        Region = "US",
        Theme = "dark",
        Notifications = true
      };

      string json = JsonSerializer.Serialize(command, ContractSerializationDefaults.Options);
      Command? parsed = JsonSerializer.Deserialize<Command>(json, ContractSerializationDefaults.Options);

      parsed.ShouldNotBeNull();
      parsed.Alias.ShouldBe("Ada");
      parsed.Email.ShouldBe("ada@example.com");
      parsed.Language.ShouldBe("en-US");
      parsed.Region.ShouldBe("US");
      parsed.Theme.ShouldBe("dark");
      parsed.Notifications.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task EmptyAlias_Should_FailValidation()
    {
      Command command = new()
      {
        Alias = "",
        Language = "en-US",
        Region = "US",
        Theme = "system"
      };

      ValidationResult result = new Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task InvalidEmail_Should_FailValidation()
    {
      Command command = new()
      {
        Alias = "Ada",
        Email = "not-an-email",
        Language = "en-US",
        Region = "US",
        Theme = "system"
      };

      ValidationResult result = new Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task MissingEmail_Should_PassValidation()
    {
      Command command = new()
      {
        Alias = "Ada",
        Email = null,
        Language = "en-US",
        Region = "US",
        Theme = "system"
      };

      ValidationResult result = new Validator().Validate(command);
      result.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }
  }

  [TestTag("Handler")]
  public class UpdateProfileHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<UpdateProfileHandler_Given_>();

    public static async Task Anonymous_Should_Return401()
    {
      InMemoryProfileStore store = new();
      UpdateHandler handler = CreateHandler(userId: null, store);

      OneOf<Response, SharedProblemDetails> result =
        await handler.Handle(ValidCommand(), CancellationToken.None);

      SharedProblemDetails problem = result.Match(
        _ => throw new InvalidOperationException("Expected problem"),
        problemDetails => problemDetails);

      problem.Status.ShouldBe(401);
    }

    public static async Task Authenticated_missing_profile_Should_CreateAndPersist()
    {
      Guid userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
      InMemoryProfileStore store = new();
      UpdateHandler handler = CreateHandler(userId, store);

      OneOf<Response, SharedProblemDetails> result =
        await handler.Handle(ValidCommand(), CancellationToken.None);

      result.IsT0.ShouldBeTrue();
      DomainProfile? stored = await store.FindAsync(ProfileId.From(userId));
      stored.ShouldNotBeNull();
      stored.DisplayName.ShouldBe("Ada");
      stored.Email.ShouldBe("ada@example.com");
      stored.Language.ShouldBe("en-US");
      stored.Theme.ShouldBe("dark");
      stored.Notifications.ShouldBeTrue();
    }

    public static async Task Authenticated_existing_profile_Should_Mutate()
    {
      Guid userId = Guid.Parse("bbbbbbbb-cccc-dddd-eeee-ffffffffffff");
      ProfileId id = ProfileId.From(userId);
      InMemoryProfileStore store = new();
      await store.AddAsync(DomainProfile.Create(id, "Member", "en-US", "US", "system"));

      UpdateHandler handler = CreateHandler(userId, store);
      (await handler.Handle(ValidCommand(), CancellationToken.None)).IsT0.ShouldBeTrue();

      DomainProfile? stored = await store.FindAsync(id);
      stored.ShouldNotBeNull();
      stored.DisplayName.ShouldBe("Ada");
      stored.Email.ShouldBe("ada@example.com");
      stored.Theme.ShouldBe("dark");
    }

    public static async Task Clearing_email_Should_SetNull()
    {
      Guid userId = Guid.Parse("cccccccc-dddd-eeee-ffff-000000000001");
      ProfileId id = ProfileId.From(userId);
      InMemoryProfileStore store = new();
      DomainProfile seeded = DomainProfile.Create(id, "Ada", "en-US", "US", "system");
      seeded.SetEmail("ada@example.com");
      await store.AddAsync(seeded);

      Command command = ValidCommand();
      command.Email = null;
      UpdateHandler handler = CreateHandler(userId, store);
      (await handler.Handle(command, CancellationToken.None)).IsT0.ShouldBeTrue();

      DomainProfile? stored = await store.FindAsync(id);
      stored.ShouldNotBeNull();
      stored.Email.ShouldBeNull();
    }

    private static Command ValidCommand() => new()
    {
      Alias = "Ada",
      Email = "ada@example.com",
      Language = "en-US",
      Region = "US",
      Theme = "dark",
      Notifications = true
    };

    private static UpdateHandler CreateHandler(Guid? userId, IProfileStore store)
    {
      PrincipalId? principalId = userId is null ? null : PrincipalId.From(userId.Value);
      return new UpdateHandler(
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
