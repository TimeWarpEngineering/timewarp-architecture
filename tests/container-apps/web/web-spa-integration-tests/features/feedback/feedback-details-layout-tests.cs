#region Purpose
// The feedback Details wrapper stays the paste target. Visible layout is proved in Playwright.
#endregion

#region Design
// FormField stretches only a direct fluent-* child, so the paste host has to be a wrapper.
// This file checks that contract: the wrapper class, the element reference, and no in-file
// style. It does not grep CSS for width or height. The shadow root's edges, the 9rem start
// height, and the resize grip are asserted by the Playwright test on the rendered page.
#endregion

namespace FeedbackDetails_;

[TestTag("Unit")]
public class FeedbackDetails_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FeedbackDetails_Should_>();

  public static Task Details_Host_Stays_The_Paste_Target()
  {
    string razor = Read("FeedbackListPage.razor");

    razor.ShouldContain("class=\"feedback-details\"");
    razor.ShouldContain("@ref=\"DetailsHost\"");
    razor.ShouldContain("data-qa=\"FeedbackBody\"");
    razor.ShouldNotContain("<style");
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
