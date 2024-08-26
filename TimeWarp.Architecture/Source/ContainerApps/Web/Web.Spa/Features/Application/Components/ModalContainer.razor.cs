namespace TimeWarp.Architecture.Components;

partial class ModalContainer
{
  [CascadingParameter, EditorRequired] private ModalController Parent { get; set; } = default!;
  [Parameter, EditorRequired] public RenderFragment MainContent { get; set; } = default!;
  [Parameter] public RenderFragment? ActionContent { get; set; } = default!;
  [Parameter, EditorRequired] public string ModalId { get; set; } = default!;
  [Parameter] public EventCallback OnActivate { get; set; }
  private bool IsActive => Parent.ActiveModalId == ModalId;
  private Task CloseModal() => Send(new ApplicationState.CloseModal.Action());

  protected override void OnInitialized()
  {
    if (Parent == null)
      throw new ArgumentNullException
      (
      nameof(Parent),
      $"{nameof(ModalContainer)} must exist within a {nameof(ModalController)} Component"
      );

    if (!OnActivate.HasDelegate)
      throw new ArgumentNullException
      (
      nameof(OnActivate),
      $"{nameof(OnActivate)} is required"
      );

    base.OnInitialized();
    Parent.AddModal(this);
  }
}
