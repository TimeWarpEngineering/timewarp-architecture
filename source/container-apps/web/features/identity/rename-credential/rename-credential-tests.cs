#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0005;IDE0007;IDE0008

// Co-located Jaribu: RenameCredential validator + handler (happy path, rejection, IDOR 404 shape).
// Run standalone:  dotnet run source/container-apps/web/features/identity/rename-credential/rename-credential-tests.cs

#region Purpose
// Jaribu runfile: RenameCredential renames only the caller's own credential; whitespace / oversize
// nicknames are rejected by the validator; another principal's id and an unknown id are the same 404.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Identity
{

  using System.Threading.Tasks;
  using FluentValidation.Results;
  using Shouldly;
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Architecture.Features.Identity.Application;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using static TimeWarp.Architecture.Features.Identity.RenameCredential;
  using RenameHandler = TimeWarp.Architecture.Features.Identity.Application.RenameCredential.Handler;

  [TestTag("Validation")]
  public class RenameCredentialValidator_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<RenameCredentialValidator_Given_>();

    public static Task Trimmed_Nickname_Within_Limit_Should_Pass()
    {
      ValidationResult result = new Validator().Validate(Command("  Work laptop  "));
      result.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task Whitespace_Nickname_Should_Fail()
    {
      new Validator().Validate(Command("   ")).IsValid.ShouldBeFalse();
      new Validator().Validate(Command("")).IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task Oversize_Nickname_Should_Fail()
    {
      new Validator().Validate(Command(new string('x', Credential.MaxNicknameLength + 1))).IsValid.ShouldBeFalse();
      new Validator().Validate(Command(new string('x', Credential.MaxNicknameLength))).IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task Empty_CredentialId_Should_Fail()
    {
      Command command = Command("ok");
      command.CredentialId = Guid.Empty;
      new Validator().Validate(command).IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }

    private static Command Command(string nickname) =>
      new() { UserId = Guid.NewGuid(), CredentialId = Guid.NewGuid(), Nickname = nickname };
  }

  [TestTag("Handler")]
  public class RenameCredentialHandler_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<RenameCredentialHandler_Given_>();

    public static async Task Own_Credential_Should_Rename_And_Persist()
    {
      (InMemoryPrincipalStore store, PrincipalId owner, Credential credential) = await SeedAsync();
      RenameHandler handler = new(store, new StubCurrentPrincipalAccessor(owner));

      OneOf.OneOf<Response, SharedProblemDetails> result =
        await handler.Handle(new Command { UserId = Guid.NewGuid(), CredentialId = credential.Id.Value, Nickname = "  Work laptop " }, CancellationToken.None);

      result.IsT0.ShouldBeTrue();
      Credential? stored = await store.GetCredentialAsync(credential.Id);
      stored.ShouldNotBeNull();
      stored.Nickname.ShouldBe("Work laptop");
      stored.Label.ShouldBe("Proton Pass");
      stored.Version.ShouldBe(credential.Version + 1);
    }

    public static async Task Another_Principals_Credential_Should_Return_404()
    {
      (InMemoryPrincipalStore store, PrincipalId _, Credential credential) = await SeedAsync();
      RenameHandler handler = new(store, new StubCurrentPrincipalAccessor(PrincipalId.New()));

      SharedProblemDetails problem = (await handler.Handle(
          new Command { UserId = Guid.NewGuid(), CredentialId = credential.Id.Value, Nickname = "hijack" }, CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected not-found problem"), p => p);

      problem.Status.ShouldBe(404);
      (await store.GetCredentialAsync(credential.Id))!.Nickname.ShouldBeNull();
    }

    public static async Task Unknown_CredentialId_Should_Return_The_Same_404()
    {
      (InMemoryPrincipalStore store, PrincipalId owner, Credential _) = await SeedAsync();
      RenameHandler handler = new(store, new StubCurrentPrincipalAccessor(owner));

      SharedProblemDetails problem = (await handler.Handle(
          new Command { UserId = Guid.NewGuid(), CredentialId = Guid.NewGuid(), Nickname = "ghost" }, CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected not-found problem"), p => p);

      problem.Status.ShouldBe(404);
      problem.Title.ShouldBe(IdentityProblems.NotFound().Title);
    }

    public static async Task Unauthenticated_Should_Return_401()
    {
      (InMemoryPrincipalStore store, PrincipalId _, Credential credential) = await SeedAsync();
      RenameHandler handler = new(store, new StubCurrentPrincipalAccessor(null));

      SharedProblemDetails problem = (await handler.Handle(
          new Command { UserId = Guid.NewGuid(), CredentialId = credential.Id.Value, Nickname = "x" }, CancellationToken.None))
        .Match(_ => throw new InvalidOperationException("Expected unauthenticated problem"), p => p);

      problem.Status.ShouldBe(401);
    }

    public static async Task Revoked_Credential_Should_Still_Rename()
    {
      (InMemoryPrincipalStore store, PrincipalId owner, Credential credential) = await SeedAsync();
      Credential toRevoke = (await store.GetCredentialAsync(credential.Id))!;
      toRevoke.Revoke();
      await store.UpdateCredentialAsync(toRevoke);
      RenameHandler handler = new(store, new StubCurrentPrincipalAccessor(owner));

      OneOf.OneOf<Response, SharedProblemDetails> result =
        await handler.Handle(new Command { UserId = Guid.NewGuid(), CredentialId = credential.Id.Value, Nickname = "Old phone" }, CancellationToken.None);

      result.IsT0.ShouldBeTrue();
      Credential stored = (await store.GetCredentialAsync(credential.Id))!;
      stored.Nickname.ShouldBe("Old phone");
      stored.IsRevoked.ShouldBeTrue();
    }

    private static async Task<(InMemoryPrincipalStore Store, PrincipalId Owner, Credential Credential)> SeedAsync()
    {
      InMemoryPrincipalStore store = new();
      Principal principal = Principal.Create(PrincipalKind.Human);
      await store.AddPrincipalAsync(principal);
      Credential credential = Credential.Create(principal.Id, CredentialType.Passkey, [1, 2, 3], [4, 5, 6], "Proton Pass");
      await store.AddCredentialAsync(credential);
      return (store, principal.Id, credential);
    }

    private sealed class StubCurrentPrincipalAccessor : ICurrentPrincipalAccessor
    {
      private readonly PrincipalId? PrincipalId;

      public StubCurrentPrincipalAccessor(PrincipalId? principalId) => PrincipalId = principalId;

      public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
        Task.FromResult(PrincipalId);
    }
  }
}
