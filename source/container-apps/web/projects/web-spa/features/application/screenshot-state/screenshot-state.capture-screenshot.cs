#region Purpose
// CaptureScreenshotActionSet: draws the page body, or one element, to a PNG and keeps it.
#endregion

#region Design
// Task 303-001. The only caller of ScreenshotJsModule, so every capture (Ask's Screenshot
// button, Feedback's capture, the Ctrl-K command, the in-app model, a WebMCP agent) is this
// action. Visibility Both with no required parameter: the palette lists it as a command and
// CatalogAgentToolSet offers it as a tool from every page. It is not on the read-only allow-list,
// so in Ask before editing an agent call shows the approval prompt first: the person sees every
// capture an agent takes. The selector is optional; blank means PageAgentContext.SurfaceRootSelector,
// the page body, which leaves out the app bar and the docked Ask panel (the page behind it).
// A selector longer than SelectorCap is refused before the browser sees it.
// Success sets Latest (path from PageAgentRoute), clears LastError, publishes an Info
// OutcomeNotification (the visible indicator; handlers never dispatch), and sets AgentCallOutcome
// to ScreenshotCaptured, whose Image (a data: URI) the WebMCP caller receives. A refusal sets LastError,
// leaves Latest alone, and publishes an Error notification. Nothing is uploaded here.
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
        string trimmed = action.Selector.Trim();
        string? selector = trimmed.Length == 0 ? null : trimmed;
        if (trimmed.Length > SelectorCap)
        {
          await FailAsync("selector-too-long", cancellationToken);
          return;
        }

        ScreenshotJsModule.Result result = await ScreenshotJsModule.CaptureAsync
        (
          jsRuntime,
          selector ?? PageAgentContext.SurfaceRootSelector,
          MaxBytes,
          cancellationToken
        );
        if (result.Error is not null)
        {
          await FailAsync(result.Error, cancellationToken);
          return;
        }

        int id = ScreenshotState.NextId + 1;
        ScreenshotCapture capture = new
        (
          id,
          result.Png,
          result.Width,
          result.Height,
          PageAgentScope.Normalize(route.PathOr(navigation)),
          selector,
          result.Masked,
          DateTimeOffset.UtcNow
        );
        ScreenshotState.NextId = id;
        ScreenshotState.Latest = capture;
        ScreenshotState.LastError = null;
        agentCallOutcome.Set(ScreenshotCaptured.From(capture));
        string masked = capture.Masked == 0 ? "" : $" {capture.Masked} masked.";
        await publisher.Publish
        (
          new OutcomeNotification(MessageBarIntent.Info, "Screenshot captured", $"{capture.Caption}.{masked} Nothing was uploaded."),
          cancellationToken
        );
      }

      private Task FailAsync(string error, CancellationToken cancellationToken)
      {
        ScreenshotState.LastError = error;
        return publisher.Publish
        (
          new OutcomeNotification(MessageBarIntent.Error, "Screenshot not captured", Describe(error)),
          cancellationToken
        );
      }
    }
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
