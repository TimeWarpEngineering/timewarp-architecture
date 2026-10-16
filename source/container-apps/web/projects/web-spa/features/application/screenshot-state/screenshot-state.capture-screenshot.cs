#region Purpose
// CaptureScreenshotActionSet: draws the page body, or one element, to a PNG and keeps it.
#endregion

#region Design
// Task 303-001. Every capture except the two composer buttons (the Ctrl-K command, the in-app
// model, a WebMCP agent) is this action. Ask's camera button is CaptureForAsk and Feedback's
// Capture this page button is CaptureForFeedback; all three go through CaptureAsync. Visibility Both with no required parameter: the palette lists it as a command and
// CatalogAgentToolSet offers it as a tool from every page. It is not on the read-only allow-list,
// so in Ask before editing an agent call shows the approval prompt first: the person sees every
// capture an agent takes. The selector is optional; blank means PageAgentContext.SurfaceRootSelector,
// the page body, which leaves out the app bar and the docked Ask panel (the page behind it).
// A selector longer than SelectorCap is refused before the browser sees it.
// The capture itself is ScreenshotState.CaptureAsync, the only caller of ScreenshotJsModule; this
// handler, CaptureForAsk and CaptureForFeedback are its three callers.
// Success sets Latest (path from PageAgentRoute), clears LastError, publishes an Info
// OutcomeNotification (the visible indicator; handlers never dispatch), and sets AgentCallOutcome
// to ScreenshotCaptured, whose Image (a data: URI) the WebMCP caller receives. A refusal sets LastError,
// leaves Latest alone, sets AgentCallOutcome to ScreenshotRefused (code and plain words, so an agent
// learns why), and publishes an Error notification. Nothing is uploaded here.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text.Json.Serialization;
using TimeWarp.Architecture.Components;

partial class ScreenshotState
{
  public static class CaptureScreenshotActionSet
  {
    [CatalogAction
    (
      DisplayName = "Capture screenshot",
      Description = "Capture a PNG screenshot of the current page or of the element a CSS selector names, with data-no-capture elements and password fields masked.",
      Visibility = ActionVisibility.Both
    )]
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(string selector = "")
      {
        Selector = selector ?? "";
      }

      /// <summary>CSS selector of the element to capture; empty captures the page body.</summary>
      public string Selector { get; }
    }

    internal sealed class Handler
    (
      IStore store,
      IJSRuntime jsRuntime,
      PageAgentRoute route,
      NavigationManager navigation,
      AgentCallOutcome agentCallOutcome,
      IPublisher<ClientPipeline> publisher
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        CaptureServices services = new(jsRuntime, route, navigation, agentCallOutcome, publisher);
        await ScreenshotState.CaptureAsync(action.Selector, services, cancellationToken);
      }
    }
  }

  /// <summary>What a capture handler needs from the container.</summary>
  internal sealed record CaptureServices
  (
    IJSRuntime JsRuntime,
    PageAgentRoute Route,
    NavigationManager Navigation,
    AgentCallOutcome AgentCallOutcome,
    IPublisher<ClientPipeline> Publisher
  );

  /// <summary>
  /// Draws the page body (blank selector) or one element and keeps it as Latest. Returns the new
  /// capture, or null after a refusal. The only caller of ScreenshotJsModule.
  /// </summary>
  private async Task<ScreenshotCapture?> CaptureAsync
  (
    string selectorText,
    CaptureServices services,
    CancellationToken cancellationToken
  )
  {
    string trimmed = selectorText.Trim();
    string? selector = trimmed.Length == 0 ? null : trimmed;
    if (trimmed.Length > SelectorCap)
    {
      await FailAsync("selector-too-long", services, cancellationToken);
      return null;
    }

    ScreenshotJsModule.Result result = await ScreenshotJsModule.CaptureAsync
    (
      services.JsRuntime,
      selector ?? PageAgentContext.SurfaceRootSelector,
      MaxBytes,
      cancellationToken
    );
    if (result.Error is not null)
    {
      await FailAsync(result.Error, services, cancellationToken);
      return null;
    }

    int id = NextId + 1;
    ScreenshotCapture capture = new
    (
      id,
      result.Png,
      result.Width,
      result.Height,
      PageAgentScope.Normalize(services.Route.PathOr(services.Navigation)),
      selector,
      result.Masked,
      DateTimeOffset.UtcNow
    );
    NextId = id;
    Latest = capture;
    LastError = null;
    services.AgentCallOutcome.Set(ScreenshotCaptured.From(capture));
    string masked = capture.Masked == 0 ? "" : $" {capture.Masked} masked.";
    await services.Publisher.Publish
    (
      new OutcomeNotification(MessageBarIntent.Info, "Screenshot captured", $"{capture.Caption}.{masked} Nothing was uploaded."),
      cancellationToken
    );
    return capture;
  }

  private Task FailAsync(string error, CaptureServices services, CancellationToken cancellationToken)
  {
    LastError = error;
    string description = Describe(error);
    services.AgentCallOutcome.Set(new ScreenshotRefused(error, description));
    return services.Publisher.Publish
    (
      new OutcomeNotification(MessageBarIntent.Error, "Screenshot not captured", description),
      cancellationToken
    );
  }

  /// <summary>Plain words for a capture refusal code.</summary>
  public static string Describe(string error) => error switch
  {
    "not-found" => "No element on this page matches that selector.",
    "invalid-selector" => "That is not a valid CSS selector.",
    "selector-too-long" => $"The selector is longer than {SelectorCap} characters.",
    "too-large" => "The picture is too large even after scaling it down.",
    "tainted" => "This browser does not allow the page to be drawn.",
    _ => "The browser could not draw the page.",
  };

  /// <summary>What an agent receives when a capture is refused: the refusal code and plain words.</summary>
  public sealed record ScreenshotRefused(string Error, string Description);

  /// <summary>What an agent receives after a capture: the size, route, and Image, the PNG as a data: URI.</summary>
  public sealed record ScreenshotCaptured
  (
    int Width,
    int Height,
    string ContentType,
    int ByteLength,
    string Path,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Selector,
    int Masked,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Image
  ) : IAgentImageOutcome
  {
    public static ScreenshotCaptured From(ScreenshotCapture capture) => new
    (
      capture.Width,
      capture.Height,
      ScreenshotState.ContentType,
      capture.Png.Length,
      capture.Path,
      capture.Selector,
      capture.Masked,
      capture.ImageSource
    );

    public object WithoutImage() => this with { Image = null };

    public DataContent ToImage() =>
      new(Image ?? throw new InvalidOperationException("This outcome has no picture."));
  }
}
