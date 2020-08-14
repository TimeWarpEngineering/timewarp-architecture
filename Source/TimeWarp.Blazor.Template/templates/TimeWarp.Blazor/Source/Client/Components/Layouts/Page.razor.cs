namespace TimeWarp.Blazor.Components
{
  using Microsoft.AspNetCore.Components;
  using TimeWarp.Blazor.Features.Bases;

  public partial class Page : BaseComponent
  {
    [Parameter] public RenderFragment HeaderContent { get; set; }
    [Parameter] public RenderFragment MainContent { get; set; }

    private string Version => ApplicationState.Version;
  }
}
