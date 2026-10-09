#region Purpose
// The server relay's fake upstream and declaration-only tools. No network and no API key.
#endregion

#region Design
// UseFakeUpstream is honored in Development and ignored in Production. The fake returns a
// page_context function call and, on the next turn, text that includes the tool result. Tool
// declarations are AIFunctionDeclaration, not AIFunction, so the server has no InvokeAsync.
#endregion

namespace XaiChatUpstream_;

using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Features.AgentChats.Application;
using static TimeWarp.Architecture.Features.AgentChats.CompleteAgentChat;

public class XaiChatUpstream_Given_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<XaiChatUpstream_Given_>();

  public static async Task Development_Fake_Returns_Page_Context_Then_The_Result()
  {
    await using ServiceProvider provider = Build("Development", useFake: true, apiKey: "should-not-be-sent");
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();
    upstream.IsConfigured.ShouldBeTrue();
    upstream.Model.ShouldBe(XaiChatDefaults.DefaultModel);

    OneOf<Response, SharedProblemDetails> first = await upstream.CompleteAsync(UserTurn(), CancellationToken.None);
    first.IsT0.ShouldBeTrue();
    first.AsT0.ToolCalls.Single().Name.ShouldBe("page_context");

    Command second = UserTurn();
    second.Messages.Add(new Turn
    {
      Role = "tool",
      ToolCallId = "call-1",
      ToolResult = "route=/Counter"
    });
    OneOf<Response, SharedProblemDetails> answered = await upstream.CompleteAsync(second, CancellationToken.None);
    answered.IsT0.ShouldBeTrue();
    answered.AsT0.Text.ShouldNotBeNull();
    answered.AsT0.Text!.ShouldContain("route=/Counter");
  }

  public static async Task Production_Ignores_The_Fake_And_A_Missing_Key()
  {
    await using ServiceProvider provider = Build("Production", useFake: true, apiKey: null);
    IAgentChatUpstream upstream = provider.GetRequiredService<IAgentChatUpstream>();
    upstream.IsConfigured.ShouldBeFalse();
    upstream.Model.ShouldBeNull();
  }

  public static Task Tool_Declarations_Are_Not_Invocable()
  {
    List<AITool> tools = XaiChatMapping.ToTools
    (
      [
        new ToolDefinition
        {
          Name = "page_context",
          Description = "Read the page",
          ParametersJson = """{"type":"object","properties":{}}"""
        }
      ]
    );

    AITool tool = tools.Single();
    AIFunctionDeclaration declaration = tool.ShouldBeAssignableTo<AIFunctionDeclaration>();
    declaration.ShouldNotBeAssignableTo<AIFunction>();
    declaration.Name.ShouldBe("page_context");
    declaration.JsonSchema.ValueKind.ShouldBe(JsonValueKind.Object);
    return Task.CompletedTask;
  }

  private static Command UserTurn() => new()
  {
    Messages = [new Turn { Role = "user", Text = "What is on this page?" }]
  };

  private static ServiceProvider Build(string environmentName, bool useFake, string? apiKey)
  {
    Dictionary<string, string?> values = new()
    {
      ["XAI:UseFakeUpstream"] = useFake ? "true" : "false"
    };
    if (apiKey is not null)
    {
      values["XAI:ApiKey"] = apiKey;
    }

    IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    ServiceCollection services = new();
    services.AddLogging();
    services.AddSingleton<IHostEnvironment>(new NamedEnvironment(environmentName));
    XaiChatRegistration.ConfigureServices(services, configuration);
    return services.BuildServiceProvider();
  }

  private sealed class NamedEnvironment : IHostEnvironment
  {
    public NamedEnvironment(string environmentName)
    {
      EnvironmentName = environmentName;
    }

    public string EnvironmentName { get; set; }

    public string ApplicationName { get; set; } = "tests";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
  }
}
