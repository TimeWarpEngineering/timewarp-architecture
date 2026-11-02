#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007;IDE0008

// Co-located Jaribu: SetRoleAgentScopes contract validation (task 466).

#region Purpose
// Host-free validation for SetRoleAgentScopes: known scopes pass, an unknown scope fails,
// and an empty list is a legal clear.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Admin.Roles
{

  using System.Threading.Tasks;
  using FluentValidation.Results;
  using Shouldly;
  using TimeWarp.Architecture.Features;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Contracts")]
  public class SetRoleAgentScopesCommand_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<SetRoleAgentScopesCommand_Given_>();

    public static Task KnownScopes_Should_PassValidation()
    {
      SetRoleAgentScopes.Command command = new()
      {
        UserId = Guid.NewGuid(),
        RoleId = RoleIds.Developer,
        Scopes = [AgentScopes.FeedbackRead, AgentScopes.IdentityRead],
      };

      ValidationResult result = new SetRoleAgentScopes.Validator().Validate(command);
      result.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task EmptyScopes_Should_PassValidation()
    {
      SetRoleAgentScopes.Command command = new()
      {
        UserId = Guid.NewGuid(),
        RoleId = RoleIds.Member,
        Scopes = [],
      };

      ValidationResult result = new SetRoleAgentScopes.Validator().Validate(command);
      result.IsValid.ShouldBeTrue();
      return Task.CompletedTask;
    }

    public static Task UnknownScope_Should_FailValidation()
    {
      SetRoleAgentScopes.Command command = new()
      {
        UserId = Guid.NewGuid(),
        RoleId = RoleIds.Member,
        Scopes = ["not-a-scope"],
      };

      ValidationResult result = new SetRoleAgentScopes.Validator().Validate(command);
      result.IsValid.ShouldBeFalse();
      return Task.CompletedTask;
    }
  }
}
