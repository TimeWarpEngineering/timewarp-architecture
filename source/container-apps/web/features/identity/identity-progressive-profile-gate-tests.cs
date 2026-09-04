#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058;IDE0007;IDE0008

// Pins locked 104 decision 1 / task 205: passkey register, agent-key register, token issuance,
// and metered (paid) capability do not take IProfileStore or IAgentHumanLinkStore.
// Run standalone:  dotnet run source/container-apps/web/features/identity/identity-progressive-profile-gate-tests.cs

#region Purpose
// Jaribu runfile: progressive profile and agent-human link are never register/session/payment gates.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Task205
{

  using System;
  using System.Linq;
  using System.Reflection;
  using System.Threading.Tasks;
  using Shouldly;
  using TimeWarp.Architecture.Features.AgentLinks.Application;
  using TimeWarp.Architecture.Features.Identity.Application;
  using TimeWarp.Architecture.Features.MeteredCapability.Application;
  using TimeWarp.Architecture.Features.Profiles.Application;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Application")]
  public class ProgressiveProfile_Is_Not_A_Gate_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<ProgressiveProfile_Is_Not_A_Gate_Given_>();

    public static Task PasskeyRegistration_Should_Not_Depend_On_ProfileStore()
    {
      AssertHandlerDoesNotTake(
        typeof(CompletePasskeyRegistration.Handler),
        typeof(IProfileStore));
      return Task.CompletedTask;
    }

    public static Task AgentKeyRegistration_Should_Not_Depend_On_ProfileStore()
    {
      AssertHandlerDoesNotTake(
        typeof(CompleteAgentKeyRegistration.Handler),
        typeof(IProfileStore));
      return Task.CompletedTask;
    }

    public static Task TokenIssuance_Should_Not_Depend_On_ProfileStore()
    {
      AssertHandlerDoesNotTake(
        typeof(CompleteAgentTokenIssuance.Handler),
        typeof(IProfileStore));
      return Task.CompletedTask;
    }

    public static Task MeteredCapability_Should_Not_Depend_On_ProfileOrLinkStore()
    {
      Type handler = typeof(InvokeMeteredCapability.Handler);
      AssertHandlerDoesNotTake(handler, typeof(IProfileStore));
      AssertHandlerDoesNotTake(handler, typeof(IAgentHumanLinkStore));
      return Task.CompletedTask;
    }

    private static void AssertHandlerDoesNotTake(Type handlerType, Type forbidden)
    {
      ConstructorInfo constructor = handlerType.GetConstructors().ShouldHaveSingleItem();
      constructor.GetParameters()
        .Select(parameter => parameter.ParameterType)
        .ShouldNotContain(forbidden);
    }
  }

} // namespace TimeWarp.Architecture.Task205
