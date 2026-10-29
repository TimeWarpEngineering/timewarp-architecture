#region Purpose
// The design-system part table: slug, title, group, usage snippet, and sample component.
#endregion

#region Design
// PageOrder is the All view order. Group declaration order is the catalog heading
// order. Within a group, the catalog follows PageOrder.
// Slugs are lowercase kebab-case. This table is the authority. Resolve compares
// ordinal and case-insensitive. Url emits the canonical lowercase slug under
// /DesignSystem/. An empty slug is All, not a part named all.
// Usage is a hand-written snippet. The single-part view renders it. All does not,
// so the full page stays the section list.
// PaletteName prefixes a part title with DesignSystemPage.Title. Ctrl-K uses it so
// the visible prefix stays the page title.
#endregion

namespace TimeWarp.Architecture.Features.DesignSystem;

/// <summary>Design-system parts, in page order, and the slug lookup.</summary>
internal static class DesignSystemParts
{
  internal enum Group
  {
    Foundations,
    Forms,
    Layout,
    Data,
    Feedback,
    Identity,
  }

  internal enum SelectionKind
  {
    All,
    Part,
    Unknown,
  }

  internal sealed record Part(string Slug, string Title, Group Group, string Usage, Type Sample);

  internal static string PaletteName(Part part) => DesignSystemPage.Title + ": " + part.Title;

  internal readonly record struct Selection(SelectionKind Kind, Part? Part);

  internal static readonly Part[] PageOrder =
  [
    new
    (
      "brand",
      "Brand",
      Group.Foundations,
      """
      <img src="@Brand.LogoSrc" alt="@Brand.LogoAlt" width="@Brand.LogoWidth" height="@Brand.LogoHeight" />
      <p>@Brand.ProductName</p>
      """,
      typeof(BrandSample)
    ),
    new
    (
      "color-tokens",
      "Color Tokens",
      Group.Foundations,
      """
      <div class="ds-swatch__chip" data-token="twe-purple"></div>
      """,
      typeof(ColorTokensSample)
    ),
    new
    (
      "type-scale",
      "Type Scale",
      Group.Foundations,
      """
      <div class="ds-type__row" data-token="twe-text-page-title">28 — Page title</div>
      """,
      typeof(TypeScaleSample)
    ),
    new
    (
      "forms",
      "Forms",
      Group.Forms,
      """
      <FormSection Title="Profile" Description="Identity shown on the account.">
        <FormGrid>
          <FormField Label="Display name" Hint="Shown in the app bar.">
            <FluentTextInput @bind-Value="Name" AriaLabel="Display name" />
          </FormField>
        </FormGrid>
      </FormSection>
      <FormActions>
        <FluentButton Appearance="ButtonAppearance.Primary">Save</FluentButton>
      </FormActions>
      """,
      typeof(FormsSample)
    ),
    new
    (
      "section",
      "Section",
      Group.Layout,
      """
      <Section Title="Reports" LoadingActionType="typeof(CounterState.IncrementCounterActionSet.Action)">
        <p>Loaded content.</p>
      </Section>
      """,
      typeof(SectionSample)
    ),
    new
    (
      "section-header",
      "Section Header",
      Group.Layout,
      """
      <SectionHeader Title="Reports" Subtitle="Summary for the current period.">
        <Aside>
          <FluentButton Appearance="ButtonAppearance.Subtle">Details</FluentButton>
        </Aside>
      </SectionHeader>
      """,
      typeof(SectionHeaderSample)
    ),
    new
    (
      "page-header",
      "Page header",
      Group.Layout,
      """
      <TimeWarpPage Title="Reports">
        <Description>Summary for the current period.</Description>
        <Actions>
          <FluentButton Appearance="ButtonAppearance.Outline" IconStart="@(new Icons.Regular.Size20.ArrowSync())">Sync</FluentButton>
          <FluentButton Appearance="ButtonAppearance.Primary" IconStart="@(new Icons.Regular.Size20.Add())">Add item</FluentButton>
        </Actions>
      </TimeWarpPage>
      """,
      typeof(PageHeaderSample)
    ),
    new
    (
      "page-split",
      "Page split",
      Group.Layout,
      """
      <PageSplit>
        <Primary>
          <p>Primary column</p>
        </Primary>
        <Secondary>
          <p>Secondary column</p>
        </Secondary>
      </PageSplit>
      """,
      typeof(PageSplitSample)
    ),
    new
    (
      "filter-bar",
      "Filter Bar",
      Group.Forms,
      """
      <FilterBar SearchText="Report" SearchPlaceholder="Search…">
        <FilterSelect Label="Status" Items="Statuses" Value="Open" />
      </FilterBar>
      """,
      typeof(FilterBarSample)
    ),
    new
    (
      "tabs",
      "Tabs",
      Group.Forms,
      """
      <Tabs Items="Items" Value="Overview" AriaLabel="Sample views" />
      """,
      typeof(TabsSample)
    ),
    new
    (
      "segmented-control",
      "Segmented Control",
      Group.Forms,
      """
      <SegmentedControl Items="Items" Value="Week" AriaLabel="Sample period" />
      """,
      typeof(SegmentedControlSample)
    ),
    new
    (
      "period-stepper",
      "Period stepper",
      Group.Forms,
      """
      <PeriodStepper Label="This period" PreviousDisabled="true" />
      """,
      typeof(PeriodStepperSample)
    ),
    new
    (
      "avatars",
      "Avatars",
      Group.Identity,
      """
      <Avatar Name="Ada Lovelace" />
      <Avatar Name="Ada Lovelace" Size="InitialsAvatarSize.Large" />
      """,
      typeof(AvatarsSample)
    ),
    new
    (
      "user-card",
      "User card",
      Group.Identity,
      """
      <UserCard Name="Ada Lovelace" Detail="ada@example.test" Href="@ProfilePage.GetPageUrl()" />
      """,
      typeof(UserCardSample)
    ),
    new
    (
      "sign-in",
      "Sign-in",
      Group.Identity,
      """
      <SignInChoices Methods="Methods" />
      """,
      typeof(SignInSample)
    ),
    new
    (
      "empty-states",
      "Empty States",
      Group.Feedback,
      """
      <EmptyState HeadingLevel="3" Title="No reports yet" Description="Reports appear here after the first run." />
      """,
      typeof(EmptyStatesSample)
    ),
    new
    (
      "drawers",
      "Drawers",
      Group.Layout,
      """
      <FluentButton OnClick="@(() => ApplicationState.SetActiveModal(drawerId))">Open drawer</FluentButton>
      <Drawer Title="Sample drawer" IsOpen="@(ApplicationState.ActiveModalId == drawerId)" OnClose="@(() => ApplicationState.CloseModal())">
        <ChildContent>
          <p>A detail or edit form can sit here.</p>
        </ChildContent>
      </Drawer>
      """,
      typeof(DrawersSample)
    ),
    new
    (
      "kpi-cards",
      "KPI Cards",
      Group.Data,
      """
      <KpiStrip>
        <KpiCard Label="Open items" Value="12" />
        <KpiCard Label="Exceptions" Value="3" Tone="KpiTone.Danger" />
      </KpiStrip>
      """,
      typeof(KpiCardsSample)
    ),
    new
    (
      "definition-list",
      "Definition list",
      Group.Data,
      """
      <DefinitionList>
        <DefinitionItem Term="Kind">Human</DefinitionItem>
        <DefinitionItem Term="Status">
          <StatusBadge Status="BadgeStatus.Active" />
        </DefinitionItem>
      </DefinitionList>
      """,
      typeof(DefinitionListSample)
    ),
    new
    (
      "chart-frame",
      "Chart frame",
      Group.Data,
      """
      <ChartCard Title="Sample series" HeadingLevel="3">
        <Header>
          <SegmentedControl Items="Items" Value="Week" AriaLabel="Sample period" />
        </Header>
        <ChildContent>
          <LineChart AxisLabels="Labels" Series="Series" />
        </ChildContent>
        <Legend>
          <ChartLegendItem Label="Series A" Tone="ChartSeriesTone.Ink" />
        </Legend>
      </ChartCard>
      """,
      typeof(ChartFrameSample)
    ),
    new
    (
      "trend-sla-class-pills",
      "Trend, SLA, and class pills",
      Group.Data,
      """
      <TrendPill Direction="TrendDirection.Up" Label="4.2%" />
      <SlaChip Tone="SlaTone.Good" Label="98%" />
      <ClassPill Label="Operations" />
      """,
      typeof(TrendSlaClassPillsSample)
    ),
    new
    (
      "integration-cards",
      "Integration Cards",
      Group.Data,
      """
      <IntegrationCard Title="External system" Meta="Last data pull 2 hours ago" Status="BadgeStatus.Active" StatusLabel="Connected">
        <FluentButton Appearance="ButtonAppearance.Outline">Sync now</FluentButton>
      </IntegrationCard>
      <IntegrationCard Variant="IntegrationCardVariant.Compact" Tone="IntegrationCardTone.Warning" Title="External system" Meta="The connection cannot be used." />
      """,
      typeof(IntegrationCardsSample)
    ),
    new
    (
      "grouped-table",
      "Grouped Table",
      Group.Data,
      """
      <GroupedTable ColumnCount="2" Label="Work items">
        <thead>
          <tr>
            <TableHeader>Item</TableHeader>
            <th class="twe-numeric">Count</th>
          </tr>
        </thead>
        <TableGroup Title="Open" Count="1">
          <tr>
            <Cell>Alpha</Cell>
            <td class="twe-numeric">12</td>
          </tr>
        </TableGroup>
      </GroupedTable>
      """,
      typeof(GroupedTableSample)
    ),
    new
    (
      "count-chip",
      "Count Chip",
      Group.Data,
      """
      <CountChip Value="12" />
      <CountChip Value="7" Accent="true" />
      """,
      typeof(CountChipSample)
    ),
    new
    (
      "confirmations",
      "Confirmations",
      Group.Feedback,
      """
      <FluentButton Appearance="ButtonAppearance.Outline" OnClick="ConfirmSampleAsync">Ask to confirm</FluentButton>
      """,
      typeof(ConfirmationsSample)
    ),
    new
    (
      "buttons",
      "Buttons (FluentUI, themed)",
      Group.Forms,
      """
      <FluentButton Appearance="ButtonAppearance.Primary">Primary</FluentButton>
      <FluentButton Appearance="ButtonAppearance.Outline" Class="twe-button-danger">Danger</FluentButton>
      """,
      typeof(ButtonsSample)
    ),
    new
    (
      "status-badges",
      "Status Badges",
      Group.Feedback,
      """
      <StatusBadge Status="BadgeStatus.Active" />
      <StatusBadge Status="BadgeStatus.Error" Label="Lost" />
      """,
      typeof(StatusBadgesSample)
    ),
    new
    (
      "role-badges",
      "Role badges",
      Group.Identity,
      """
      <RoleBadge Role="Owner" Status="BadgeStatus.Warning" />
      """,
      typeof(RoleBadgesSample)
    ),
    new
    (
      "role-cards",
      "Role cards",
      Group.Identity,
      """
      <RoleCard Title="Owner" Description="Full access to billing, people, and records." Selected="true" />
      """,
      typeof(RoleCardsSample)
    ),
    new
    (
      "permission-grid",
      "Permission grid",
      Group.Identity,
      """
      <PermissionGrid Groups="Groups" />
      """,
      typeof(PermissionGridSample)
    ),
    new
    (
      "dev-note",
      "Dev note",
      Group.Foundations,
      """
      <DevNote Eyebrow="Note" Title="How a note reads">
        The body can include <code>inline code</code>.
      </DevNote>
      """,
      typeof(DevNoteSample)
    ),
    new
    (
      "message-bars",
      "Message bars",
      Group.Feedback,
      """
      <MessageBar Intent="MessageBarIntent.Info"
                  Title="Information"
                  Body="Helpful context that does not require immediate action." />
      """,
      typeof(MessageBarsSample)
    ),
    new
    (
      "notifications",
      "Notifications (message bars)",
      Group.Feedback,
      """
      <FluentButton Appearance="ButtonAppearance.Primary" OnClick="@(() => ShowMessage(MessageBarIntent.Info, "Heads up."))">Info</FluentButton>
      """,
      typeof(NotificationsSample)
    ),
  ];

  internal static Selection Resolve(string? slug)
  {
    if (string.IsNullOrWhiteSpace(slug))
    {
      return new Selection(SelectionKind.All, null);
    }

    foreach (Part part in PageOrder)
    {
      if (part.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase))
      {
        return new Selection(SelectionKind.Part, part);
      }
    }

    return new Selection(SelectionKind.Unknown, null);
  }

  internal static bool TryGet(string slug, out Part? part)
  {
    Selection selection = Resolve(slug);
    part = selection.Part;
    return selection.Kind == SelectionKind.Part;
  }

  internal static string Url(Part part) => string.Concat(DesignSystemPage.GetPageUrl(), "/", part.Slug);

  internal static RenderFragment Render(Part part) => builder =>
  {
    builder.OpenComponent(0, part.Sample);
    builder.CloseComponent();
  };
}
