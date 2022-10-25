namespace TimeWarp.Architecture.Components;

using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture.Features;

public partial class SiteFooter : BaseComponent
{
  [Parameter] public RenderFragment SiteFooterContent { get; set; }
  private string Version => ApplicationState.Version;
  private bool IsProcessing => ApplicationState.IsProcessing;
}
