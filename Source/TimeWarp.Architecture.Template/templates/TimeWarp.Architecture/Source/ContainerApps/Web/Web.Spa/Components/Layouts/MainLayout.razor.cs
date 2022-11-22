namespace TimeWarp.Architecture.Components;

public partial class MainLayout : LayoutComponentBase
{
  protected const string HeadingHeight = "52px";
  [Inject] public BlazorHostingLocation BlazorHostingLocation { get; set; }

  [Parameter] public RenderFragment HeaderTemplate { get; set; }
}
