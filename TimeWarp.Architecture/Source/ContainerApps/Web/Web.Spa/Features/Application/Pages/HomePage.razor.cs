namespace TimeWarp.Architecture.Pages;

[UsedImplicitly]
[Page("/")]
partial class HomePage
{
  [Inject]
  private ILogger<HomePage> Logger { get; set; } = null!;

  private async Task FiveSecondTaskButtonClick() =>
    await ApplicationState.FiveSecondTask(CancellationToken.None);

  private async Task TwoSecondTaskButtonClick() =>
    await ApplicationState.TwoSecondTask(CancellationToken.None);

  private async Task ModalButtonClick() =>
    await Send(new ApplicationState.SetActiveModal.Action(ModalId: AssemblyInfoModal.ModalId));

  protected override void OnInitialized()
  {
    base.OnInitialized();
    Logger.LogDebug("This is a debug message");
    Logger.LogInformation("This is an info message");
    Logger.LogWarning("This is a warning message");
    // Logger.LogError("This is an error message");
    // Logger.LogCritical("This is a critical message");
  }
}
