namespace TimeWarp.Architecture.Components;

public partial class SideBarPage: BaseComponent
{
  [Parameter] public RenderFragment HeaderContent { get; set; }
  [Parameter] public RenderFragment MainContent { get; set; }
  [Parameter] public RenderFragment SideBarContent { get; set; }
  [Parameter] public RenderFragment ModalContent { get; set; }
  [Parameter] public RenderFragment CustomSiteFooterContent { get; set; }
  [Parameter] public bool ShowNavBar { get; set; } = true;
  [Parameter] public bool ShowFooter { get; set; } = true;
  protected string ActiveModalId => ApplicationState.ActiveModalId;

}
