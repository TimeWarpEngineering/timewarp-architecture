#region Purpose
// The feedback Details host stays the paste target and stretches like the Title field.
#endregion

#region Design
// FormField stretches only a direct fluent-* child. Details wraps FluentTextArea so the paste
// listener has a host, which is why the control used to stay at the component's intrinsic
// width. FluentTextArea renders fluent-field around fluent-textarea, so the sheet reaches
// the control as a descendant. A custom element stays at its intrinsic width until it is
// display:block with width:100%. The Playwright width check is the rendered proof.
#endregion

namespace FeedbackDetails_;

[TestTag("Unit")]
public class FeedbackDetails_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FeedbackDetails_Should_>();

  public static Task Details_Host_Stretches_And_Stays_The_Paste_Target()
  {
    string razor = Read("FeedbackListPage.razor");
    string css = Read("FeedbackListPage.razor.css");

    razor.ShouldContain("class=\"feedback-details\"");
    razor.ShouldContain("@ref=\"DetailsHost\"");
    razor.ShouldContain("data-qa=\"FeedbackBody\"");
    razor.ShouldContain("Width=\"100%\"");
    razor.ShouldContain("Height=\"9rem\"");
    razor.ShouldContain("Resize=\"TextAreaResize.Vertical\"");
    razor.ShouldNotContain("<style");

    css.ShouldContain(".feedback-details");
    css.ShouldContain("width: 100%");
    css.ShouldContain("min-height: 9rem");
    css.ShouldContain("::deep fluent-textarea");
    css.ShouldContain("::deep fluent-text-area");
    css.ShouldContain("display: block");
    css.ShouldNotContain("::deep > fluent-textarea");
    css.ShouldNotContain("<style");
    return Task.CompletedTask;
  }

  private static string Read(string fileName)
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, "timewarp-architecture.slnx")))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    string path = Path.Combine(
      directory,
      "source",
      "container-apps",
      "web",
      "projects",
      "web-spa",
      "features",
      "feedback",
      "pages",
      fileName);
    return File.ReadAllText(path);
  }
}
