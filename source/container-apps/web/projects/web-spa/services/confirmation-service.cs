#region Purpose
// Asks before a destructive action through FluentUI's dialog. Handlers depend on this, not on FluentUI types.
#endregion

#region Design
// FluentUI 5.0.1, from the package XML docs and DialogService / MessageBoxOptions / FluentDialog:
// IDialogService.ShowConfirmationAsync(message, title, primaryButton, secondaryButton) returns
// DialogResult directly. There is no IDialogReference and no dialog.Result step. Yes is
// Cancelled == false. That method sets PrimaryShortCut to "Enter;Y" and SecondaryShortCut to
// "Escape;N", so Enter confirms. MessageBoxOptions.PrimaryShortCut defaults to "Enter" and
// SecondaryShortCut defaults to "Escape". DialogOptionsFooterAction does the same for a primary
// button. FluentDialog.OnKeyDownHandlerAsync ignores a null or empty ShortCut, and
// ShowMessageBoxAsync assigns the options onto the footer, replacing those defaults.
// This wrapper calls IDialogService.ShowMessageBoxAsync(MessageBoxOptions) with PrimaryShortCut
// "" and SecondaryShortCut "Escape". Enter does not take the primary action. Escape calls
// CancelAsync, so Cancelled is true. Confirmed is a result that is not cancelled. The question
// icon matches ShowConfirmationAsync. No tone enum: every confirm through this service uses that
// shape.
// [SideEffectService] on the interface: a component cannot call it (TWA0026). Handlers do.
#endregion

namespace TimeWarp.Architecture.Services;

/// <summary>Yes/no question a handler asks before it does something destructive.</summary>
[SideEffectService]
internal interface IConfirmationService
{
  /// <summary>True when the person chooses the confirm label. False on cancel or Escape.</summary>
  Task<bool> ConfirmAsync
  (
    string title,
    string message,
    string confirmLabel,
    string cancelLabel,
    CancellationToken cancellationToken
  );
}

/// <summary>FluentUI message box. Enter does not confirm. Escape cancels.</summary>
internal sealed class DialogConfirmationService(IDialogService DialogService) : IConfirmationService
{
  /// <inheritdoc />
  public async Task<bool> ConfirmAsync
  (
    string title,
    string message,
    string confirmLabel,
    string cancelLabel,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(title);
    ArgumentNullException.ThrowIfNull(message);
    ArgumentNullException.ThrowIfNull(confirmLabel);
    ArgumentNullException.ThrowIfNull(cancelLabel);
    cancellationToken.ThrowIfCancellationRequested();

    DialogResult result = await DialogService.ShowMessageBoxAsync(new MessageBoxOptions
    {
      Title = title,
      Message = message,
      PrimaryButton = confirmLabel,
      SecondaryButton = cancelLabel,
      // Empty replaces the v5 default "Enter". FluentDialog ignores an empty shortcut.
      PrimaryShortCut = "",
      SecondaryShortCut = "Escape",
      Icon = new Icons.Regular.Size20.QuestionCircle(),
      IconColor = Color.Default,
    });

    return result is { Cancelled: false };
  }
}
