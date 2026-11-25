#!/usr/bin/env -S dotnet --
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false

// Run: dotnet run tests/common/timewarp-testing-tests/validator-options-guard-tests.cs

#region Purpose
// Prove WebApplicationHost leaves FluentValidation's static ValidatorOptions.Global as it found it
// (task 526): after a real Api.Server host starts and is disposed, or fails to start, both resolvers
// are back and a plain validator reports PascalCase PropertyName again.
#endregion

#region Design
// Tasks 520 and 521 failed web-jaribu-tests in CI only: a host test earlier in the aggregator
// process left FastEndpoints' camelCase PropertyNameResolver behind, so a later validator test saw
// "pagePath" for PagePath. The host tests here boot the real Api.Server (FastEndpoints +
// CommonServerModule), assert the leak is visible while the host lives (so the restore proof is
// not vacuous), then assert it is gone after dispose. The startup-failure test throws from the
// per-host configureServices hook, after Api.Server's ConfigureServices already changed the
// DisplayNameResolver, so the constructor's catch path must restore it. The guard test pins the
// ref-count: hosts alive together share one snapshot, and the last Exit restores it.
// Lives in timewarp-testing-tests (dual-mode, JARIBU_MULTI) beside the HostGraphFactory smoke; it
// binds the Api port from InProcTestPorts like that smoke does.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Testing.ValidatorOptionsGuardTests
{

  using System;
  using System.Linq.Expressions;
  using System.Reflection;
  using System.Threading.Tasks;
  using FluentValidation;
  using FluentValidation.Results;
  using Shouldly;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  internal sealed class Sample
  {
    public string? PagePath { get; set; }
  }

  internal static class SampleValidation
  {
    internal static string FirstPropertyName()
    {
      InlineValidator<Sample> validator = new();
      validator.RuleFor(sample => sample.PagePath).NotEmpty();
      ValidationResult result = validator.Validate(new Sample());
      result.IsValid.ShouldBeFalse();
      return result.Errors[0].PropertyName;
    }
  }

  public class ValidatorOptionsGuard_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<ValidatorOptionsGuard_Given_>();

    private static readonly Func<Type, MemberInfo, LambdaExpression, string> Sentinel = (_, _, _) => "sentinel";

    public static Task NestedEnter_Should_RestoreOnlyOnLastExit()
    {
      Func<Type, MemberInfo, LambdaExpression, string> propertyBefore = ValidatorOptions.Global.PropertyNameResolver;
      Func<Type, MemberInfo, LambdaExpression, string> displayBefore = ValidatorOptions.Global.DisplayNameResolver;

      ValidatorOptionsGuard.Enter();
      ValidatorOptionsGuard.Enter();
      ValidatorOptions.Global.PropertyNameResolver = Sentinel;
      ValidatorOptions.Global.DisplayNameResolver = Sentinel;

      ValidatorOptionsGuard.Exit();
      ValidatorOptions.Global.PropertyNameResolver.ShouldBeSameAs(Sentinel);

      ValidatorOptionsGuard.Exit();
      ValidatorOptions.Global.PropertyNameResolver.ShouldBeSameAs(propertyBefore);
      ValidatorOptions.Global.DisplayNameResolver.ShouldBeSameAs(displayBefore);
      ValidatorOptionsGuard.Active.ShouldBe(0);
      return Task.CompletedTask;
    }

    public static Task UnmatchedExit_Should_Throw()
    {
      ValidatorOptionsGuard.Active.ShouldBe(0);
      Should.Throw<InvalidOperationException>(ValidatorOptionsGuard.Exit);
      return Task.CompletedTask;
    }
  }

  [TestTag("Integration")]
  public class WebApplicationHost_ValidatorOptions_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<WebApplicationHost_ValidatorOptions_Given_>();

    public static async Task DisposedHost_Should_RestoreResolvers()
    {
      Func<Type, MemberInfo, LambdaExpression, string> propertyBefore = ValidatorOptions.Global.PropertyNameResolver;
      Func<Type, MemberInfo, LambdaExpression, string> displayBefore = ValidatorOptions.Global.DisplayNameResolver;
      SampleValidation.FirstPropertyName().ShouldBe(nameof(Sample.PagePath));

      ApiTestServerApplication api = new();
      try
      {
        // The leak this guards against is real while the host lives.
        ValidatorOptions.Global.DisplayNameResolver.ShouldNotBeSameAs(displayBefore);
        SampleValidation.FirstPropertyName().ShouldBe("pagePath");
      }
      finally
      {
        await api.DisposeAsync();
      }

      ValidatorOptions.Global.PropertyNameResolver.ShouldBeSameAs(propertyBefore);
      ValidatorOptions.Global.DisplayNameResolver.ShouldBeSameAs(displayBefore);
      SampleValidation.FirstPropertyName().ShouldBe(nameof(Sample.PagePath));
      ValidatorOptionsGuard.Active.ShouldBe(0);
    }

    public static Task FailedStartup_Should_RestoreResolvers()
    {
      Func<Type, MemberInfo, LambdaExpression, string> propertyBefore = ValidatorOptions.Global.PropertyNameResolver;
      Func<Type, MemberInfo, LambdaExpression, string> displayBefore = ValidatorOptions.Global.DisplayNameResolver;

      Should.Throw<InvalidOperationException>(
        () => new ApiTestServerApplication(_ => throw new InvalidOperationException("startup failure")));

      ValidatorOptions.Global.PropertyNameResolver.ShouldBeSameAs(propertyBefore);
      ValidatorOptions.Global.DisplayNameResolver.ShouldBeSameAs(displayBefore);
      SampleValidation.FirstPropertyName().ShouldBe(nameof(Sample.PagePath));
      ValidatorOptionsGuard.Active.ShouldBe(0);
      return Task.CompletedTask;
    }
  }

} // namespace TimeWarp.Architecture.Testing.ValidatorOptionsGuardTests
