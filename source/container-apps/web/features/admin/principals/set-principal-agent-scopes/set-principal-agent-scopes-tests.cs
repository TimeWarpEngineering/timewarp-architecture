#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007;IDE0008

// Co-located Jaribu: SetPrincipalAgentScopes contract validation (task 466).

#region Purpose
// Host-free validation for SetPrincipalAgentScopes: known scopes pass, an unknown scope
// fails, and an empty list is a legal clear.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Admin.Principals
{

  using System.Threading.Tasks;
  using FluentValidation.Results;
  using Shouldly;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Contracts")]
  public class SetPrincipalAgentScopesCommand_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<SetPrincipalAgentScopesCommand_Given_>();

    public static Task KnownScope_Should_PassValidation()
    {
      SetPrincipalAgentScopes.Command command = new()
      {
        UserId = Guid.NewGuid(),
        PrincipalId = Guid.NewGuid(),
        Scopes = [AgentScopes.DemoInvoke],
      };

      ValidationResult result = new SetPrincipalAgentScopes.Validator().Validate(command);
      result.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task EmptyScopes_Should_PassValidation()
    {
      SetPrincipalAgentScopes.Command command = new()
      {
        UserId = Guid.NewGuid(),
        PrincipalId = Guid.NewGuid(),
        Scopes = [],
      };

      ValidationResult result = new SetPrincipalAgentScopes.Validator().Validate(command);
      result.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task UnknownScope_Should_FailValidation()
    {
      SetPrincipalAgentScopes.Command command = new()
      {
        UserId = Guid.NewGuid(),
        PrincipalId = Guid.NewGuid(),
        Scopes = ["not-a-scope"],
      };

      ValidationResult result = new SetPrincipalAgentScopes.Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }
  }
}
