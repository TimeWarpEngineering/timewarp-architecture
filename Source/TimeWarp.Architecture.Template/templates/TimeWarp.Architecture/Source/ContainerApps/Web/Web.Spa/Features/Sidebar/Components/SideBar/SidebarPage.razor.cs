namespace TimeWarp.Architecture.Components;

public partial class SidebarPage: BaseComponent
{
  [Parameter] public RenderFragment? HeaderContent { get; set; }
  [Parameter, EditorRequired] public RenderFragment MainContent { get; set; } = default!;
  // [Parameter] public RenderFragment? SideBarContent { get; set; }
  [Parameter] public RenderFragment? ModalContent { get; set; }
  // [Parameter] public RenderFragment? CustomSiteFooterContent { get; set; }
  // [Parameter] public bool ShowNavBar { get; set; } = true;
  // [Parameter] public bool ShowFooter { get; set; } = true;
  private string? ActiveModalId => ApplicationState.ActiveModalId;
}
