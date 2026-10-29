#region Purpose
// Task 445: every page-local message bar copies through the shared MessageBar control.
// CopyMessageBar writes the bar's text, confirms with the "Copied" icon swap, and reports a
// clipboard failure as a Warning bar; the control's Copy button dispatches it.
#endregion

#region Design
// Same C-create in-proc shape and doubles as CopyMessage_Should_ (notification-copy-test-
// doubles.cs). The control test renders MessageBar with ChildContent (an input standing in for
// the passkey nickname draft) and ActionsTemplate (a Save button), so it proves Copy writes only
// Title and Body, keeps the bar's own action working, and swaps its label to "Copied" (read from
// the diff, because a fresh host would be a new bar with a new id). The design-system test
// clicks each of MessageBarsSample's four Copy buttons and checks each copies its own bar.
// RemoveExpired clears Copied only when CopiedUntil passes or the copied row itself expires; the
// two expiry tests add a backdated Success row so it dismisses before CopiedUntil and each branch
// is reached on its own.
#endregion

namespace NotificationCopy_;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.DesignSystem;
using TimeWarp.Architecture.Services;
using static TimeWarp.Architecture.Features.NotificationState;

[TestTag("Integration")]
public class CopyMessageBar_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CopyMessageBar_Should_>();

  private const string DriftTitle = "Configuration drift";
  private const string DriftBody = "Saved policy differs from SignInOptions:Methods (Microsoft365).";

  public static Task Format_Title_Only_When_Body_Is_Empty()
  {
    ClipboardText("Name this passkey", string.Empty).ShouldBe("Name this passkey");
    ClipboardText("Name this passkey", "  ").ShouldBe("Name this passkey");
    ClipboardText("Session expired", "Session expired, sign in with Microsoft 365 again.")
      .ShouldBe("Session expired\n\nSession expired, sign in with Microsoft 365 again.");
    return Task.CompletedTask;
  }

  public static async Task Write_Title_And_Body_To_Clipboard_And_Confirm_Until_Expiry()
  {
    using CopySpa spa = new(clipboardThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new CopyMessageBarActionSet.Action("bar-1", DriftTitle, DriftBody));

    spa.JsRuntime.ImportedSpecifier.ShouldBe(ClipboardJsModule.Specifier);
    spa.JsRuntime.Module.Calls.ShouldBe([(ClipboardJsModule.WriteTextExport, $"{DriftTitle}\n\n{DriftBody}")]);
    NotificationState state = scope.Store.GetState<NotificationState>();
    state.CopiedMessageId.ShouldBe("bar-1");
    state.CopiedUntil.ShouldNotBeNull();
    state.NextExpiry.ShouldBe(state.CopiedUntil);
    state.Messages.ShouldBeEmpty("a successful copy adds no bar");

    await scope.Send(new ExpireMessagesActionSet.Action(state.CopiedUntil.Value + TimeSpan.FromSeconds(1)));
    state = scope.Store.GetState<NotificationState>();
    state.CopiedMessageId.ShouldBeNull();
    state.NextExpiry.ShouldBeNull();
  }

  public static async Task Keep_Confirmation_Through_An_Earlier_Expiry_Pass()
  {
    using CopySpa spa = new(clipboardThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new CopyMessageBarActionSet.Action("bar-1", DriftTitle, DriftBody));
    DateTimeOffset copiedUntil = scope.Store.GetState<NotificationState>().CopiedUntil!.Value;

    // The bar is not a NotificationState row; an expiry pass before CopiedUntil must keep it.
    await scope.Send(new ExpireMessagesActionSet.Action(copiedUntil - TimeSpan.FromSeconds(1)));

    scope.Store.GetState<NotificationState>().CopiedMessageId.ShouldBe("bar-1");
  }

  public static async Task Clear_Confirmation_When_The_Copied_Row_Expires_First()
  {
    using CopySpa spa = new(clipboardThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    NotificationMessage row = AddSuccessRowNearItsExpiry(scope);

    await scope.Send(new CopyMessageActionSet.Action(row.Id));
    NotificationState state = scope.Store.GetState<NotificationState>();
    state.CopiedMessageId.ShouldBe(row.Id);
    row.AutoDismissAt!.Value.ShouldBeLessThan(state.CopiedUntil!.Value, "the row expires while Copied still shows");

    await scope.Send(new ExpireMessagesActionSet.Action(row.AutoDismissAt.Value));

    state = scope.Store.GetState<NotificationState>();
    state.Messages.ShouldBeEmpty();
    state.CopiedMessageId.ShouldBeNull("the copied row is gone, so its confirmation goes with it");
  }

  public static async Task Keep_Page_Bar_Confirmation_When_An_Unrelated_Row_Expires()
  {
    using CopySpa spa = new(clipboardThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new CopyMessageBarActionSet.Action("bar-1", DriftTitle, DriftBody));
    NotificationMessage row = AddSuccessRowNearItsExpiry(scope);
    row.AutoDismissAt!.Value.ShouldBeLessThan(scope.Store.GetState<NotificationState>().CopiedUntil!.Value);

    await scope.Send(new ExpireMessagesActionSet.Action(row.AutoDismissAt.Value));

    NotificationState state = scope.Store.GetState<NotificationState>();
    state.Messages.ShouldBeEmpty("the Success row auto-dismissed");
    state.CopiedMessageId.ShouldBe("bar-1", "another row expiring leaves the page bar's Copied in place");
  }

  public static async Task Report_Warning_When_Clipboard_Rejects()
  {
    using CopySpa spa = new(clipboardThrows: true);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new CopyMessageBarActionSet.Action("bar-1", DriftTitle, DriftBody));

    spa.JsRuntime.Module.Calls.Count.ShouldBe(1);
    NotificationState state = scope.Store.GetState<NotificationState>();
    state.CopiedMessageId.ShouldBeNull();
    NotificationMessage warning = state.Messages.ShouldHaveSingleItem();
    warning.Intent.ShouldBe(MessageBarIntent.Warning);
    warning.Title.ShouldBe(CopyFailedTitle);
    warning.Body.ShouldBe(CopyFailedBody);
  }

  public static async Task Report_Warning_When_Clipboard_Times_Out()
  {
    using CopySpa spa = new(clipboardThrows: false, clipboardTimesOut: true);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new CopyMessageBarActionSet.Action("bar-1", DriftTitle, string.Empty));

    NotificationState state = scope.Store.GetState<NotificationState>();
    state.CopiedMessageId.ShouldBeNull();
    state.Messages.ShouldHaveSingleItem("a timeout is a Warning, not a pipeline Error bar").Title.ShouldBe(CopyFailedTitle);
  }

  public static async Task Copy_Button_Dispatches_CopyMessageBar_Beside_The_Bar_Actions()
  {
    using CopySpa spa = new(clipboardThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    bool saved = false;

    await using ClickRenderer renderer = new(scope.ServiceProvider);
    await renderer.RenderAsync<MessageBar>
    (
      new Dictionary<string, object?>
      {
        [nameof(MessageBar.Intent)] = MessageBarIntent.Info,
        [nameof(MessageBar.Title)] = "Name this passkey",
        [nameof(MessageBar.ChildContent)] = (RenderFragment)(builder =>
        {
          builder.OpenElement(0, "input");
          builder.AddAttribute(1, "value", "My laptop");
          builder.AddAttribute(2, "data-qa", "NicknameDraft");
          builder.CloseElement();
        }),
        [nameof(MessageBar.ActionsTemplate)] = (RenderFragment)(builder => SaveButton(builder, () => saved = true))
      }
    );

    renderer.Labels["MessageBarCopy"].ShouldBe("Copy message");
    renderer.ClickHandlers.ShouldContainKey("Save", "the bar's own action renders beside Copy");

    await renderer.ClickAsync("MessageBarCopy");

    spa.JsRuntime.Module.Calls.ShouldBe([(ClipboardJsModule.WriteTextExport, "Name this passkey")], "the nickname draft is not copied");
    scope.Store.GetState<NotificationState>().CopiedMessageId.ShouldNotBeNull();
    renderer.UpdatedLabels.ShouldContain("Copied", "the bar swaps to the Copied confirmation");

    await renderer.ClickAsync("Save");
    saved.ShouldBeTrue("Copy leaves the bar's own action working");
  }

  public static async Task Copy_Each_Design_System_Sample_Bar()
  {
    using CopySpa spa = new(clipboardThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    string[] expected =
    [
      "Success\n\nOperation completed successfully.",
      "Error\n\nSomething went wrong — you can select and copy this text.",
      "Warning\n\nProceed with caution.",
      "Information\n\nHelpful context that does not require immediate action."
    ];

    for (int index = 0; index < expected.Length; index++)
    {
      await using ClickRenderer renderer = new(scope.ServiceProvider);
      await renderer.RenderAsync<MessageBarsSample>();
      renderer.AllClickHandlers["MessageBarCopy"].Count.ShouldBe(expected.Length);

      await renderer.ClickAsync("MessageBarCopy", index);

      spa.JsRuntime.Module.Calls[^1].ShouldBe((ClipboardJsModule.WriteTextExport, expected[index]));
    }

    spa.JsRuntime.Module.Calls.Count.ShouldBe(expected.Length);
  }

  // A Success row added one second before its auto-dismiss, so it expires before a copy made now
  // stops showing Copied. Backdating AddMessage stands in for waiting out the dismiss interval.
  private static NotificationMessage AddSuccessRowNearItsExpiry(SpaTestScope scope)
  {
    NotificationState state = scope.Store.GetState<NotificationState>();
    DateTimeOffset addedAt = DateTimeOffset.UtcNow - SuccessAutoDismissInterval + TimeSpan.FromSeconds(1);
    state.AddMessage(MessageBarIntent.Success, "Saved", body: null, addedAt);
    return state.Messages.ShouldHaveSingleItem();
  }

  private static void SaveButton(RenderTreeBuilder builder, Action onClick)
  {
    builder.OpenElement(0, "button");
    builder.AddAttribute(1, "data-qa", "Save");
    builder.AddAttribute(2, "onclick", onClick);
    builder.AddContent(3, "Save");
    builder.CloseElement();
  }
}
