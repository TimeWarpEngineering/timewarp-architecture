#region Purpose
// Instruction cap, profile and site-settings page context, JSON argument rendering, relay client, and observed route.
#endregion

namespace CatalogAgent_;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Features.Feedback;
using TimeWarp.Architecture.Features.Settings;

[TestTag("Integration")]
public partial class CatalogAgent_Should
{
  public static Task Instructions_Stay_Under_The_Contract_Cap()
  {
    string text = AgentAskInstructions.For("Akili", new string('x', 20_000));
    text.Length.ShouldBeLessThanOrEqualTo(AgentAskInstructions.MaxLength);
    text.Length.ShouldBeLessThan(CompleteAgentChat.MaxInstructionsLength);
    text.ShouldStartWith("You are Akili, the assistant built into this app.");
    text.ShouldContain("You can answer about, and move to, any page in the site map");
    AgentAskInstructions.Preface.ShouldNotContain("Drive this page only");
    AgentAskInstructions.Preface.ShouldNotContain("this page only");

    // With a full site map the page copy gets what is left, and the whole stays under the cap.
    string siteMap = new('m', AgentAskInstructions.SiteMapCap);
    int cap = AgentAskInstructions.ContextCapFor("Akili", siteMap);
    cap.ShouldBeGreaterThanOrEqualTo(AgentAskInstructions.MinContextCap);
    string mapped = AgentAskInstructions.For("Akili", siteMap, new string('p', cap));
    mapped.Length.ShouldBeLessThanOrEqualTo(AgentAskInstructions.MaxLength);
    mapped.ShouldContain(AgentAskInstructions.SiteMapHeading);
    mapped.ShouldEndWith(new string('p', 10));
    return Task.CompletedTask;
  }

  public static Task Page_Context_Carries_Profile_And_Site_Settings_Records()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);

    using JsonDocument profile = JsonDocument.Parse(PageAgentContext.Describe(scope.Store, "/Profile"));
    JsonElement profileRecord = profile.RootElement.GetProperty("profile");
    foreach (string field in new[] { "alias", "email", "language", "region", "theme", "notifications" })
    {
      profileRecord.TryGetProperty(field, out _).ShouldBeTrue(field);
    }

    using JsonDocument settings = JsonDocument.Parse(PageAgentContext.Describe(scope.Store, "/Admin/Authentication"));
    JsonElement settingsRecord = settings.RootElement.GetProperty("siteSettings");
    settingsRecord.GetProperty("version").GetInt64().ShouldBe(scope.Store.GetState<SiteSettingsState>().Version);
    settingsRecord.GetProperty("passkeyPromptMode").GetString()
      .ShouldBe(scope.Store.GetState<SiteSettingsState>().PasskeyPromptMode.ToString());
    settingsRecord.TryGetProperty("entraSignInEnabled", out _).ShouldBeTrue();
    settingsRecord.GetProperty("entraTenantAcceptanceMode").GetString()
      .ShouldBe(scope.Store.GetState<SiteSettingsState>().EntraTenantAcceptanceMode.ToString());
    settingsRecord.TryGetProperty("entraAcceptedTenants", out _).ShouldBeTrue();

    using JsonDocument ai = JsonDocument.Parse(PageAgentContext.Describe(scope.Store, "/Admin/AI"));
    JsonElement aiRecord = ai.RootElement.GetProperty("siteSettings");
    aiRecord.GetProperty("assistantName").GetString()
      .ShouldBe(scope.Store.GetState<SiteSettingsState>().AssistantName);
    aiRecord.GetProperty("xaiByokMode").GetString()
      .ShouldBe(scope.Store.GetState<SiteSettingsState>().XaiByokMode.ToString());
    aiRecord.GetProperty("version").GetInt64().ShouldBe(scope.Store.GetState<SiteSettingsState>().Version);
    return Task.CompletedTask;
  }

  public static Task Arguments_Render_As_Json_And_Empty_Payloads_Are_Omitted()
  {
    AgentArgumentText.Format(null).ShouldBeNull();
    AgentArgumentText.Format(new Dictionary<string, object?>()).ShouldBeNull();

    Dictionary<string, object?>? deserialized =
      JsonSerializer.Deserialize<Dictionary<string, object?>>("""{"path":"/Feedback"}""");
    string formatted = AgentArgumentText.Format(deserialized).ShouldNotBeNull();
    formatted.ShouldNotContain("System.Collections.Generic");
    using JsonDocument document = JsonDocument.Parse(formatted);
    document.RootElement.GetProperty("path").GetString().ShouldBe("/Feedback");

    string razor = File.ReadAllText(RepoFile(
      "source",
      "container-apps",
      "web",
      "projects",
      "web-spa",
      "features",
      "application",
      "modals",
      "agent-ask",
      "AgentAsk.razor"));
    razor.ShouldNotContain("@call.Arguments");
    razor.ShouldNotContain("@approval.Arguments");
    razor.ShouldContain("AgentArgumentText.Format(call.Arguments)");
    razor.ShouldContain("AgentArgumentText.Format(approval.Arguments)");
    return Task.CompletedTask;
  }

  public static async Task Relay_Sends_Tool_Arguments_And_Page_Context_As_Json()
  {
    CapturingChatApi api = new();
    RelayChatClient client = new(api);
    Dictionary<string, object?> arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>
    (
      """{"title":"Export failed"}"""
    ).ShouldNotBeNull();
    ChatMessage user = new(ChatRole.User, "Explain this page");
    ChatMessage call = new
    (
      ChatRole.Assistant,
      [new FunctionCallContent("call-page", "page_context", arguments)]
    );
    ChatMessage pageResult = new
    (
      ChatRole.Tool,
      [new FunctionResultContent("call-page", """{"path":"/Feedback","page":"Feedback"}""")]
    );
    ChatMessage dictionaryResult = new
    (
      ChatRole.Tool,
      [
        new FunctionResultContent
        (
          "call-dict",
          new Dictionary<string, object?> { ["path"] = "/Feedback" }
        ),
      ]
    );

    await client.GetResponseAsync([user, call, pageResult, dictionaryResult]);

    CompleteAgentChat.Command command = api.Command.ShouldNotBeNull();
    string payload = JsonSerializer.Serialize(command);
    payload.ShouldNotContain("System.Collections.Generic");

    CompleteAgentChat.Turn callTurn = command.Messages.Single(turn => turn.ToolCalls.Count > 0);
    string argumentsJson = callTurn.ToolCalls.Single().ArgumentsJson;
    argumentsJson.ShouldNotContain("System.Collections.Generic");
    using (JsonDocument parsedArguments = JsonDocument.Parse(argumentsJson))
    {
      parsedArguments.RootElement.GetProperty("title").GetString().ShouldBe("Export failed");
    }

    CompleteAgentChat.Turn pageTurn = command.Messages.Single(turn => turn.ToolCallId == "call-page");
    pageTurn.ToolResult.ShouldBe("""{"path":"/Feedback","page":"Feedback"}""");
    using JsonDocument parsedPage = JsonDocument.Parse(pageTurn.ToolResult.ShouldNotBeNull());
    parsedPage.RootElement.GetProperty("path").GetString().ShouldBe("/Feedback");

    CompleteAgentChat.Turn dictionaryTurn = command.Messages.Single(turn => turn.ToolCallId == "call-dict");
    string dictionaryJson = dictionaryTurn.ToolResult.ShouldNotBeNull();
    dictionaryJson.ShouldNotContain("System.Collections.Generic");
    using JsonDocument parsedDictionary = JsonDocument.Parse(dictionaryJson);
    parsedDictionary.RootElement.GetProperty("path").GetString().ShouldBe("/Feedback");
  }

  public static async Task Route_Follows_The_Observed_Manager_And_Keeps_The_Path_For_A_Stuck_One()
  {
    TestNavigationManager shell = new();
    TestNavigationManager stuck = new();
    PageAgentRoute route = new();
    route.PathOr(shell).ShouldBe("/");

    shell.NavigateTo("/Feedback");
    route.Observe(shell, jsRuntime: null);
    route.PathOr(shell).ShouldBe("/Feedback");
    route.PathOr(stuck).ShouldBe("/Feedback");

    // No observer runs on a focused-page hop. The observed manager's live path still wins.
    shell.NavigateTo("/Settings");
    route.PathOr(shell).ShouldBe("/Settings");
    route.PathOr(stuck).ShouldBe("/Settings");
    await Task.CompletedTask;
  }

  public static async Task Page_Context_On_Feedback_Uses_The_Ask_Scope_Not_A_Stuck_Manager()
  {
    const string secretBody = "secret body that must not reach the model";
    using FeedbackSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    NavigationManager shellNavigation = scope.ServiceProvider.GetRequiredService<NavigationManager>();
    shellNavigation.NavigateTo("/Feedback");
    StuckNavigationProvider stuck = new(scope.ServiceProvider);

    string stuckAtRoot = await InvokePageContextAsync(stuck);
    stuckAtRoot.ShouldNotContain("System.Collections.Generic");
    using (JsonDocument root = JsonDocument.Parse(stuckAtRoot))
    {
      root.RootElement.GetProperty("path").GetString().ShouldBe("/");
    }

    scope.ServiceProvider.GetRequiredService<PageAgentRoute>().Observe
    (
      shellNavigation,
      scope.ServiceProvider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()
    );
    await scope.Send(new FeedbackState.ListMyFeedbackActionSet.Action());
    await scope.Send(new FeedbackState.NoteComposerActionSet.Action(true, "Complaint", false, true));

    string feedback = await InvokePageContextAsync(stuck);
    feedback.ShouldNotContain("System.Collections.Generic");
    feedback.ShouldNotContain(secretBody);
    using (JsonDocument document = JsonDocument.Parse(feedback))
    {
      JsonElement element = document.RootElement;
      element.GetProperty("path").GetString().ShouldBe("/Feedback");
      element.GetProperty("page").GetString().ShouldBe("Feedback");
      element.GetProperty("filingsLoaded").GetBoolean().ShouldBeTrue();
      element.GetProperty("filingCount").GetInt32().ShouldBe(21);
      element.GetProperty("filings").GetArrayLength().ShouldBe(20);
      element.GetProperty("filings")[0].GetProperty("title").GetString().ShouldBe("Filing 0");
      element.GetProperty("filings")[0].TryGetProperty("body", out _).ShouldBeFalse();
      element.GetProperty("emailCopyAvailable").GetBoolean().ShouldBeTrue();
      element.GetProperty("draftAttachmentCount").GetInt32().ShouldBe(0);
      JsonElement draft = element.GetProperty("draft");
      draft.GetProperty("kind").GetString().ShouldBe("Complaint");
      draft.GetProperty("hasTitle").GetBoolean().ShouldBeFalse();
      draft.GetProperty("hasBody").GetBoolean().ShouldBeTrue();
      draft.TryGetProperty("body", out _).ShouldBeFalse();
    }

    Guid openId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    string permalink = $"/Feedback/{openId:D}";
    shellNavigation.NavigateTo(permalink);
    scope.ServiceProvider.GetRequiredService<PageAgentRoute>().Observe
    (
      shellNavigation,
      scope.ServiceProvider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()
    );
    await scope.Send(new FeedbackState.OpenFeedbackActionSet.Action(openId));

    string item = await InvokePageContextAsync(stuck);
    item.ShouldNotContain("System.Collections.Generic");
    item.ShouldNotContain(secretBody);
    using JsonDocument itemDocument = JsonDocument.Parse(item);
    itemDocument.RootElement.GetProperty("path").GetString().ShouldBe(permalink);
    itemDocument.RootElement.GetProperty("openFeedbackId").GetString().ShouldBe(openId.ToString("D"));
    itemDocument.RootElement.GetProperty("openItem").GetProperty("title").GetString().ShouldBe("Filing 0");
    itemDocument.RootElement.GetProperty("openItem").TryGetProperty("body", out _).ShouldBeFalse();
  }
}
