#region Purpose
// Registers the Chat route and authorize policy; markup and behavior live in ChatPage.razor.
#endregion

#region Design
// The page owns the SignalR connection lifecycle — connect in OnInitializedAsync,
// dispose on page teardown — so the socket exists only while chat is in use.
// Sends go through the ChatState action rather than ChatHubConnection directly, so
// the message renders only after the server broadcast; the page never mutates the
// transcript itself.
#endregion

namespace TimeWarp.Architecture.Features.Chat;

[Page("/Chat", Policy = PermissionIds.DeveloperAccess)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
partial class ChatPage;
