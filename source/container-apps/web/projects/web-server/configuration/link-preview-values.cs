#region Purpose
// The absolute link-preview values one request renders into the document head.
#endregion

namespace TimeWarp.Architecture.Configuration;

internal sealed class LinkPreviewValues
{
  public required string Description { get; init; }
  public required string SiteName { get; init; }
  public required string Title { get; init; }
  public required Uri PageUrl { get; init; }
  public required Uri ImageUrl { get; init; }
  public required string ImageAlt { get; init; }
  public required string ImageType { get; init; }
  public required int ImageWidth { get; init; }
  public required int ImageHeight { get; init; }
}
