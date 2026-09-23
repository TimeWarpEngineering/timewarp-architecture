#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0005;IDE0007;IDE0008

// Co-located Jaribu: RegistrationContext attachment precedence and User-Agent family classification.
// Run standalone:  dotnet run source/container-apps/web/features/identity/registration-context-tests.cs

#region Purpose
// Jaribu runfile: attachment comes from authenticatorAttachment first, transports second, else
// Unknown; User-Agent reduces to short browser/OS families and never to the raw string.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.Identity
{

  using System.Threading.Tasks;
  using Shouldly;
  using TimeWarp.Architecture.Features.Identity.Application;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;

  [TestTag("Application")]
  public class RegistrationContextAttachment_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<RegistrationContextAttachment_Given_>();

    public static Task Browser_Attachment_Should_Win_Over_Transports()
    {
      RegistrationContext.ResolveAttachment("platform", ["usb"]).ShouldBe(AuthenticatorAttachment.Platform);
      RegistrationContext.ResolveAttachment("Cross-Platform", ["internal"]).ShouldBe(AuthenticatorAttachment.CrossPlatform);
      return Task.CompletedTask;
    }

    public static Task Transports_Should_Decide_When_Attachment_Missing()
    {
      RegistrationContext.ResolveAttachment(null, ["internal", "hybrid"]).ShouldBe(AuthenticatorAttachment.Platform);
      RegistrationContext.ResolveAttachment(null, ["hybrid"]).ShouldBe(AuthenticatorAttachment.CrossPlatform);
      RegistrationContext.ResolveAttachment(null, ["usb", "nfc"]).ShouldBe(AuthenticatorAttachment.CrossPlatform);
      RegistrationContext.ResolveAttachment(null, ["ble"]).ShouldBe(AuthenticatorAttachment.CrossPlatform);
      return Task.CompletedTask;
    }

    public static Task Nothing_Usable_Should_Be_Unknown()
    {
      RegistrationContext.ResolveAttachment(null, null).ShouldBe(AuthenticatorAttachment.Unknown);
      RegistrationContext.ResolveAttachment("", []).ShouldBe(AuthenticatorAttachment.Unknown);
      RegistrationContext.ResolveAttachment("bogus", ["teleport"]).ShouldBe(AuthenticatorAttachment.Unknown);
      return Task.CompletedTask;
    }
  }

  [TestTag("Application")]
  public class RegistrationContextUserAgent_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<RegistrationContextUserAgent_Given_>();

    public static Task Chrome_On_Windows()
    {
      RegistrationContext.ParseUserAgent(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36")
        .ShouldBe(("Chrome", "Windows"));
      return Task.CompletedTask;
    }

    public static Task Edge_Is_Not_Chrome()
    {
      RegistrationContext.ParseUserAgent(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 Edg/128.0.0.0")
        .ShouldBe(("Edge", "Windows"));
      return Task.CompletedTask;
    }

    public static Task Safari_On_iPhone_And_macOS()
    {
      RegistrationContext.ParseUserAgent(
        "Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1")
        .ShouldBe(("Safari", "iOS"));
      RegistrationContext.ParseUserAgent(
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_5) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Safari/605.1.15")
        .ShouldBe(("Safari", "macOS"));
      return Task.CompletedTask;
    }

    public static Task Firefox_On_Linux_And_Chrome_On_Android()
    {
      RegistrationContext.ParseUserAgent("Mozilla/5.0 (X11; Linux x86_64; rv:129.0) Gecko/20100101 Firefox/129.0")
        .ShouldBe(("Firefox", "Linux"));
      RegistrationContext.ParseUserAgent(
        "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Mobile Safari/537.36")
        .ShouldBe(("Chrome", "Android"));
      return Task.CompletedTask;
    }

    public static Task Unrecognized_Or_Missing_Should_Be_Null_Not_A_Guess()
    {
      RegistrationContext.ParseUserAgent(null).ShouldBe((null, null));
      RegistrationContext.ParseUserAgent("   ").ShouldBe((null, null));
      RegistrationContext.ParseUserAgent("curl/8.7.1").ShouldBe((null, null));
      RegistrationContext.ParseUserAgent("ganda-agent/1.0 (Windows)").ShouldBe((null, "Windows"));
      return Task.CompletedTask;
    }

    public static Task Resolve_Should_Never_Store_The_Raw_UserAgent_And_Caps_Length()
    {
      string hostile = new string('A', RegistrationContext.MaxUserAgentLength * 4) + " Chrome/1 Windows";
      RegisteredWith context = RegistrationContext.Resolve("platform", null, hostile);

      context.Attachment.ShouldBe(AuthenticatorAttachment.Platform);
      context.Browser.ShouldBeNull();
      context.Os.ShouldBeNull();
      RegistrationContext.ResolveForAgentKey(null).ShouldBe(RegisteredWith.Unknown);
      RegistrationContext.ResolveForAgentKey("Mozilla/5.0 (Windows NT 10.0) Chrome/128.0 Safari/537.36")
        .ShouldBe(new RegisteredWith(AuthenticatorAttachment.Unknown, "Chrome", "Windows"));
      return Task.CompletedTask;
    }
  }
}
