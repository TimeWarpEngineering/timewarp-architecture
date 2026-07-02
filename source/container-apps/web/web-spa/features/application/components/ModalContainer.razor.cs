#region Purpose
// Code-behind for ModalContainer: registers a modal with its parent ModalController and derives visibility from state.
#endregion

#region Design
// Visibility derives from Parent.ActiveModalId (fed by ApplicationState), so opening and
// closing modals is store-driven rather than component-local.
// OnActivate is mandatory so modal content is loaded only when the modal actually opens,
// not when the container renders.
// Explicit throws in OnInitialized turn missing cascade/callback wiring into clear errors
// instead of NullReferenceExceptions at render time.
#endregion

namespace TimeWarp.Architecture.Components;

partial class ModalContainer
{
  [CascadingParameter, EditorRequired] private ModalController Parent { get; set; } = default!;
  [Parameter, EditorRequired] public RenderFragment MainContent { get; set; } = default!;
  [Parameter] public RenderFragment? ActionContent { get; set; } = default!;
  [Parameter, EditorRequired] public string ModalId { get; set; } = default!;
  [Parameter] public EventCallback OnActivate { get; set; }
  private bool IsActive => Parent.ActiveModalId == ModalId;
  private Task CloseModal() => ApplicationState.CloseModal();

  protected override void OnInitialized()
  {
    if (Parent == null)
    {
      throw new ArgumentNullException
      (
        nameof(Parent),
        $"{nameof(ModalContainer)} must exist within a {nameof(ModalController)} Component"
      );
    }

    if (!OnActivate.HasDelegate)
    {
      throw new ArgumentNullException
      (
        nameof(OnActivate),
        $"{nameof(OnActivate)} is required"
      );
    }

    base.OnInitialized();
    Parent.AddModal(this);
  }
}
