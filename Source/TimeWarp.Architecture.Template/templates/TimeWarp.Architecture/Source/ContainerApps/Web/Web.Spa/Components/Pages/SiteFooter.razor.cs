namespace TimeWarp.Architecture.Components;

public partial class SiteFooter : BaseComponent
{
  [Parameter] public RenderFragment SiteFooterContent { get; set; }
  private string Version => ApplicationState.Version;
  private bool IsProcessing => ApplicationState.IsProcessing;
}
