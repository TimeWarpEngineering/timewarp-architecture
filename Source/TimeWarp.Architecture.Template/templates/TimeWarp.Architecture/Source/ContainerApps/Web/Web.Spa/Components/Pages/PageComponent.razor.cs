namespace TimeWarp.Architecture.Components;

public partial class PageComponent : BaseComponent
{
  [Parameter] public RenderFragment HeaderContent { get; set; }
  [Parameter] public RenderFragment MainContent { get; set; }
  [Parameter] public RenderFragment SiteFooterContent { get; set; }
  [Parameter] public bool ShowNavBar { get; set; } = true;
  [Parameter] public bool ShowFooter { get; set; } = true;

  private string Version => ApplicationState.Version;
}
