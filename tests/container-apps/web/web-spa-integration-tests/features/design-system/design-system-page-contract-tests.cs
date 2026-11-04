#region Purpose
// Pin the composed DesignSystemPage: every upstreamed part, in page order, with its sample.
#endregion

#region Design
// Sibling ports added a card and a leaf render test, and left this page contract for task 379-017.
// HtmlRenderer over StaticRenderHost, shared with TimeWarpPage_Should_, shows the catalog
// without a browser. Scope attributes change with the stylesheet, so the contract is structure
// and sample text. A closed drawer omits its title. The form fields are the only stored choice.
// The source scan counts every @bind on the forms sample, rejects any Changed= callback
// on the demos, and requires the ConfirmSample button's OnClick to be ConfirmSampleAsync,
// which calls NoSubDesignSystemState.ConfirmSample(). The catalog filter is the one
// ValueChanged, and it is component-local text, not a stored demo choice. That
// exemption covers only the Changed= and ValueChanged assertions. The product-name
// scan still reads DesignSystemCatalog.razor.
// Group headings are h2 elements. Each nested part list is labelled by that heading id.
// A static render cannot see an unused callback parameter.
// The absent-name check composes its token so this file does not spell the source product name.
// Slugs are lowercase kebab-case. Resolve is ordinal and case-insensitive. /DesignSystem/all
// is not a part. Usage is rendered on the single-part view only.
// The shell header has no actions. twe-page__actions appears once, after the Page header
// example, and a non-header part renders none. That example's title is a div, not an h1.
// All-view card titles are Part.Title in PageOrder. A group heading is the enum name, and the
// catalog shows a group only when it has a part, in Group declaration order.
#endregion

namespace DesignSystemPageContract_;

using TimeWarp.Architecture.Features.DesignSystem;

[TestTag("Unit")]
public partial class DesignSystemPage_Should_
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<DesignSystemPage_Should_>();

  public static async Task List_Every_Part_In_Page_Order()
  {
    DesignSystemParts.PageOrder.Select(static part => part.Slug).ShouldBeUnique();
    foreach (DesignSystemParts.Part part in DesignSystemParts.PageOrder)
    {
      IsKebabSlug(part.Slug).ShouldBeTrue(part.Slug);
      DesignSystemParts.TryGet(part.Slug, out DesignSystemParts.Part? resolved).ShouldBeTrue(part.Slug);
      resolved!.Title.ShouldBe(part.Title);
      DesignSystemParts.TryGet(part.Slug.ToUpperInvariant(), out DesignSystemParts.Part? ignoredCase).ShouldBeTrue(part.Slug);
      ignoredCase!.Slug.ShouldBe(part.Slug);
      DesignSystemParts.Url(part).ShouldBe("/DesignSystem/" + part.Slug);
    }

    DesignSystemParts.Resolve(null).Kind.ShouldBe(DesignSystemParts.SelectionKind.All);
    DesignSystemParts.Resolve(" ").Kind.ShouldBe(DesignSystemParts.SelectionKind.All);
    DesignSystemParts.Resolve("all").Kind.ShouldBe(DesignSystemParts.SelectionKind.Unknown);
    DesignSystemParts.TryGet("no-such-part", out _).ShouldBeFalse();

    string html = await RenderAsync();

    StaticRenderHost.CountOpen(html, "h1").ShouldBe(1);
    StaticRenderHost.Title(html).ShouldBe("Design System");
    html.ShouldContain("twe-page__description");
    AssertPageHeaderOwnsTheOnlyActions(html, "all");
    html.Contains(SourceProductName(), StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    html.ShouldNotContain("Sample drawer");
    html.ShouldNotContain("data-qa=\"DesignSystemUsage\"");
    html.ShouldNotContain(">Omitted<");
    string catalog = Catalog(html);
    Count(catalog, "aria-current=\"page\"").ShouldBe(1);
    TagContaining(catalog, "data-qa=\"DesignSystemAll\"").ShouldContain("aria-current=\"page\"");
    foreach (DesignSystemParts.Group group in Enum.GetValues<DesignSystemParts.Group>())
    {
      if (!Array.Exists(DesignSystemParts.PageOrder, part => part.Group == group))
      {
        continue;
      }

      string headingId = "ds-catalog-" + group.ToString().ToLowerInvariant();
      string heading = TagContaining(catalog, $"id=\"{headingId}\"");
      heading.StartsWith("<h2", StringComparison.Ordinal).ShouldBeTrue(group.ToString());
      heading.ShouldContain("ds-catalog__heading");
      catalog.ShouldContain($">{group}</h2>");
      string parts = TagContaining(catalog, $"aria-labelledby=\"{headingId}\"");
      parts.StartsWith("<ul", StringComparison.Ordinal).ShouldBeTrue(group.ToString());
    }

    catalog.ShouldNotContain("<p class=\"ds-catalog__heading\"");

    string[] partTitles = [.. DesignSystemParts.PageOrder.Select(static part => part.Title)];
    ContainTitlesInOrder(CardTitles(html), partTitles);

    AppearInOrder
    (
      html,
      "Live brand tokens, type scale, and components",
      "data-qa=\"DesignSystemBrandName\"",
      "TimeWarp.Architecture",
      "data-qa=\"DesignSystemBrandSeed\"",
      "#55409c",
      "Harmonized",
      "Purple (primary)",
      "28 — Page title",
      "value=\"Ada Lovelace\"",
      "data-qa=\"DesignSystemFormField\"",
      "Reports",
      "Summary for the current period.",
      "data-qa=\"DesignSystemPageSplit\"",
      "Search…",
      "aria-label=\"Sample views\"",
      "aria-label=\"Sample period\"",
      "data-qa=\"DesignSystemPeriodStepper\"",
      "href=\"/Profile\"",
      "data-qa=\"DesignSystemUserCard\"",
      "data-qa=\"DesignSystemSignInOne\"",
      "data-qa=\"ContinueWithPasskey\"",
      "data-qa=\"DesignSystemSignInBoth\"",
      "data-qa=\"ContinueWithMicrosoft365\"",
      "No reports yet",
      "No locations selected",
      "No rows match",
      "data-qa=\"DrawerOpen\"",
      "Open items",
      "49.5%",
      "Last 12 months",
      "Exceptions",
      "data-qa=\"DesignSystemDefinitionList\"",
      "data-qa=\"DesignSystemChartCard\"",
      "All locations",
      "twe-line-chart",
      "Series A",
      "Series B",
      "Series C",
      "4.2%",
      "1.8%",
      "98%",
      "90%",
      "78%",
      "Operations",
      "Not mapped",
      "Connected",
      "Needs attention",
      "data-qa=\"DesignSystemIntegrationWarning\"",
      "data-qa=\"DesignSystemGroupedTable\"",
      "twe-numeric",
      ">Alpha<",
      "In progress",
      "data-qa=\"ConfirmSample\"",
      "data-qa=\"DangerOutline\"",
      "Lost",
      "data-qa=\"DesignSystemRoleBadges\"",
      "data-qa=\"DesignSystemPermissionGrid\"",
      "View assigned work",
      "Not granted",
      "data-qa=\"DesignSystemDevNote\"",
      "inline code",
      "data-qa=\"ToastInfo\"",
      "data-qa=\"ToastSuccess\"",
      "data-qa=\"ToastWarning\"",
      "data-qa=\"ToastError\"",
      "data-qa=\"ToastException\""
    );

    string roles = Slice(html, "data-qa=\"DesignSystemRoleCards\"", "data-qa=\"DesignSystemPermissionGrid\"");
    Count(roles, "aria-checked=\"true\"").ShouldBe(1);
    Count(roles, "aria-checked=\"false\"").ShouldBe(2);
    string owner = RoleCard(roles, "Owner");
    string admin = RoleCard(roles, "Admin");
    string staff = RoleCard(roles, "Staff");
    owner.ShouldContain("aria-checked=\"true\"");
    owner.ShouldContain("tabindex=\"0\"");
    admin.ShouldContain("aria-checked=\"false\"");
    admin.ShouldContain("tabindex=\"-1\"");
    admin.ShouldNotContain("aria-checked=\"true\"");
    staff.ShouldContain("aria-checked=\"false\"");
    staff.ShouldContain("tabindex=\"-1\"");
    staff.ShouldNotContain("aria-checked=\"true\"");
    roles.IndexOf("aria-checked=\"true\"", StringComparison.Ordinal)
      .ShouldBeLessThan(roles.IndexOf(">Admin<", StringComparison.Ordinal));
    CardTitles(html).Count.ShouldBe(DesignSystemParts.PageOrder.Length);
  }

  public static async Task Render_Each_Part_With_Its_Usage()
  {
    foreach (DesignSystemParts.Part part in DesignSystemParts.PageOrder)
    {
      string html = await RenderAsync(part.Slug);
      List<string> titles = CardTitles(html);
      titles.Count.ShouldBe(1, part.Slug);
      titles[0].ShouldBe(part.Title);
      html.ShouldContain("data-qa=\"DesignSystemUsage\"");
      html.ShouldContain(part.Usage);
      string catalog = Catalog(html);
      Count(catalog, "aria-current=\"page\"").ShouldBe(1, part.Slug);
      TagContaining(catalog, $"data-part=\"{part.Slug}\"").ShouldContain("aria-current=\"page\"");
      StaticRenderHost.Title(html).ShouldBe("Design System");
      AssertPageHeaderOwnsTheOnlyActions(html, part.Slug);
    }
  }

  public static async Task Render_The_Page_Header_Sample_Without_An_H1()
  {
    string html = await RenderAsync("page-header");
    string example = Slice(html, "data-qa=\"PageHeaderExample\"", "data-qa=\"PageHeaderRule\"");
    example.ShouldContain("twe-page__title");
    example.ShouldContain("Reports");
    example.IndexOf("<h1", StringComparison.Ordinal).ShouldBe(-1);
    StaticRenderHost.CountOpen(html, "h1").ShouldBe(1);
    StaticRenderHost.Title(html).ShouldBe("Design System");
  }

  public static async Task Show_An_Unknown_Slug_Inside_The_Page()
  {
    string html = await RenderAsync("no-such-part");
    StaticRenderHost.CountOpen(html, "h1").ShouldBe(1);
    StaticRenderHost.Title(html).ShouldBe("Design System");
    html.ShouldContain("data-qa=\"DesignSystemUnknown\"");
    html.ShouldContain("No such part.");
    html.ShouldContain("href=\"/DesignSystem\"");
    html.ShouldNotContain("data-qa=\"DesignSystemUsage\"");
    Catalog(html).ShouldNotContain("aria-current=\"page\"");
    CardTitles(html).ShouldBeEmpty();
  }

  public static Task Author_No_Inline_Style_Attributes()
  {
    foreach (string file in FeatureFiles())
    {
      File.ReadAllText(file).Contains("style=", StringComparison.Ordinal).ShouldBeFalse(file);
    }

    return Task.CompletedTask;
  }

  public static Task Keep_Stored_Choices_On_The_Form_Fields_Only()
  {
    string forms = File.ReadAllText(FeatureFile("samples", "FormsSample.razor"));
    Count(forms, "@bind").ShouldBe(2);
    forms.ShouldContain("@bind-Value=\"FormDemoName\"");
    forms.ShouldContain("@bind-Value=\"FormDemoEmail\"");

    string confirmations = File.ReadAllText(FeatureFile("samples", "ConfirmationsSample.razor"));
    TagContaining(confirmations, "data-qa=\"ConfirmSample\"").ShouldContain("OnClick=\"ConfirmSampleAsync\"");
    int handler = confirmations.IndexOf("ConfirmSampleAsync()", StringComparison.Ordinal);
    handler.ShouldBeGreaterThan(-1, "ConfirmSampleAsync()");
    int statementEnd = confirmations.IndexOf(';', handler, StringComparison.Ordinal);
    statementEnd.ShouldBeGreaterThan(handler);
    confirmations.Substring(handler, statementEnd - handler).ShouldContain("NoSubDesignSystemState.ConfirmSample()");

    string drawers = File.ReadAllText(FeatureFile("samples", "DrawersSample.razor"));
    drawers.ShouldContain("ApplicationState.SetActiveModal(DesignSystemDrawerId)");
    drawers.ShouldContain("ApplicationState.CloseModal()");

    foreach (string file in FeatureFiles())
    {
      string source = File.ReadAllText(file);
      // ValueChanged="@…" also contains the Changed=" substring, so both assertions
      // skip the catalog filter. The product-name scan still reads that file.
      if (!file.EndsWith("DesignSystemCatalog.razor", StringComparison.Ordinal))
      {
        source.ShouldNotContain("Changed=\"");
        source.ShouldNotContain("ValueChanged");
      }

      source.IndexOf(SourceProductName(), StringComparison.OrdinalIgnoreCase).ShouldBe(-1);
    }

    return Task.CompletedTask;
  }
}
