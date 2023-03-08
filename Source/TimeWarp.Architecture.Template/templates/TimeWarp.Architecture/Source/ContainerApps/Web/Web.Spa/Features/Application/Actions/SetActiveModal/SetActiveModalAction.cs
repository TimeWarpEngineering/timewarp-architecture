namespace TimeWarp.Architecture.Features.Applications;

internal partial class ApplicationState
{
    internal record SetActiveModalAction(string ModalId) : BaseAction;
}
