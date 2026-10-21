#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj
#:project $(SourceDirectory)container-apps/web/projects/web-application/web-application.csproj
#:project $(TestsDirectory)common/timewarp-testing/timewarp-testing.csproj
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;CA2000;IDE0161;IDE0021;IDE0058;IDE0211;IDE0007;IDE0008

// Co-located Jaribu tests for the Ask configuration hint. No xAI key and no network.
// Run standalone: dotnet run source/container-apps/web/features/agent-chat/get-agent-chat-configuration/get-agent-chat-configuration-tests.cs

#region Purpose
// Proves the setup hint is the user-secrets command in Development and the XAI__ApiKey hint
// everywhere else, and that the configuration handler follows the host environment.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace TimeWarp.Architecture.Features.AgentChats
{

  using Microsoft.Extensions.FileProviders;
  using Microsoft.Extensions.Hosting;
  using Microsoft.Extensions.Options;
  using OneOf;
  using Shouldly;
  using TimeWarp.Architecture.Abstractions;
  using TimeWarp.Architecture.Features.AgentChats.Application;
  using TimeWarp.Foundation.Types;
  using TimeWarp.Identity;
  using TimeWarp.Jaribu;
  using static TimeWarp.Jaribu.TestRunner;
  using Handler = TimeWarp.Architecture.Features.AgentChats.Application.GetAgentChatConfiguration.Handler;

  [TestTag("Unit")]
  public class GetAgentChatConfigurationHint_Given_
  {
    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Register() => RegisterTests<GetAgentChatConfigurationHint_Given_>();

    public static Task Development_Hint_Is_The_User_Secrets_Command()
    {
      string command = XaiChatDefaults.SetupHint(development: true);
      command.ShouldBe(XaiChatDefaults.SetupCommand);
      command.ShouldNotContain("Admin");
      string withStorage = XaiChatDefaults.SetupHint(development: true, adminStorage: true);
      withStorage.ShouldBe(XaiChatDefaults.SetupCommand + " " + XaiChatDefaults.AdminAiSentence);
      return Task.CompletedTask;
    }

    public static Task Other_Environments_Name_The_Variable_And_Hide_The_User_Secrets_Id()
    {
      string hint = XaiChatDefaults.SetupHint(development: false);
      hint.ShouldBe(XaiChatDefaults.DeployedSetupHint);
      hint.ShouldContain("XAI__ApiKey");
      hint.ShouldNotContain("Admin");
      hint.ShouldNotContain(XaiChatDefaults.UserSecretsId);
      hint.ShouldNotContain("user-secrets");
      XaiChatDefaults.SetupHint(development: false, adminStorage: true).ShouldContain("Admin → AI");
      return Task.CompletedTask;
    }

    public static async Task Development_Handler_Returns_The_User_Secrets_Command()
    {
      GetAgentChatConfiguration.Response response = await Handle("Development", adminStorage: true);

      response.SetupCommand.ShouldBe(XaiChatDefaults.SetupCommand);
      response.SetupCommand.ShouldNotContain("Admin");
      response.AdminHint.ShouldBe(XaiChatDefaults.AdminAiSentence);

      GetAgentChatConfiguration.Response withoutStorage = await Handle("Development", adminStorage: false);
      withoutStorage.SetupCommand.ShouldBe(XaiChatDefaults.SetupCommand);
      withoutStorage.AdminHint.ShouldBeNull();
    }

    public static async Task Production_Handler_Does_Not_Name_User_Secrets()
    {
      await AssertDeployed("Production");
    }

    public static async Task Testing_Handler_Does_Not_Name_User_Secrets()
    {
      await AssertDeployed("Testing");
    }

    public static async Task Staging_Handler_Does_Not_Name_User_Secrets()
    {
      await AssertDeployed("Staging");
    }

    private static async Task AssertDeployed(string environmentName)
    {
      GetAgentChatConfiguration.Response response = await Handle(environmentName, adminStorage: true);
      response.SetupCommand.ShouldBe(XaiChatDefaults.DeployedSetupHint);
      response.SetupCommand.ShouldNotContain(XaiChatDefaults.UserSecretsId);
      response.SetupCommand.ShouldNotContain("user-secrets");
      response.SetupCommand.ShouldNotContain("Admin");
      response.SetupCommand.ShouldContain("XAI__ApiKey");
      response.AdminHint.ShouldBe(XaiChatDefaults.AdminAiSentence);
    }

    private static async Task<GetAgentChatConfiguration.Response> Handle(string environmentName, bool adminStorage)
    {
      Handler handler = new(
        new FixedPrincipal(),
        new UnconfiguredUpstream(),
        Options.Create(new XaiChatOptions()),
        new NamedHostEnvironment(environmentName),
        new FixedKeys(adminStorage));
      OneOf<GetAgentChatConfiguration.Response, SharedProblemDetails> result =
        await handler.Handle(new GetAgentChatConfiguration.Query(), CancellationToken.None);
      result.IsT0.ShouldBeTrue();
      return result.AsT0;
    }

    private sealed class FixedPrincipal : ICurrentPrincipalAccessor
    {
      public Task<PrincipalId?> GetCurrentPrincipalIdAsync(CancellationToken cancellationToken) =>
        Task.FromResult<PrincipalId?>(PrincipalId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")));
    }

    private sealed class FixedKeys(bool storageAvailable) : ISiteXaiApiKey
    {
      public bool StorageAvailable => storageAvailable;

      public Task<OneOf<SiteXaiApiKeyStatus, SharedProblemDetails>> GetStatusAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException();

      public Task<string?> ResolveApiKeyAsync(CancellationToken cancellationToken) =>
        throw new NotSupportedException();

      public Task<OneOf<SiteXaiApiKeyStatus, SharedProblemDetails>> SetAsync(
        string apiKey,
        Guid setByPrincipalId,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();

      public Task<OneOf<SiteXaiApiKeyStatus, SharedProblemDetails>> ClearAsync(
        Guid setByPrincipalId,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException();
    }

    private sealed class UnconfiguredUpstream : IAgentChatUpstream
    {
      public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken)
      {
        _ = cancellationToken;
        return Task.FromResult(false);
      }

      public string? Model => null;

      public Task<OneOf<CompleteAgentChat.Response, SharedProblemDetails>> CompleteAsync
      (
        CompleteAgentChat.Command command,
        CancellationToken cancellationToken
      )
      {
        _ = command;
        _ = cancellationToken;
        throw new NotSupportedException(nameof(CompleteAsync));
      }
    }

    private sealed class NamedHostEnvironment(string environmentName) : IHostEnvironment
    {
      public string EnvironmentName { get; set; } = environmentName;

      public string ApplicationName { get; set; } = "tests";

      public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

      public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
  }
}
