#region Purpose
// Reads an upload stream under the size cap and checks magic bytes for the claimed type.
#endregion

#region Design
// FluentValidation must not read the stream: a non-seekable body can be read once.
// The handler calls ReadAsync, then stores the bytes. Oversize is 413, an empty body is
// 400, and a type outside the allow-list or a body that does not match that type is 415.
// text/plain rejects a leading NUL and a body that starts with html, script, or svg markup.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Application;

using System.Text;
using TimeWarp.Architecture.Features.Feedback;

/// <summary>Bounded read and magic-byte check for a feedback attachment.</summary>
public static class FeedbackAttachmentContent
{
  /// <summary>Reads at most <see cref="FeedbackAttachmentRules.MaxBytes"/> and checks the type.</summary>
  public static async Task<OneOf<byte[], SharedProblemDetails>> ReadAsync(
    Stream content,
    string? contentType,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(content);
    string? canonical = FeedbackAttachmentRules.CanonicalContentType(contentType);
    if (canonical is null)
    {
      return FeedbackProblems.UnsupportedMedia();
    }

    await using MemoryStream memory = new();
    byte[] buffer = new byte[81920];
    long total = 0;
    while (true)
    {
      int read = await content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
      if (read == 0)
      {
        break;
      }

      total += read;
      if (total > FeedbackAttachmentRules.MaxBytes)
      {
        return FeedbackProblems.TooLarge();
      }

      await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
    }

    if (total == 0)
    {
      return FeedbackProblems.EmptyFile();
    }

    byte[] bytes = memory.ToArray();
    if (!Matches(canonical, bytes))
    {
      return FeedbackProblems.UnsupportedMedia();
    }

    return bytes;
  }

  /// <summary>True when <paramref name="body"/> matches <paramref name="contentType"/>.</summary>
  public static bool Matches(string contentType, ReadOnlySpan<byte> body)
  {
    if (body.IsEmpty)
    {
      return false;
    }

    return contentType switch
    {
      FeedbackAttachmentRules.Png => body.Length >= 4
        && body[0] == 0x89 && body[1] == 0x50 && body[2] == 0x4E && body[3] == 0x47,
      FeedbackAttachmentRules.Jpeg => body.Length >= 3
        && body[0] == 0xFF && body[1] == 0xD8 && body[2] == 0xFF,
      FeedbackAttachmentRules.Gif => body.Length >= 6
        && body[0] == (byte)'G' && body[1] == (byte)'I' && body[2] == (byte)'F'
        && body[3] == (byte)'8' && (body[4] == (byte)'7' || body[4] == (byte)'9') && body[5] == (byte)'a',
      FeedbackAttachmentRules.Webp => body.Length >= 12
        && body[0] == (byte)'R' && body[1] == (byte)'I' && body[2] == (byte)'F' && body[3] == (byte)'F'
        && body[8] == (byte)'W' && body[9] == (byte)'E' && body[10] == (byte)'B' && body[11] == (byte)'P',
      FeedbackAttachmentRules.Pdf => body.Length >= 4
        && body[0] == (byte)'%' && body[1] == (byte)'P' && body[2] == (byte)'D' && body[3] == (byte)'F',
      FeedbackAttachmentRules.Text => IsPlainText(body),
      _ => false,
    };
  }

  private static bool IsPlainText(ReadOnlySpan<byte> body)
  {
    if (body.IndexOf((byte)0) >= 0)
    {
      return false;
    }

    ReadOnlySpan<byte> payload = body;
    if (payload.Length >= 3 && payload[0] == 0xEF && payload[1] == 0xBB && payload[2] == 0xBF)
    {
      payload = payload[3..];
    }

    int length = Math.Min(payload.Length, 512);
    string prefix = Encoding.UTF8.GetString(payload[..length]).TrimStart();
    return !prefix.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
      && !prefix.StartsWith("<script", StringComparison.OrdinalIgnoreCase)
      && !prefix.StartsWith("<svg", StringComparison.OrdinalIgnoreCase);
  }
}
