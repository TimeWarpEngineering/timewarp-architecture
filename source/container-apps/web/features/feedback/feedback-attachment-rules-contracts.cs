#region Purpose
// Shared size and content-type limits for feedback attachment contracts.
#endregion

#region Design
// Contracts must not reference domain, so these literals are duplicated on
// FeedbackAttachment in the domain. A unit test locks the two copies together.
// Five mebibytes and eight files per item bound memory on the in-memory blob and on the
// browser draft. SVG and HTML are refused so a download cannot become active content.
// The served Content-Type is always one of AllowedContentTypes, never the request header.
// PendingLifetime bounds an upload that is never filed: the draft list lives only in the
// browser, so a reload loses it. Upload deletes the caller's own unlinked files older than
// this before it counts against MaxPerItem, which keeps a person from locking themselves out.
#endregion

namespace TimeWarp.Architecture.Features.Feedback;

/// <summary>Size and media-type limits for a feedback attachment.</summary>
public static class FeedbackAttachmentRules
{
  /// <summary>Maximum accepted file size, in bytes.</summary>
  public const int MaxBytes = 5 * 1024 * 1024;

  /// <summary>Maximum file name length after normalization.</summary>
  public const int MaxFileNameLength = 200;

  /// <summary>Maximum attachments filed on one feedback item.</summary>
  public const int MaxPerItem = 8;

  /// <summary>How long an unlinked upload is kept before the owner's next upload deletes it.</summary>
  public static readonly TimeSpan PendingLifetime = TimeSpan.FromHours(24);

  /// <summary>PNG image.</summary>
  public const string Png = "image/png";

  /// <summary>JPEG image.</summary>
  public const string Jpeg = "image/jpeg";

  /// <summary>GIF image.</summary>
  public const string Gif = "image/gif";

  /// <summary>WebP image.</summary>
  public const string Webp = "image/webp";

  /// <summary>PDF document.</summary>
  public const string Pdf = "application/pdf";

  /// <summary>Plain text.</summary>
  public const string Text = "text/plain";

  /// <summary>Media types a feedback attachment may use.</summary>
  public static readonly string[] AllowedContentTypes = [Png, Jpeg, Gif, Webp, Pdf, Text];

  /// <summary>The allow-list spelling of <paramref name="contentType"/>, or null.</summary>
  public static string? CanonicalContentType(string? contentType)
  {
    if (string.IsNullOrWhiteSpace(contentType))
    {
      return null;
    }

    string trimmed = contentType.Trim();
    foreach (string allowed in AllowedContentTypes)
    {
      if (string.Equals(allowed, trimmed, StringComparison.OrdinalIgnoreCase))
      {
        return allowed;
      }
    }

    return null;
  }
}
