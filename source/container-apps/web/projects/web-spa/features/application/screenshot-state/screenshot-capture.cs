#region Purpose
// One PNG the app drew of its own page: bytes, size, route, and what was masked.
#endregion

#region Design
// Immutable. ImageSource (a data: URI for an img src) is built once, because every thumbnail
// render reads it. Path is the normalized route the capture was taken on (PageAgentRoute), so
// "the page behind" Feedback and page_context.screenshot can tell which page it shows.
// The constructor does not throw on null: the generated state cloner (StateTransactionBehavior)
// calls it with default arguments and then copies every field from the source. Selector is null for the page body.
// FileName is unique per capture: Feedback matches it against its draft attachments to know
// whether this capture is already attached.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>A capture taken by ScreenshotState.CaptureAsync.</summary>
public sealed class ScreenshotCapture
{
  public ScreenshotCapture
  (
    int id,
    byte[] png,
    int width,
    int height,
    string path,
    string? selector,
    int masked,
    DateTimeOffset capturedAt
  )
  {
    Id = id;
    Png = png;
    Width = width;
    Height = height;
    Path = path;
    Selector = selector;
    Masked = masked;
    CapturedAt = capturedAt;
    ImageSource = png is null ? "" : $"data:{ScreenshotState.ContentType};base64,{Convert.ToBase64String(png)}";
  }

  public int Id { get; }

  public byte[] Png { get; }

  public int Width { get; }

  public int Height { get; }

  public string Path { get; }

  public string? Selector { get; }

  public int Masked { get; }

  public DateTimeOffset CapturedAt { get; }

  public string ImageSource { get; }

  /// <summary>The attachment name Feedback uploads it under; the id keeps two captures in one second apart.</summary>
  public string FileName => $"screenshot-{CapturedAt.UtcDateTime:yyyyMMdd'T'HHmmss'Z'}-{Id}.png";

  /// <summary>Short text for a thumbnail caption: size and route.</summary>
  public string Caption => $"{Width}×{Height} of {Path}";
}
