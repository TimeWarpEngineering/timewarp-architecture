#region Purpose
// Feedback tool scoping and submit receipt, plus page_context tests and the stand-in surface JS runtime.
#endregion

namespace CatalogAgent_;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.AI;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Feedback;

[TestTag("Integration")]
public partial class CatalogAgent_Should
{
  public static async Task Feedback_Tools_Are_Page_Scoped_And_Read_Only_Except_Submit()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    IAuthorizationService authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
    ClaimsPrincipal everyone = Principal([.. PermissionIds.All]);

    IReadOnlyList<WebMcpToolDescriptor> feedback = await WebMcpPublisher.DescribeAsync
    (
      everyone, authorization, catalog, "/Feedback", CancellationToken.None
    );
    Names(feedback).ShouldBe(await ExpectedWebMcpNamesAsync(everyone, authorization, catalog, "/Feedback"));
    feedback.ShouldContain(tool => tool.Name == "Feedback.SubmitFeedback");
    feedback.ShouldContain(tool => tool.Name == "Feedback.ListMyFeedback");
    feedback.ShouldContain(tool => tool.Name == "Feedback.OpenFeedback");

    Guid itemId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    IReadOnlyList<WebMcpToolDescriptor> detail = await WebMcpPublisher.DescribeAsync
    (
      everyone, authorization, catalog, $"/Feedback/{itemId:D}", CancellationToken.None
    );
    Names(detail).ShouldBe(Names(feedback));

    Names
    (
      await WebMcpPublisher.DescribeAsync
      (
        everyone, authorization, catalog, "/FeedbackExtra", CancellationToken.None
      )
    ).ShouldBe(await ExpectedWebMcpNamesAsync(everyone, authorization, catalog, "/FeedbackExtra"));

    ActionCatalogEntry submit = catalog.Find("Feedback.SubmitFeedback").ShouldNotBeNull();
    ActionCatalogEntry list = catalog.Find("Feedback.ListMyFeedback").ShouldNotBeNull();
    ActionCatalogEntry open = catalog.Find("Feedback.OpenFeedback").ShouldNotBeNull();
    CatalogAgentApproval.RequiresApproval(submit).ShouldBeTrue();
    CatalogAgentApproval.RequiresApproval(list).ShouldBeFalse();
    CatalogAgentApproval.RequiresApproval(open).ShouldBeFalse();
    submit.Permissions.ShouldBe([PermissionIds.FeedbackFileSelf]);
    submit.Visibility.ShouldBe(ActionVisibility.Both);

    string schema = CatalogAgentSchema.For(submit);
    schema.ShouldContain("BugReport");
    schema.ShouldContain("FeatureRequest");
    schema.ShouldContain("Complaint");
    schema.ShouldContain("Other");
    schema.ShouldContain("emailCopy");

    object?[] bound = CatalogAgentArguments.Bind
    (
      submit,
      new Dictionary<string, object?>
      {
        ["kind"] = "Complaint",
        ["title"] = "The export failed",
        ["body"] = "Nothing came back.",
        ["emailCopy"] = false,
      }
    );
    bound[0].ShouldBe(FeedbackKind.Complaint);
    bound[1].ShouldBe("The export failed");
    bound[2].ShouldBe("Nothing came back.");
    bound[3].ShouldBe(false);

    Console.WriteLine($"WEBMCP-PROOF path=/Feedback tools={string.Join(",", Names(feedback))}");
  }

  public static async Task WebMcp_Approved_Submit_Feedback_Returns_The_Real_Receipt()
  {
    using FeedbackSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Feedback");
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();

    Task<string> invoke = dispatcher.InvokeTool
    (
      "Feedback.SubmitFeedback",
      """{"kind":"Complaint","title":"The export failed","body":"Nothing came back.","emailCopy":false}"""
    );
    Guid callId = await WaitForPendingAsync(scope, invoke);
    spa.Api.Requests.ShouldBeEmpty();
    await scope.Store.GetState<AgentSurfaceState>().ResolveApproval(callId, approved: true);
    string receipt = await invoke.WaitAsync(Timeout);

    SubmitFeedback.Command sent = spa.Api.Requests.ShouldHaveSingleItem();
    sent.Kind.ShouldBe(FeedbackKind.Complaint);
    sent.Title.ShouldBe("The export failed");
    using JsonDocument document = JsonDocument.Parse(receipt);
    JsonElement root = document.RootElement;
    root.GetProperty("action").GetString().ShouldBe("Feedback.SubmitFeedback");
    root.GetProperty("completed").GetBoolean().ShouldBeTrue();
    JsonElement result = root.GetProperty("result");
    Guid feedbackItemId = result.GetProperty("feedbackItemId").GetGuid();
    feedbackItemId.ShouldBe(spa.Api.LastId.ShouldNotBeNull());
    result.GetProperty("permalink").GetString().ShouldBe($"/Feedback/{feedbackItemId:D}");
    result.GetProperty("emailCopySent").GetBoolean().ShouldBeFalse();

    FeedbackState state = scope.Store.GetState<FeedbackState>();
    state.LastReceipt.ShouldNotBeNull().FeedbackItemId.ShouldBe(feedbackItemId);
    state.Items.ShouldNotBeEmpty();
    state.Items[0].FeedbackItemId.ShouldBe(feedbackItemId);
    state.Items[0].Permalink.ShouldBe($"/Feedback/{feedbackItemId:D}");
    Console.WriteLine($"WEBMCP-PROOF invoke Feedback.SubmitFeedback approved receipt={receipt}");
  }

  public static async Task Home_Page_Context_Carries_Title_Purpose_Tools_And_Headings()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    string bare = PageAgentContext.Describe(scope.Store, "/");
    using (JsonDocument document = JsonDocument.Parse(bare))
    {
      document.RootElement.GetProperty("path").GetString().ShouldBe("/");
      document.RootElement.GetProperty("title").GetString().ShouldBe("Home");
      document.RootElement.GetProperty("purpose").GetString()
        .ShouldBe("Public welcome page for TimeWarp.Architecture, with a sign-in entry.");
      document.RootElement.GetProperty("tools").GetArrayLength().ShouldBeGreaterThan(1);
      document.RootElement.GetProperty("screenshot").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    bare.ShouldContain("\"title\"");
    bare.Length.ShouldBeGreaterThan("{\"path\":\"/\"}".Length);

    await scope.Send
    (
      new AgentSurfaceState.RememberPageSurfaceActionSet.Action
      (
        "/",
        """
        {"headings":["Welcome to TimeWarp.Architecture","Built with","Signed in"],"summary":"Welcome to TimeWarp.Architecture. Built with. Signed in.","forms":[],"buttons":[],"items":[]}
        """
      )
    );
    string described = PageAgentContext.Describe(scope.Store, "/");
    described.ShouldContain("Welcome to TimeWarp.Architecture");
    described.ShouldContain("Built with");
    described.ShouldContain("Signed in");
    Console.WriteLine("PAGE-CONTEXT / " + described);

    string feedback = PageAgentContext.Describe(scope.Store, "/Feedback");
    feedback.ShouldContain("File feedback and review the filings you submitted.");
    feedback.ShouldContain("\"title\":\"Feedback\"");
    feedback.ShouldNotContain("Welcome to TimeWarp.Architecture");
    feedback.ShouldNotContain("Built with");
    Console.WriteLine("PAGE-CONTEXT /Feedback " + feedback);
  }

  public static async Task Page_Context_Stays_Valid_Json_Under_Its_Cap()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    string longText = new('h', 500);
    System.Text.Json.Nodes.JsonObject surface = new()
    {
      ["headings"] = new System.Text.Json.Nodes.JsonArray([.. Enumerable.Range(0, 60).Select(_ => (System.Text.Json.Nodes.JsonNode?)longText)]),
      ["summary"] = new string('s', 5_000),
      ["buttons"] = new System.Text.Json.Nodes.JsonArray([.. Enumerable.Range(0, 60).Select(_ => (System.Text.Json.Nodes.JsonNode?)longText)]),
      ["forms"] = new System.Text.Json.Nodes.JsonArray
      (
        [
          .. Enumerable.Range(0, 20).Select
          (
            _ => (System.Text.Json.Nodes.JsonNode?)new System.Text.Json.Nodes.JsonObject
            {
              ["name"] = longText,
              ["fields"] = new System.Text.Json.Nodes.JsonArray([.. Enumerable.Range(0, 40).Select(_ => (System.Text.Json.Nodes.JsonNode?)longText)]),
            }
          ),
        ]
      ),
      ["items"] = new System.Text.Json.Nodes.JsonArray
      (
        [
          .. Enumerable.Range(0, 80).Select
          (
            _ => (System.Text.Json.Nodes.JsonNode?)new System.Text.Json.Nodes.JsonObject { ["id"] = longText, ["text"] = longText }
          ),
        ]
      ),
    };
    await scope.Send(new AgentSurfaceState.RememberPageSurfaceActionSet.Action("/Counter", surface.ToJsonString()));

    string described = PageAgentContext.Describe(scope.Store, "/Counter");
    described.Length.ShouldBeLessThanOrEqualTo(PageAgentContext.DocumentCap);
    using (JsonDocument document = JsonDocument.Parse(described))
    {
      document.RootElement.GetProperty("title").GetString().ShouldBe("Counter");
      foreach (JsonElement heading in document.RootElement.GetProperty("headings").EnumerateArray())
      {
        heading.GetString().ShouldNotBeNull().Length.ShouldBeLessThanOrEqualTo(200);
      }
    }

    string forAsk = PageAgentContext.Describe
    (
      scope.Store, "/Counter", toolNames: null, liveSurfaceJson: null, AgentAskInstructions.ContextCap
    );
    forAsk.Length.ShouldBeLessThanOrEqualTo(AgentAskInstructions.ContextCap);
    using (JsonDocument document = JsonDocument.Parse(forAsk))
    {
      document.RootElement.GetProperty("path").GetString().ShouldBe("/Counter");
    }
  }

  public static async Task WebMcp_Page_Context_Walks_The_Page_When_Called()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IServiceProvider services = scope.ServiceProvider;
    await scope.Send
    (
      new AgentSurfaceState.RememberPageSurfaceActionSet.Action
      (
        "/", """{"headings":["Welcome to TimeWarp.Architecture"],"summary":"Welcome.","forms":[],"buttons":[],"items":[]}"""
      )
    );
    services.GetRequiredService<NavigationManager>().NavigateTo("/Counter");

    SurfaceJsRuntime browser = new
    (
      """{"headings":["Counter live heading"],"summary":"Counter live text.","forms":[],"buttons":["Increment"],"items":[]}"""
    );
    WebMcpDispatcher dispatcher = new
    (
      scope.Store,
      services.GetRequiredService<IActionCatalog>(),
      services.GetRequiredService<IAuthorizationService>(),
      services.GetRequiredService<AuthenticationStateProvider>(),
      services.GetRequiredService<NavigationManager>(),
      services.GetRequiredService<PageAgentRoute>(),
      services.GetRequiredService<WebMcpApprovalGate>(),
      services.GetRequiredService<AgentCallOutcome>(),
      browser
    );

    string described = await dispatcher.InvokeTool(PageAgentContext.ToolName, "{}").WaitAsync(Timeout);
    described.ShouldContain("\"path\":\"/Counter\"");
    described.ShouldContain("Counter live heading");
    described.ShouldContain("Increment");
    described.ShouldNotContain("Welcome to TimeWarp.Architecture");
    browser.Selector.ShouldBe(PageAgentContext.SurfaceRootSelector);
    Console.WriteLine("PAGE-CONTEXT live /Counter " + described);
  }

  /// <summary>Stands in for the browser: import returns a module whose summarizeJson answers the fixture.</summary>
  private sealed class SurfaceJsRuntime : Microsoft.JSInterop.IJSRuntime, Microsoft.JSInterop.IJSObjectReference
  {
    private readonly string Json;

    public SurfaceJsRuntime(string json)
    {
      Json = json;
    }

    public string? Selector { get; private set; }

    // Test double: this forwards to the other overload. It is not a browser interop call.
#pragma warning disable BL0016
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
      InvokeAsync<TValue>(identifier, CancellationToken.None, args);
#pragma warning restore BL0016

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
      if (identifier == "import")
      {
        return ValueTask.FromResult((TValue)(object)this);
      }

      if (identifier == "summarizeJson")
      {
        Selector = args is { Length: > 0 } ? args[0] as string : null;
        return ValueTask.FromResult((TValue)(object)Json);
      }

      throw new InvalidOperationException($"Unexpected identifier '{identifier}'.");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  }
}
