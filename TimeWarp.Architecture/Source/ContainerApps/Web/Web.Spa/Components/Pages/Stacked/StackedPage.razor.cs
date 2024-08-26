#nullable enable
namespace TimeWarp.Architecture.Components;

partial class StackedPage
{
  [Parameter] public RenderFragment? HeaderContent { get; set; }
  [Parameter] public RenderFragment? MainContent { get; set; }
  [Parameter] public RenderFragment? ModalContent { get; set; }
  [Parameter] public RenderFragment? CustomSiteFooterContent { get; set; }
  [Parameter] public bool ShowNavBar { get; set; } = true;
  [Parameter] public bool ShowFooter { get; set; } = true;

  private string? ActiveModalId => ApplicationState.ActiveModalId;
  private string? Version => ApplicationState.Version;
}
