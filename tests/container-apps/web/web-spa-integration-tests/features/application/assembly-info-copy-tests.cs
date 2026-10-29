#region Purpose
// Task 450: the version dialog's Copy button writes every displayed AssemblyInfo row and both
// URLs to the clipboard, renders only once AssemblyInfo is loaded, and warns on a denial.
#endregion

#region Design
// Same C-create in-proc shape and doubles as CopyMessageBar_Should_ (notification-copy-test-
// doubles.cs): the clipboard is a recording IJSRuntime. AssemblyInfo reads attributes from an
// Assembly, so the fixture is a dynamic assembly with a known title, version, informational
// version (carrying the commit hash), RepositoryUrl metadata, and a blank Description that
// must be omitted. The button is rendered on its own: ModalContainer needs a ModalController
// cascade, and the null case is the button's own guard.
#endregion

namespace AssemblyInfoCopy_;

using System.Reflection;
using System.Reflection.Emit;
using NotificationCopy_;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Services;
using TimeWarp.State.Plus.Extensions;
using static TimeWarp.Architecture.Features.NotificationState;

[TestTag("Integration")]
public class AssemblyInfoCopy_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AssemblyInfoCopy_Should_>();

  private const string CommitHash = "0123456789abcdef0123456789abcdef01234567";
  private const string RepositoryUrl = "https://github.com/Example/fake-spa";

  private const string ExpectedText =
    "Title: Fake.Spa\n"
    + "Version: 1.2.3.4\n"
    + "InformationalVersion: 1.2.3+" + CommitHash + "\n"
    + "CommitHash: " + CommitHash + "\n"
    + "ShortHash: 234567\n"
    + "CommitUrl: " + RepositoryUrl + "/commit/" + CommitHash + "\n"
    + "RepositoryUrl: " + RepositoryUrl;

  public static Task Format_Every_Displayed_Row_And_Both_Urls_Without_Blanks()
  {
    string text = AssemblyInfoText.ClipboardText(FakeAssemblyInfo());

    text.ShouldBe(ExpectedText);
    text.ShouldNotContain(nameof(AssemblyInfo.Description), Case.Sensitive, "a blank value is omitted like PropertyDisplay");
    text.ShouldNotContain(nameof(AssemblyInfo.InternalsVisibleTo), Case.Sensitive, "an empty join is omitted too");
    return Task.CompletedTask;
  }

  public static async Task Hide_Copy_Button_While_AssemblyInfo_Is_Null()
  {
    using CopySpa spa = new(clipboardThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await using ClickRenderer renderer = new(scope.ServiceProvider);
    await renderer.RenderAsync<AssemblyInfoCopyButton>
    (
      new Dictionary<string, object?> { [nameof(AssemblyInfoCopyButton.AssemblyInfo)] = null }
    );

    renderer.ClickHandlers.ShouldNotContainKey("AssemblyInfoCopy");
    renderer.Labels.ShouldNotContainKey("AssemblyInfoCopy");
  }

  public static async Task Copy_Button_Writes_The_Dialog_Text_And_Confirms()
  {
    using CopySpa spa = new(clipboardThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await using ClickRenderer renderer = new(scope.ServiceProvider);
    await renderer.RenderAsync<AssemblyInfoCopyButton>
    (
      new Dictionary<string, object?> { [nameof(AssemblyInfoCopyButton.AssemblyInfo)] = FakeAssemblyInfo() }
    );
    renderer.Labels["AssemblyInfoCopy"].ShouldBe("Copy version info");

    await renderer.ClickAsync("AssemblyInfoCopy");

    spa.JsRuntime.ImportedSpecifier.ShouldBe(ClipboardJsModule.Specifier);
    (string export, string text) = spa.JsRuntime.Module.Calls.ShouldHaveSingleItem();
    export.ShouldBe(ClipboardJsModule.WriteTextExport);
    text.ShouldBe(ExpectedText);
    text.ShouldContain(CommitHash);
    text.ShouldContain($"CommitUrl: {RepositoryUrl}/commit/{CommitHash}");
    text.ShouldContain($"RepositoryUrl: {RepositoryUrl}");
    scope.Store.GetState<ApplicationState>().IsAssemblyInfoCopied.ShouldBeTrue();
    renderer.UpdatedLabels.ShouldContain("Copied", "the button swaps to the Copied confirmation");
    scope.Store.GetState<NotificationState>().Messages.ShouldBeEmpty("a successful copy adds no bar");

    await scope.Send(new ApplicationState.SetActiveModalActionSet.Action(AssemblyInfoModal.ModalId));
    scope.Store.GetState<ApplicationState>().IsAssemblyInfoCopied.ShouldBeFalse("reopening the dialog clears Copied");
  }

  public static async Task Report_Warning_When_Clipboard_Rejects()
  {
    using CopySpa spa = new(clipboardThrows: true);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new ApplicationState.CopyAssemblyInfoActionSet.Action(ExpectedText));

    spa.JsRuntime.Module.Calls.Count.ShouldBe(1);
    scope.Store.GetState<ApplicationState>().IsAssemblyInfoCopied.ShouldBeFalse();
    NotificationMessage warning = scope.Store.GetState<NotificationState>().Messages.ShouldHaveSingleItem();
    warning.Intent.ShouldBe(MessageBarIntent.Warning);
    warning.Title.ShouldBe(ApplicationState.AssemblyInfoCopyFailedTitle);
    warning.Body.ShouldBe(ApplicationState.AssemblyInfoCopyFailedBody);
  }

  public static async Task Report_Warning_When_Clipboard_Times_Out()
  {
    using CopySpa spa = new(clipboardThrows: false, clipboardTimesOut: true);
    using SpaTestScope scope = SpaTestScope.Create(spa);

    await scope.Send(new ApplicationState.CopyAssemblyInfoActionSet.Action(ExpectedText));

    scope.Store.GetState<ApplicationState>().IsAssemblyInfoCopied.ShouldBeFalse();
    scope.Store.GetState<NotificationState>().Messages.ShouldHaveSingleItem().Title
      .ShouldBe(ApplicationState.AssemblyInfoCopyFailedTitle);
  }

  private static AssemblyInfo FakeAssemblyInfo()
  {
    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly
    (
      new AssemblyName("Fake.Spa") { Version = new Version(1, 2, 3, 4) },
      AssemblyBuilderAccess.Run,
      [
        Attribute<AssemblyTitleAttribute>("Fake.Spa"),
        Attribute<AssemblyDescriptionAttribute>("  "),
        Attribute<AssemblyInformationalVersionAttribute>($"1.2.3+{CommitHash}"),
        new CustomAttributeBuilder
        (
          typeof(AssemblyMetadataAttribute).GetConstructor([typeof(string), typeof(string)])!,
          ["RepositoryUrl", RepositoryUrl]
        )
      ]
    );
    return new AssemblyInfo(assembly);
  }

  private static CustomAttributeBuilder Attribute<TAttribute>(string value) where TAttribute : Attribute =>
    new(typeof(TAttribute).GetConstructor([typeof(string)])!, [value]);
}
