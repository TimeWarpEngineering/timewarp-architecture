namespace TimeWarp.Architecture.Components;

using Microsoft.AspNetCore.Components;
using TimeWarp.Architecture.Features;

public partial class Page : BaseComponent
{
  [Parameter] public RenderFragment HeaderContent { get; set; }
  [Parameter] public RenderFragment MainContent { get; set; }
  [Parameter] public RenderFragment SiteFooterContent { get; set; }
  [Parameter] public bool ShowNavBar { get; set; } = true;
  [Parameter] public bool ShowFooter { get; set; } = true;

  private string Version => ApplicationState.Version;
}
