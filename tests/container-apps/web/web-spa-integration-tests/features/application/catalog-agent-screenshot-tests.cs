#region Purpose
// Task 303-001: the capture_screenshot WebMCP tool runs ScreenshotState.CaptureScreenshot, and the
// picture travels to the model as image content, never as tool-result text.
#endregion

#region Design
// Same shape as the feedback receipt proof: an in-proc SPA (CaptureSpa, C-create) with the real
// catalog, store, WebMCP dispatcher and approval gate, signed in with every permission. The one
// stand-in is the browser: CaptureJsRuntime answers screenshot.js's capture and takeBytes, the way
// SurfaceJsRuntime stands in for page-surface.js. Everything between the tool call and the state
// (selection, approval, binding, the ActionSet handler, AgentCallOutcome, the dispatcher's JSON) is
// product code. The relay proof drives RelayChatClient and AskScreenshotChatClient over the
// recording API: a waiting Ask screenshot becomes the user turn's Images, and a tool result that
// carries a picture becomes a user turn after the tool turns. The limit test is the build check
// that keeps ScreenshotState.MaxBytes equal to FeedbackAttachmentRules.MaxBytes. The cap proof
// sends an Ask screenshot plus two tool pictures in one turn and checks the command stays within
// CompleteAgentChat.MaxImagesPerRequest and validates. CaptureSpa registers no API service:
// nothing in these proofs calls web-server. CaptureSpa and CaptureJsRuntime are internal so the
// Ask rejection tests (AskRejection_Should, no Aspire session) select the same real tools.
#endregion

namespace CatalogAgent_;

using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Features.Feedback;
using TimeWarp.Architecture.Web.Spa;

public partial class CatalogAgent_Should
{
  private const string CaptureTool = "Screenshot.CaptureScreenshot";

  private static readonly byte[] FakePng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

  public static async Task WebMcp_Capture_Screenshot_Runs_The_Action_And_Returns_The_Image()
  {
    CaptureJsRuntime browser = new(new ScreenshotJsModule.BrowserCapture
    {
      Token = "shot-1",
      Width = 640,
      Height = 480,
      ByteLength = FakePng.Length,
      Masked = 2,
    });
    using CaptureSpa spa = new(browser);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Settings");

    string before = PageAgentContext.Describe(scope.Store, "/Settings");
    using (JsonDocument document = JsonDocument.Parse(before))
    {
      document.RootElement.GetProperty("screenshot").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    IReadOnlyList<CatalogAgentTool> tools = await CatalogAgentToolSet.SelectAsync
    (
      spa.User,
      scope.ServiceProvider.GetRequiredService<IAuthorizationService>(),
      scope.ServiceProvider.GetRequiredService<IActionCatalog>().Entries,
      "/Settings",
      CancellationToken.None
    );
    CatalogAgentTool tool = tools.Single(candidate => candidate.Name == CaptureTool);
    tool.RequiresApproval.ShouldBeTrue("an agent's capture must be visible to the person");
    tool.InputSchema.ShouldContain("selector");

    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();
    Task<string> invoke = dispatcher.InvokeTool(CaptureTool, "{}");
    Guid callId = await WaitForPendingAsync(scope, invoke);
    browser.Selector.ShouldBeNull("nothing is drawn before the person approves");
    await scope.Store.GetState<AgentSurfaceState>().ResolveApproval(callId, approved: true);
    string json = await invoke.WaitAsync(Timeout);

    browser.Selector.ShouldBe(PageAgentContext.SurfaceRootSelector);
    browser.MaxBytes.ShouldBe(ScreenshotState.MaxBytes);
    using (JsonDocument document = JsonDocument.Parse(json))
    {
      JsonElement root = document.RootElement;
      root.GetProperty("action").GetString().ShouldBe(CaptureTool);
      root.GetProperty("completed").GetBoolean().ShouldBeTrue();
      JsonElement result = root.GetProperty("result");
      result.GetProperty("width").GetInt32().ShouldBe(640);
      result.GetProperty("height").GetInt32().ShouldBe(480);
      result.GetProperty("contentType").GetString().ShouldBe("image/png");
      result.GetProperty("byteLength").GetInt32().ShouldBe(FakePng.Length);
      result.GetProperty("path").GetString().ShouldBe("/Settings");
      result.GetProperty("masked").GetInt32().ShouldBe(2);
      result.GetProperty("image").GetString().ShouldBe("data:image/png;base64," + Convert.ToBase64String(FakePng));
    }

    ScreenshotCapture capture = scope.Store.GetState<ScreenshotState>().Latest.ShouldNotBeNull();
    capture.Png.ShouldBe(FakePng);
    capture.Path.ShouldBe("/Settings");
    capture.Selector.ShouldBeNull();
    scope.Store.GetState<ScreenshotState>().LastError.ShouldBeNull();
    scope.Store.GetState<NotificationState>().Messages
      .ShouldContain(message => message.Title == "Screenshot captured");

    string after = PageAgentContext.Describe(scope.Store, "/Settings");
    after.ShouldNotContain("base64", Case.Sensitive, "page_context carries metadata, never the picture");
    using (JsonDocument document = JsonDocument.Parse(after))
    {
      JsonElement screenshot = document.RootElement.GetProperty("screenshot");
      screenshot.GetProperty("width").GetInt32().ShouldBe(640);
      screenshot.GetProperty("masked").GetInt32().ShouldBe(2);
      screenshot.GetProperty("sentToAsk").GetBoolean().ShouldBeFalse();
    }

    using (JsonDocument document = JsonDocument.Parse(PageAgentContext.Describe(scope.Store, "/Feedback")))
    {
      document.RootElement.GetProperty("screenshot").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    Console.WriteLine($"WEBMCP-PROOF invoke {CaptureTool} approved result={json[..Math.Min(json.Length, 240)]}");
  }

  public static async Task WebMcp_Capture_Screenshot_Refusal_Keeps_The_Last_Capture_Out()
  {
    CaptureJsRuntime browser = new(new ScreenshotJsModule.BrowserCapture { Error = "not-found" });
    using CaptureSpa spa = new(browser);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Settings");
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();

    Task<string> invoke = dispatcher.InvokeTool(CaptureTool, """{"selector":"#missing"}""");
    Guid callId = await WaitForPendingAsync(scope, invoke);
    await scope.Store.GetState<AgentSurfaceState>().ResolveApproval(callId, approved: true);
    string json = await invoke.WaitAsync(Timeout);

    browser.Selector.ShouldBe("#missing");
    json.ShouldNotContain("\"image\"");
    ScreenshotState state = scope.Store.GetState<ScreenshotState>();
    state.Latest.ShouldBeNull();
    state.LastError.ShouldBe("not-found");
    browser.Took.ShouldBeFalse();
    scope.Store.GetState<NotificationState>().Messages
      .ShouldContain(message => message.Title == "Screenshot not captured");
    using (JsonDocument document = JsonDocument.Parse(json))
    {
      JsonElement result = document.RootElement.GetProperty("result");
      result.GetProperty("error").GetString().ShouldBe("not-found", "the agent learns why nothing was captured");
      result.GetProperty("description").GetString().ShouldBe(ScreenshotState.Describe("not-found"));
    }
  }

  public static Task Screenshot_Limit_Matches_The_Feedback_Attachment_Rules()
  {
    ScreenshotState.MaxBytes.ShouldBe(FeedbackAttachmentRules.MaxBytes);
    FeedbackAttachmentRules.CanonicalContentType(ScreenshotState.ContentType).ShouldBe(ScreenshotState.ContentType);
    CompleteAgentChat.ImageMediaTypes.ShouldContain(ScreenshotState.ContentType);
    // base64 of MaxBytes must fit the relay's per-image cap.
    ((ScreenshotState.MaxBytes + 2) / 3 * 4).ShouldBeLessThanOrEqualTo(CompleteAgentChat.MaxImageDataLength);
    return Task.CompletedTask;
  }

  public static Task Screenshot_File_Name_Is_An_Iso_Basic_Utc_Stamp()
  {
    // Task 452: the attachment name carries the capture instant as ISO 8601 basic UTC, whatever
    // offset the capture was taken in.
    DateTimeOffset capturedAt = new(2026, 10, 28, 8, 34, 56, TimeSpan.FromHours(-4));
    ScreenshotCapture capture = new(7, FakePng, 640, 480, "/Settings", selector: null, masked: 0, capturedAt);

    capture.FileName.ShouldBe("screenshot-20261028T123456Z-7.png");
    return Task.CompletedTask;
  }

  public static async Task Relay_Sends_Ask_Screenshot_And_Tool_Picture_As_Images()
  {
    CapturingChatApi api = new();
    ScreenshotCapture capture = new(7, FakePng, 640, 480, "/Settings", selector: null, masked: 0, DateTimeOffset.UtcNow);
    List<int> marked = [];
    using AskScreenshotChatClient client = new
    (
      new RelayChatClient(api),
      () => marked.Count == 0 ? capture : null,
      id =>
      {
        marked.Add(id);
        return Task.CompletedTask;
      }
    );

    ChatMessage user = new(ChatRole.User, "What is wrong here?");
    await client.GetResponseAsync([user]);

    CompleteAgentChat.Command first = api.Command.ShouldNotBeNull();
    CompleteAgentChat.Turn userTurn = first.Messages.ShouldHaveSingleItem();
    userTurn.Text.ShouldBe("What is wrong here?");
    CompleteAgentChat.TurnImage image = userTurn.Images.ShouldHaveSingleItem();
    image.MediaType.ShouldBe("image/png");
    image.Data.ShouldBe(Convert.ToBase64String(FakePng));
    marked.ShouldBe([7]);
    user.Contents.OfType<DataContent>().ShouldBeEmpty("the thread keeps the person's own message");
    new CompleteAgentChat.Validator().Validate(first).IsValid.ShouldBeTrue();

    // The same turn's tool loop: the capture tool answered; its picture follows the tool turn.
    ChatMessage call = new(ChatRole.Assistant, [new FunctionCallContent("call-capture", CaptureTool, new Dictionary<string, object?>())]);
    ChatMessage result = new
    (
      ChatRole.Tool,
      [new FunctionResultContent("call-capture", new PictureResult("""{"width":640}""", new DataContent(FakePng, "image/png")))]
    );
    await client.GetResponseAsync([user, call, result]);

    CompleteAgentChat.Command second = api.Command.ShouldNotBeNull();
    second.Messages.Count.ShouldBe(4);
    CompleteAgentChat.Turn toolTurn = second.Messages[2];
    toolTurn.Role.ShouldBe("tool");
    toolTurn.Images.ShouldBeEmpty();
    toolTurn.ToolResult.ShouldNotBeNull().ShouldNotContain(Convert.ToBase64String(FakePng));
    CompleteAgentChat.Turn pictureTurn = second.Messages[3];
    pictureTurn.Role.ShouldBe("user");
    pictureTurn.Text.ShouldBe(RelayChatClient.ToolPictureText);
    pictureTurn.Images.ShouldHaveSingleItem().Data.ShouldBe(Convert.ToBase64String(FakePng));
    second.Messages[0].Images.ShouldBeEmpty("the Ask screenshot was sent once");
    new CompleteAgentChat.Validator().Validate(second).IsValid.ShouldBeTrue();

    // A later turn does not send the tool picture again.
    ChatMessage answer = new(ChatRole.Assistant, "It looks fine.");
    ChatMessage next = new(ChatRole.User, "Thanks");
    await client.GetResponseAsync([user, call, result, answer, next]);
    api.Command.ShouldNotBeNull().Messages.ShouldAllBe(turn => turn.Images.Count == 0);
  }

  public static async Task Relay_Keeps_The_Ask_Screenshot_And_The_Newest_Tool_Picture_Within_The_Cap()
  {
    CapturingChatApi api = new();
    byte[] older = [.. FakePng, 0xA1];
    byte[] newer = [.. FakePng, 0xB2];
    ScreenshotCapture capture = new(9, FakePng, 640, 480, "/Settings", selector: null, masked: 0, DateTimeOffset.UtcNow);
    using RelayChatClient client = new(api);

    // One Ask turn: the person attached a screenshot, then the model captured twice (page, then
    // element). AskScreenshotChatClient sits outside the tool loop, so every loop iteration hands
    // the relay the person's message with the screenshot still on it.
    ChatMessage user = AskScreenshotChatClient.WithScreenshot([new ChatMessage(ChatRole.User, "What is wrong here?")], capture)
      .ShouldNotBeNull()
      .ShouldHaveSingleItem();
    ChatMessage firstCall = new(ChatRole.Assistant, [new FunctionCallContent("call-1", CaptureTool, new Dictionary<string, object?>())]);
    ChatMessage firstResult = new
    (
      ChatRole.Tool,
      [new FunctionResultContent("call-1", new PictureResult("""{"width":640}""", new DataContent(older, "image/png")))]
    );
    ChatMessage secondCall = new
    (
      ChatRole.Assistant,
      [new FunctionCallContent("call-2", CaptureTool, new Dictionary<string, object?> { ["selector"] = "#main" })]
    );
    ChatMessage secondResult = new
    (
      ChatRole.Tool,
      [new FunctionResultContent("call-2", new PictureResult("""{"width":320}""", new DataContent(newer, "image/png")))]
    );
    await client.GetResponseAsync([user, firstCall, firstResult, secondCall, secondResult]);

    CompleteAgentChat.Command command = api.Command.ShouldNotBeNull();
    command.Messages.Sum(turn => turn.Images.Count).ShouldBeLessThanOrEqualTo(CompleteAgentChat.MaxImagesPerRequest);
    new CompleteAgentChat.Validator().Validate(command).IsValid.ShouldBeTrue();
    command.Messages[0].Images.ShouldHaveSingleItem("the person's own screenshot counts first")
      .Data.ShouldBe(Convert.ToBase64String(FakePng));
    List<CompleteAgentChat.TurnImage> toolPictures = [.. command.Messages.Skip(1).SelectMany(turn => turn.Images)];
    toolPictures.ShouldHaveSingleItem("only the newest tool picture fits").Data.ShouldBe(Convert.ToBase64String(newer));
    command.Messages.ShouldContain
    (
      turn => turn.Role == "user" && turn.Images.Count == 0 && turn.Text == RelayChatClient.OmittedPictureText
    );
  }

  /// <summary>A tool result that carries a picture, as CatalogAgentFunctions returns for a capture.</summary>
  private sealed record PictureResult(string Summary, [property: JsonIgnore] DataContent? Image) : IToolResultImage;

  /// <summary>Stands in for the browser: import returns a module whose capture and takeBytes answer the fixture.</summary>
  /// <remarks>Both overloads read <see cref="CaptureExports"/>, a plain type, so no call here is to a JS runtime.</remarks>
  internal sealed class CaptureJsRuntime(ScreenshotJsModule.BrowserCapture capture) : IJSRuntime, IJSObjectReference
  {
    private readonly CaptureExports Exports = new(capture);

    public string? Selector => Exports.Selector;

    public int MaxBytes => Exports.MaxBytes;

    public bool Took => Exports.Took;

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
      object answer = identifier == "import" ? this : Exports.Answer(identifier, args);
      return ValueTask.FromResult((TValue)answer);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
      object answer = identifier == "import" ? this : Exports.Answer(identifier, args);
      return ValueTask.FromResult((TValue)answer);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  }

  /// <summary>What screenshot.js's capture and takeBytes answer, and what they were asked.</summary>
  private sealed class CaptureExports(ScreenshotJsModule.BrowserCapture capture)
  {
    public string? Selector { get; private set; }

    public int MaxBytes { get; private set; }

    public bool Took { get; private set; }

    public object Answer(string export, object?[]? args)
    {
      switch (export)
      {
        case ScreenshotJsModule.CaptureExport:
          Selector = args?[0] as string;
          MaxBytes = args?[1] is int max ? max : 0;
          return capture;
        case ScreenshotJsModule.TakeBytesExport:
          Took = true;
          return new PngStream(FakePng);
        default:
          throw new InvalidOperationException($"Unexpected identifier '{export}'.");
      }
    }
  }

  private sealed class PngStream(byte[] bytes) : IJSStreamReference
  {
    public long Length => bytes.Length;

    public ValueTask<Stream> OpenReadStreamAsync(long maxAllowedSize = 512_000, CancellationToken cancellationToken = default)
    {
      Length.ShouldBeLessThanOrEqualTo(maxAllowedSize);
      return ValueTask.FromResult<Stream>(new MemoryStream(bytes));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  }

  /// <summary>In-proc SPA: real catalog, WebMCP dispatcher, and store; signed in with every permission; the browser is <see cref="CaptureJsRuntime"/>.</summary>
  internal sealed class CaptureSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }

    public ClaimsPrincipal User { get; } = new(new ClaimsIdentity(
      [
        new Claim("sub", Guid.NewGuid().ToString()),
        .. PermissionIds.All.Select(static permission => new Claim(PermissionIds.ClaimType, permission)),
      ],
      authenticationType: "test"));

    public CaptureSpa(CaptureJsRuntime browser)
    {
      ServiceCollection services = new();
      services.AddLogging();
      services.AddWebSpaGeneratedMediator();
      services.AddTimeWarpState
      (
        options =>
        {
          options.Assemblies =
          [
            typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly,
            typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly
          ];
        }
      );
      services.AddTimeWarpStateBlazor();
      services.AddActionCatalog(typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly);
      services.AddTimeWarpStatePersistence();
      services.AddAuthorizationCore(TimeWarp.Architecture.PolicyRegistration.AddPolicies);
      ClaimsPrincipal user = User;
      services.AddScoped<AuthenticationStateProvider>(_ => new FixedAuthenticationStateProvider(user));
      // An instance registration: the container does not own (or dispose) the stand-in browser.
      services.AddSingleton<IJSRuntime>(browser);
      services.AddScoped<NavigationManager, TestNavigationManager>();
      services.AddScoped<PageAgentRoute>();
      services.AddScoped<AgentCallOutcome>();
      services.AddScoped<WebMcpApprovalGate>();
      services.AddScoped<WebMcpDispatcher>();

      ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
      if (ServiceProvider is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }
}
