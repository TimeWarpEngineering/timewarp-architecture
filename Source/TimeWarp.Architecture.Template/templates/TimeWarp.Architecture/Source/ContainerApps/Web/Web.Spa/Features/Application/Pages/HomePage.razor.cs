namespace TimeWarp.Architecture.Pages;

[UsedImplicitly]
[Page("/")]
public partial class HomePage : BaseComponent
{
  private async Task FiveSecondTaskButtonClick() =>
    await Send(new ActionTrackingState.FiveSecondTask.Action());

  private async Task TwoSecondTaskButtonClick() =>
    await Send(new ActionTrackingState.TwoSecondTask.Action());

  private async Task ModalButtonClick() =>
    await Send(new ApplicationState.SetActiveModal.Action(ModalId: AssemblyInfoModal.ModalId));
}
