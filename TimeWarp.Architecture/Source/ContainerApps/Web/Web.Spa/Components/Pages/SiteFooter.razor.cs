namespace TimeWarp.Architecture.Components.Pages;

public partial class SiteFooter : BaseComponent
{
  [Parameter] public RenderFragment? CustomContent { get; set; }
  private string? Version => ApplicationState.Version;
  private bool IsActive => ActionTrackingState.IsActive;

  private async Task ShowAssemblyInfoModal() =>
    await Send(new ApplicationState.SetActiveModal.Action(ModalId: AssemblyInfoModal.ModalId));
}
