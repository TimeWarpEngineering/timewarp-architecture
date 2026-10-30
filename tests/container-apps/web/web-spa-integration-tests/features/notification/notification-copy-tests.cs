#region Purpose
// Task 313: CopyMessage writes a bar's text to the clipboard, confirms with an icon swap, and
// reports a clipboard failure as a Warning bar; the MessageBars Copy button dispatches it.
#endregion

#region Design
// C-create in-proc ServiceProvider (TimeWarp.State + ClientPipeline), same shape as
// SignOut_Should_: the closed-box Aspire host cannot observe IJSRuntime calls. The clipboard is
// an externality, so IJSRuntime is a recording fake (import → module → "WriteText"); throwing a
// JSException from the module stands in for a denied clipboard, and a TaskCanceledException
// for a JS-interop timeout.
// The dispatch test renders MessageBars on ClickRenderer, a minimal Renderer that records each
// element's onclick handler id by data-qa and fires it with DispatchEventAsync (no bUnit in this
// repo). Clicking MessageBarCopy must reach the fake clipboard through the real pipeline, which
// proves the button dispatches CopyMessage rather than calling the clipboard itself.
// The doubles live in notification-copy-test-doubles.cs, shared with CopyMessageBar_Should_.
#endregion

namespace NotificationCopy_;

using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Services;
using static TimeWarp.Architecture.Features.NotificationState;

[TestTag("Integration")]
public class CopyMessage_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CopyMessage_Should_>();

  public static Task Format_Title_Only_When_There_Is_No_Body()
  {
    NotificationMessage message = new("id", MessageBarIntent.Error, "Passkey rejected.", null, null);

    ClipboardText(message).ShouldBe("Passkey rejected.");
    return Task.CompletedTask;
  }

  public static Task Format_Title_Blank_Line_Body()
  {
    NotificationMessage message =
      new("id", MessageBarIntent.Error, "Already on this account", "This passkey is already on this account.", null);

    ClipboardText(message).ShouldBe("Already on this account\n\nThis passkey is already on this account.");
    return Task.CompletedTask;
  }

  public static async Task Write_Title_And_Body_To_Clipboard_And_Confirm_Until_Expiry()
  {
    using CopySpa spa = new(clipboardThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AddNotificationActionSet.Action(MessageBarIntent.Error, "Save failed", "The server is unavailable."));
    string id = scope.Store.GetState<NotificationState>().Messages[0].Id;

    await scope.Send(new CopyMessageActionSet.Action(id));

    spa.JsRuntime.ImportedSpecifier.ShouldBe(ClipboardJsModule.Specifier);
    spa.JsRuntime.Module.Calls.ShouldBe([(ClipboardJsModule.WriteTextExport, "Save failed\n\nThe server is unavailable.")]);
    NotificationState state = scope.Store.GetState<NotificationState>();
    state.CopiedMessageId.ShouldBe(id);
    state.CopiedUntil.ShouldNotBeNull();
    state.NextExpiry.ShouldBe(state.CopiedUntil);
    state.Messages.Count.ShouldBe(1, "a successful copy adds no bar");

    // The host's ExpireMessages timer ends the confirmation; the Error bar itself stays.
    await scope.Send(new ExpireMessagesActionSet.Action(state.CopiedUntil.Value + TimeSpan.FromSeconds(1)));
    state = scope.Store.GetState<NotificationState>();
    state.CopiedMessageId.ShouldBeNull();
    state.NextExpiry.ShouldBeNull();
    state.Messages.Count.ShouldBe(1);
  }

  public static async Task Report_Warning_When_Clipboard_Rejects()
  {
    using CopySpa spa = new(clipboardThrows: true);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AddNotificationActionSet.Action(MessageBarIntent.Error, "Save failed"));
    string id = scope.Store.GetState<NotificationState>().Messages[0].Id;

    await scope.Send(new CopyMessageActionSet.Action(id));

    spa.JsRuntime.Module.Calls.Count.ShouldBe(1);
    NotificationState state = scope.Store.GetState<NotificationState>();
    state.CopiedMessageId.ShouldBeNull();
    state.Messages.Count.ShouldBe(2);
    NotificationMessage warning = state.Messages.Single(message => message.Intent == MessageBarIntent.Warning);
    warning.Title.ShouldBe(CopyFailedTitle);
    warning.Body.ShouldBe(ClipboardJsModule.DeniedBody);
  }

  public static async Task Report_Warning_When_Clipboard_Times_Out()
  {
    using CopySpa spa = new(clipboardThrows: false, clipboardTimesOut: true);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AddNotificationActionSet.Action(MessageBarIntent.Error, "Save failed"));
    string id = scope.Store.GetState<NotificationState>().Messages[0].Id;

    await scope.Send(new CopyMessageActionSet.Action(id));

    NotificationState state = scope.Store.GetState<NotificationState>();
    state.CopiedMessageId.ShouldBeNull();
    state.Messages.Count.ShouldBe(2, "a timeout is a Warning, not a pipeline Error bar");
    state.Messages.Single(message => message.Intent == MessageBarIntent.Warning).Title.ShouldBe(CopyFailedTitle);
    state.Messages.ShouldNotContain(message => message.Intent == MessageBarIntent.Error && message.Title != "Save failed");
  }

  public static async Task Ignore_Unknown_Id()
  {
    using CopySpa spa = new(clipboardThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new CopyMessageActionSet.Action("gone"));

    spa.JsRuntime.Module.Calls.ShouldBeEmpty();
    scope.Store.GetState<NotificationState>().Messages.ShouldBeEmpty();
  }

  public static async Task Copy_Button_Dispatches_CopyMessage()
  {
    using CopySpa spa = new(clipboardThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new AddNotificationActionSet.Action(MessageBarIntent.Info, "Heads up", "Details here."));

    await using ClickRenderer renderer = new(scope.ServiceProvider);
    await renderer.RenderAsync<MessageBars>();

    renderer.Labels["MessageBarCopy"].ShouldBe("Copy message");
    spa.JsRuntime.Module.Calls.ShouldBeEmpty();

    await renderer.ClickAsync("MessageBarCopy");

    spa.JsRuntime.Module.Calls.ShouldBe([(ClipboardJsModule.WriteTextExport, "Heads up\n\nDetails here.")]);
    scope.Store.GetState<NotificationState>().CopiedMessageId.ShouldNotBeNull();

    // A diff batch carries only changed attributes, so paint a fresh host to read the swapped label.
    await using ClickRenderer fresh = new(scope.ServiceProvider);
    await fresh.RenderAsync<MessageBars>();
    fresh.Labels["MessageBarCopy"].ShouldBe("Copied", "the copied row swaps to the Copied confirmation");
  }
}
