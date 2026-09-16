#region Purpose
// Render FormField and assert the hint, default span, and full-width host contract.
#endregion

#region Design
// HtmlRenderer is enough — FormField is a Tier-1 leaf with no DI. Isolated
// FormField.razor.css cannot target Fluent hosts (Wall A); host stretch is
// Exception B in FormField.razor. The test asserts that unscoped rule and the
// wrapper width on FormField.razor.css, not a child `> *` isolation selector.
#endregion

namespace FormFieldRender_;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class FormField_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<FormField_Should_>();

  public static async Task Render_Control_Full_Width_And_Hint()
  {
    string css = ReadFormFieldCss();
    css.ShouldContain(".twe-form-field__control");
    css.ShouldContain("width: 100%");
    css.ShouldContain(".twe-form-field__hint");
    css.ShouldNotContain(".twe-form-field__control > *");

    string razor = ReadFormFieldRazor();
    razor.ShouldContain(".twe-form-field__control > fluent-text-input");
    razor.ShouldContain(".twe-form-field__control > fluent-dropdown");
    razor.ShouldContain("width: 100%");

    await using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
    await using HtmlRenderer renderer = new(services, NullLoggerFactory.Instance);

    string html = await renderer.Dispatcher.InvokeAsync(async () =>
    {
      ParameterView parameters = ParameterView.FromDictionary(
        new Dictionary<string, object?>
        {
          ["Label"] = "Display name",
          ["Hint"] = "Shown under the control",
          ["ChildContent"] = (RenderFragment)(builder =>
          {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "data-qa", "FormFieldControl");
            builder.CloseElement();
          })
        });
      return (await renderer.RenderComponentAsync<FormField>(parameters)).ToHtmlString();
    });

    html.ShouldContain("Display name");
    html.ShouldContain("Shown under the control");
    html.ShouldContain("twe-form-field__hint");
    html.ShouldContain("twe-form-field__control");
    html.ShouldContain("twe-form-field--span-12");
    html.ShouldContain("data-qa=\"FormFieldControl\"");
    html.ShouldContain("fluent-text-input");
    html.ShouldContain("fluent-dropdown");
  }

  private static string ReadFormFieldCss() => ReadFormFieldSource("FormField.razor.css");

  private static string ReadFormFieldRazor() => ReadFormFieldSource("FormField.razor");

  private static string ReadFormFieldSource(string fileName)
  {
    string repoRoot = FindRepoRoot();
    string path = Path.Combine(
      repoRoot,
      "source",
      "container-apps",
      "web",
      "projects",
      "web-spa",
      "components",
      "forms",
      fileName);
    File.Exists(path).ShouldBeTrue(path);
    return File.ReadAllText(path);
  }

  private static string FindRepoRoot()
  {
    DirectoryInfo? dir = new(AppContext.BaseDirectory);
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir.FullName, "source", "Directory.Build.props")))
      {
        return dir.FullName;
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException("Could not locate repo root from " + AppContext.BaseDirectory);
  }
}
