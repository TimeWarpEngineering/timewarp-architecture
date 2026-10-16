#region Purpose
// One PNG the app drew of its own page: bytes, size, route, and what was masked.
#endregion

#region Design
// Immutable. ImageSource (a data: URI for an img src) is built once, because every thumbnail
// render reads it. Path is the normalized route the capture was taken on (PageAgentRoute), so
// "the page behind" Feedback and page_context.screenshot can tell which page it shows. Selector is null for the page body.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>A capture taken by ScreenshotState.CaptureScreenshot.</summary>
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
    ArgumentNullException.ThrowIfNull(png);
    ArgumentNullException.ThrowIfNull(path);
    Id = id;
    Png = png;
    Width = width;
    Height = height;
    Path = path;
    Selector = selector;
    Masked = masked;
    CapturedAt = capturedAt;
    ImageSource = $"data:{ScreenshotState.ContentType};base64,{Convert.ToBase64String(png)}";
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

  /// <summary>The attachment name Feedback uploads it under.</summary>
  public string FileName => $"screenshot-{CapturedAt.UtcDateTime:yyyyMMdd-HHmmss}.png";

  /// <summary>Short text for a thumbnail caption: size and route.</summary>
  public string Caption => $"{Width}×{Height} of {Path}";
}
