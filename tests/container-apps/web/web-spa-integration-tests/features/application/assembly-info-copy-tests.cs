#region Purpose
// Task 450: the version dialog's Copy button writes every displayed AssemblyInfo row and both
// URLs to the clipboard, renders only once AssemblyInfo is loaded, and warns on a denial.
#endregion

#region Design
// Same C-create in-proc shape and doubles as CopyMessageBar_Should_ (notification-copy-test-
// doubles.cs): the clipboard is a recording IJSRuntime. AssemblyInfo reads attributes from an
// Assembly, so the fixture is a dynamic assembly with a known title, version, informational
// version (carrying the commit hash), RepositoryUrl and CommitDate metadata, and a blank
// Description that must be omitted. The button tests render it on its own, since the null case
// is the button's own guard. The modal tests render AssemblyInfoModal inside a ModalController
// (the command-palette RenderShellModalsAsync precedent) through ModalHost, which first renders
// with no active modal and then opens AssemblyInfoModal, so ModalController runs OnActivate the
// way the shell does and the dialog loads the real Web.Spa AssemblyInfo.
#endregion

namespace AssemblyInfoCopy_;

using System.Reflection;
using System.Reflection.Emit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.Logging;
using NotificationCopy_;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Services;
using static TimeWarp.Architecture.Features.NotificationState;

[TestTag("Integration")]
public class AssemblyInfoCopy_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AssemblyInfoCopy_Should_>();

  private const string CommitHash = "0123456789abcdef0123456789abcdef01234567";
  private const string RepositoryUrl = "https://github.com/Example/fake-spa";
  private const string CommitDate = "2026-10-28T12:34:56";

  private const string ExpectedText =
    "Title: Fake.Spa\n"
    + "Version: 1.2.3.4\n"
    + "InformationalVersion: 1.2.3+" + CommitHash + "\n"
    + "CommitHash: " + CommitHash + "\n"
    + "ShortHash: 234567\n"
    + "CommitDate: 2026-10-28 12:34:56\n"
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

  public static async Task Render_Copy_Beside_One_Close_In_The_Open_Dialog()
  {
    using CopySpa spa = new(clipboardThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    ModalHost? host = null;

    await using HtmlRenderer renderer = new(scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<ILoggerFactory>());
    HtmlRootComponent root = await renderer.Dispatcher.InvokeAsync(() =>
      renderer.RenderComponentAsync<ModalHost>
      (
        ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(ModalHost.Registered)] = (Action<ModalHost>)(h => host = h) })
      ));
    string closed = await renderer.Dispatcher.InvokeAsync(() => root.ToHtmlString());
    closed.ShouldNotContain("data-qa=\"ModalButton\"");

    await host!.OpenAsync(AssemblyInfoModal.ModalId);
    string open = await renderer.Dispatcher.InvokeAsync(() => root.ToHtmlString());

    Occurrences(open, "data-qa=\"ModalButton\"").ShouldBe(1, "ActionContent replaces the container's default Close");
    Occurrences(open, "data-qa=\"AssemblyInfoCopy\"").ShouldBe(1, "Copy renders once the dialog loads AssemblyInfo");
    open.IndexOf("data-qa=\"AssemblyInfoCopy\"", StringComparison.Ordinal)
      .ShouldBeLessThan(open.IndexOf("data-qa=\"ModalButton\"", StringComparison.Ordinal), "Copy sits before Close");
    open.ShouldContain(">Version:<", Case.Sensitive, "the Properties loop renders the rows");
  }

  public static async Task Close_The_Dialog_From_Its_Own_Close_Button()
  {
    using CopySpa spa = new(clipboardThrows: false);
    using SpaTestScope scope = SpaTestScope.Create(spa);
    await scope.Send(new ApplicationState.SetActiveModalActionSet.Action(AssemblyInfoModal.ModalId));
    ModalHost? host = null;

    await using ClickRenderer renderer = new(scope.ServiceProvider);
    await renderer.RenderAsync<ModalHost>
    (
      new Dictionary<string, object?> { [nameof(ModalHost.Registered)] = (Action<ModalHost>)(h => host = h) }
    );
    await host!.OpenAsync(AssemblyInfoModal.ModalId);
    renderer.ClickHandlers.ShouldContainKey("AssemblyInfoCopy");

    await renderer.ClickAsync("ModalButton");

    scope.Store.GetState<ApplicationState>().ActiveModalId.ShouldBeNull("the re-declared Close dispatches CloseModal");
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
    warning.Body.ShouldBe(ClipboardJsModule.DeniedBody);
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
        Metadata("RepositoryUrl", RepositoryUrl),
        Metadata("CommitDate", CommitDate),
      ]
    );
    return new AssemblyInfo(assembly);
  }

  private static CustomAttributeBuilder Metadata(string key, string value) =>
    new(typeof(AssemblyMetadataAttribute).GetConstructor([typeof(string), typeof(string)])!, [key, value]);

  private static CustomAttributeBuilder Attribute<TAttribute>(string value) where TAttribute : Attribute =>
    new(typeof(TAttribute).GetConstructor([typeof(string)])!, [value]);

  private static int Occurrences(string text, string token) => text.Split(token).Length - 1;

  // Hosts AssemblyInfoModal in a ModalController the way the shell does; OpenAsync changes
  // ActiveModalId after the first render so ModalController invokes OnActivate.
  private sealed class ModalHost : ComponentBase
  {
    private string? ActiveModalId;

    [Parameter] public Action<ModalHost>? Registered { get; set; }

    public Task OpenAsync(string modalId) =>
      InvokeAsync(() =>
      {
        ActiveModalId = modalId;
        StateHasChanged();
      });

    protected override void OnInitialized() => Registered?.Invoke(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
      builder.OpenComponent<ModalController>(0);
      builder.AddComponentParameter(1, nameof(ModalController.ActiveModalId), ActiveModalId);
      builder.AddComponentParameter
      (
        2,
        nameof(ModalController.ModalContainers),
        (RenderFragment)(modals =>
        {
          modals.OpenComponent<AssemblyInfoModal>(0);
          modals.CloseComponent();
        })
      );
      builder.CloseComponent();
    }
  }
}
