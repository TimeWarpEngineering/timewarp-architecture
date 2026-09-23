#region Purpose
// Root partial for ToastNotificationState: holds shell message bars for pipeline failures.
#endregion

#region Design
// The shell renders FluentMessageBar from this list. Handlers append here instead of calling
// INotificationService, which throws FluentServiceProviderException unless a provider is in
// the render tree — headless SPA tests have no provider.
// Namespace is Features substrate (not a product slice): DefaultApiHandler and other base
// handlers publish failures here, so every product may depend on it without TWA0009 opt-outs.
// Section and Card have no error slot; the shell host is the application surface.
// Initialize clears the list so a reset drops stale bars.
#endregion

namespace TimeWarp.Architecture.Features;

[StateAccess]
public sealed partial class ToastNotificationState : State<ToastNotificationState>
{
  private List<NotificationMessage> MessageBarList { get; set; } = [];

  public IReadOnlyList<NotificationMessage> Messages => MessageBarList;

  public sealed record NotificationMessage(string Id, MessageBarIntent Intent, string Title, string? Body);

  public ToastNotificationState()
  {
    Initialize();
  }

  public sealed override void Initialize()
  {
    MessageBarList = [];
  }

  internal void AddMessage(MessageBarIntent intent, string title, string? body)
  {
    MessageBarList.Add(new NotificationMessage(Guid.NewGuid().ToString("N"), intent, title, body));
  }

  internal void RemoveMessage(string id)
  {
    MessageBarList.RemoveAll(message => message.Id == id);
  }
}
