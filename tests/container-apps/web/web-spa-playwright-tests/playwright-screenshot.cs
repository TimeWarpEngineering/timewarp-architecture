#region Purpose
// One place that decides where a Playwright suite writes its screenshots and captured HTML.
#endregion

#region Design
// Shots go to the test's own output: <test bin>/playwright-screenshots/<test class>/<file>. They
// never go into the source tree, so a test run leaves git status clean. Generated apps ship
// tests/ without the monorepo's kanban/, so no path here may depend on it. CI uploads the
// playwright-screenshots folders from the test bin.
// Review evidence: a worker who needs shots on a task copies them from test output into the task
// folder by hand, as part of that task.
#endregion

namespace WebSpaPlaywright_;

public static class PlaywrightScreenshot
{
  public const string FolderName = "playwright-screenshots";

  public static string PathFor<TTest>(string fileName)
  {
    string folder = Path.Combine(AppContext.BaseDirectory, FolderName, typeof(TTest).Name);
    Directory.CreateDirectory(folder);
    return Path.Combine(folder, fileName);
  }
}
