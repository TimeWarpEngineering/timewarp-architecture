#region Purpose
// On-demand import of screenshot.js: draws the page body or one element to PNG bytes.
#endregion

#region Design
// [SideEffectService]: the only caller is ScreenshotState.CaptureAsync (the CaptureScreenshot and
// CaptureForAsk handlers), so no component or other service captures the page. The JS returns
// the size and a token, then hands the PNG over as an IJSStreamReference read with
// maxAllowedSize = maxBytes, the same reason feedback paste streams: one SignalR message on
// InteractiveServer has a size cap.
// A refusal from the browser (no element, a bad selector, a PNG that cannot fit maxBytes, a
// tainted canvas) comes back as Error with no bytes. A missing runtime or a disconnected circuit
// is Error "unavailable".
#endregion

namespace TimeWarp.Architecture.Services;

[SideEffectService]
internal static class ScreenshotJsModule
{
  internal const string Specifier = "./js/features/screenshot.js";
  internal const string CaptureExport = "capture";
  internal const string TakeBytesExport = "takeBytes";
  internal const string Unavailable = "unavailable";

  /// <summary>What the browser drew, or the reason it did not.</summary>
  internal sealed record Result(byte[] Png, int Width, int Height, int Masked, string? Error);

  internal sealed class BrowserCapture
  {
    public string Token { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public long ByteLength { get; set; }
    public int Masked { get; set; }
    public string? Error { get; set; }
  }

  internal static async Task<Result> CaptureAsync
  (
    IJSRuntime? jsRuntime,
    string selector,
    int maxBytes,
    CancellationToken cancellationToken
  )
  {
    if (jsRuntime is null)
    {
      return Failed(Unavailable);
    }

    IJSObjectReference? module = null;
    try
    {
      module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, Specifier);
      BrowserCapture? capture = await module.InvokeAsync<BrowserCapture?>(
        CaptureExport,
        cancellationToken,
        selector,
        maxBytes);
      if (capture is null)
      {
        return Failed(Unavailable);
      }

      if (capture.Error is not null || capture.Token.Length == 0)
      {
        return Failed(capture.Error ?? Unavailable);
      }

      if (capture.ByteLength <= 0 || capture.ByteLength > maxBytes)
      {
        return Failed("too-large");
      }

      IJSStreamReference stream = await module.InvokeAsync<IJSStreamReference>(
        TakeBytesExport,
        cancellationToken,
        capture.Token);
      await using (stream)
      {
        await using Stream read = await stream.OpenReadStreamAsync(maxAllowedSize: maxBytes, cancellationToken);
        await using MemoryStream buffer = new();
        await read.CopyToAsync(buffer, cancellationToken);
        byte[] png = buffer.ToArray();
        return png.Length == 0
          ? Failed(Unavailable)
          : new Result(png, capture.Width, capture.Height, capture.Masked, Error: null);
      }
    }
    catch (Exception exception) when (exception is JSException or JSDisconnectedException or InvalidOperationException)
    {
      return Failed(Unavailable);
    }
    finally
    {
      if (module is not null)
      {
        try
        {
          await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
          // The circuit is already gone.
        }
      }
    }
  }

  private static Result Failed(string error) => new([], 0, 0, 0, error);
}
