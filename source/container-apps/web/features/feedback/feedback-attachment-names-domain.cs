#region Purpose
// Turns a client file name into the single path segment stored and downloaded.
#endregion

#region Design
// The download Content-Disposition is built from this string. Directory segments, controls,
// and quote characters are rejected so a name cannot escape the header or the storage key.
// A name longer than FeedbackAttachment.MaxFileNameLength is truncated. An empty result is
// invalid. Paste supplies pasted-image.png when the clipboard name is empty; that default
// lives in the browser, not here.
#endregion

namespace TimeWarp.Architecture.Features.Feedback.Domain;

/// <summary>Normalizes a feedback attachment file name.</summary>
public static class FeedbackAttachmentNames
{
  /// <summary>The stored file name, or null when <paramref name="fileName"/> cannot be stored.</summary>
  public static string? Normalize(string? fileName)
  {
    if (string.IsNullOrWhiteSpace(fileName))
    {
      return null;
    }

    string trimmed = fileName.Trim();
    int slash = Math.Max(
      trimmed.LastIndexOf('/', StringComparison.Ordinal),
      trimmed.LastIndexOf('\\', StringComparison.Ordinal));
    string segment = slash >= 0 ? trimmed[(slash + 1)..] : trimmed;
    if (string.IsNullOrWhiteSpace(segment))
    {
      return null;
    }

    foreach (char character in segment)
    {
      if (char.IsControl(character) || character is '"' or '\'' or '`' or ';')
      {
        return null;
      }
    }

    if (segment.Length > FeedbackAttachment.MaxFileNameLength)
    {
      segment = segment[..FeedbackAttachment.MaxFileNameLength];
    }

    return segment;
  }
}
