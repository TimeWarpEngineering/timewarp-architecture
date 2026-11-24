#region Purpose
// Render PermissionTree with HtmlRenderer and pin groups, three-state parents, locked rows, and data-qa.
#endregion

#region Design
// FluentCheckbox needs the FluentUI services, so the host is StaticRenderHost. A static render
// cannot click, so this pins what each state renders. Clicks, arrow keys, and row height are
// pinned in the role pages Playwright test.
#endregion

namespace PermissionTreeRender_;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Components;

[TestTag("Unit")]
public class PermissionTree_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<PermissionTree_Should_>();

  public static async Task Render_ThreeState_Parents_From_The_Selected_Set()
  {
    string html = await RenderAsync(Parameters());

    html.ShouldContain("role=\"group\"");
    html.ShouldContain("aria-label=\"Sample permissions\"");
    Parent(html, "PermissionGroup-records").ShouldContain("indeterminate=\"true\"");
    Parent(html, "PermissionGroup-people").ShouldContain("checked=\"true\"");
    Parent(html, "PermissionGroup-billing").ShouldNotContain("checked");
    Parent(html, "PermissionGroup-billing").ShouldNotContain("indeterminate");
    Item(html, "Permission-records.read").ShouldContain("checked=\"true\"");
    Item(html, "Permission-records.write").ShouldNotContain("checked");
    html.ShouldContain(">records.*</label>");
    html.ShouldContain(">records.read</label>");
  }

  public static async Task Lock_Locked_Items_And_A_Parent_With_Only_Locked_Items()
  {
    string html = await RenderAsync(Parameters());

    string locked = Item(html, "Permission-people.manage");
    locked.ShouldContain("checked=\"true\"");
    locked.ShouldContain(" disabled");
    Item(html, "Permission-people.read").ShouldNotContain("disabled");
    Parent(html, "PermissionGroup-people").ShouldNotContain("disabled");

    string onlyLocked = await RenderAsync(Parameters(locked: ["people.read", "people.manage"]));
    Parent(onlyLocked, "PermissionGroup-people").ShouldContain(" disabled");
    Parent(onlyLocked, "PermissionGroup-records").ShouldNotContain("disabled");
  }

  public static async Task Disable_Every_Row_When_Disabled()
  {
    Dictionary<string, object?> parameters = Parameters();
    parameters["Disabled"] = true;
    string html = await RenderAsync(parameters);

    int checkboxes = Count(html, "<fluent-checkbox");
    checkboxes.ShouldBe(8);
    Count(html, "<fluent-checkbox", " disabled").ShouldBe(checkboxes);
  }

  public static async Task Pass_Margin_Zero_On_Every_Checkbox()
  {
    string html = await RenderAsync(Parameters());

    Count(html, "<fluent-field").ShouldBe(8);
    Count(html, "style=\"margin: 0px;\"").ShouldBe(8);
  }

  public static async Task Use_The_Callers_Qa_Prefixes()
  {
    Dictionary<string, object?> parameters = Parameters();
    parameters["GroupQaPrefix"] = "AgentScopeGroup";
    parameters["ItemQaPrefix"] = "AgentScope";
    parameters["data-qa"] = "RoleAgentScopeEditor";
    string html = await RenderAsync(parameters);

    html.ShouldContain("data-qa=\"RoleAgentScopeEditor\"");
    html.ShouldContain("data-qa=\"AgentScopeGroup-records\"");
    html.ShouldContain("data-qa=\"AgentScope-records.read\"");
    html.ShouldNotContain("data-qa=\"Permission-");
  }

  public static async Task Render_The_Root_And_No_Rows_For_Null_Groups()
  {
    string html = await RenderAsync(new Dictionary<string, object?>());

    html.ShouldContain("twe-perm-tree");
    html.ShouldNotContain("<fluent-checkbox");
  }

  private static Dictionary<string, object?> Parameters(string[]? locked = null) => new()
  {
    ["Groups"] = Sample(),
    ["Selected"] = new HashSet<string>(["records.read", "people.read", "people.manage"], StringComparer.Ordinal),
    ["Locked"] = new HashSet<string>(locked ?? ["people.manage"], StringComparer.Ordinal),
    ["AriaLabel"] = "Sample permissions",
  };

  private static string Parent(string html, string groupQa)
  {
    int group = html.IndexOf($"data-qa=\"{groupQa}\"", StringComparison.Ordinal);
    group.ShouldBeGreaterThan(-1, groupQa);
    return CheckboxTag(html, html.IndexOf("<fluent-checkbox", group, StringComparison.Ordinal));
  }

  private static string Item(string html, string itemQa)
  {
    int qa = html.IndexOf($"data-qa=\"{itemQa}\"", StringComparison.Ordinal);
    qa.ShouldBeGreaterThan(-1, itemQa);
    return CheckboxTag(html, html.LastIndexOf("<fluent-checkbox", qa, StringComparison.Ordinal));
  }

  private static string CheckboxTag(string html, int start)
  {
    start.ShouldBeGreaterThan(-1);
    int end = html.IndexOf('>', start, StringComparison.Ordinal);
    return html.Substring(start, end - start + 1);
  }

  private static int Count(string html, string marker, string? within = null)
  {
    int count = 0;
    int index = 0;
    while ((index = html.IndexOf(marker, index, StringComparison.Ordinal)) >= 0)
    {
      if (within is null || CheckboxTag(html, index).Contains(within, StringComparison.Ordinal))
      {
        count++;
      }

      index += marker.Length;
    }

    return count;
  }

  private static PermissionTreeGroup[] Sample() =>
  [
    new("records", "records.*", ["records.read", "records.write"]),
    new("people", "people.*", ["people.read", "people.manage"]),
    new("billing", "billing.*", ["billing.read"])
  ];

  private static async Task<string> RenderAsync(Dictionary<string, object?> parameters)
  {
    await using ServiceProvider provider = StaticRenderHost.CreateServices().BuildServiceProvider();
    await using HtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    return await renderer.Dispatcher.InvokeAsync(async () =>
      (await renderer.RenderComponentAsync<PermissionTree>(ParameterView.FromDictionary(parameters))).ToHtmlString());
  }
}
