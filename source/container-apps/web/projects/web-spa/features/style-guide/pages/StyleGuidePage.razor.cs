#region Purpose
// Registers the Style Guide routes. /StyleGuide is the navigable All view.
// /StyleGuide/{Part} is a route alias. Markup lives in StyleGuidePage.razor.
#endregion

#region Design
// Demos go through production code paths (state action sets, mediator behaviors).
// Each card is a sample component under samples/. The All view and the single-part
// view render that same component.
// Slugs are lowercase kebab-case. StyleGuideParts is the authority: a slug is not a
// mechanical rewrite of the title. Lookup is ordinal and case-insensitive, so
// /StyleGuide/Drawers still resolves. Links and aria-current use the canonical
// lowercase slug. An empty Part is All (/StyleGuide). There is no /StyleGuide/all
// route. An unknown slug renders the missing-part state and does not throw.
// Catalog group names are h2 headings in StyleGuideParts.Group declaration order
// (Foundations, Forms, Layout, Data, Feedback, Identity). Each nested part list
// is labelled by that heading. Within a group, entries follow
// StyleGuideParts.PageOrder, which is also the All view order.
// A usage snippet is hand-written and shown only on the single-part view.
// The page actions dispatch NotificationState. Confirmations dispatch
// StyleGuideState.ConfirmSample. The sample drawer follows ApplicationState.
// The exception demo dispatches CounterState.ThrowException.
// MessageBarsSample carries the PageLocalMessageBar opt-out (TWA0025). The bars
// are component documentation, not operation outcomes.
// UserCardSample carries the ProfilePage cross-slice opt-out. NotificationsSample
// carries the CounterState cross-slice opt-out. That sample dispatches
// NotificationState rather than INotificationService. Its throw follows
// ExceptionNotification into the message bar. TWA0022 bans a direct mediator
// Send in that sample.
#endregion

namespace TimeWarp.Architecture.Features.StyleGuide;

[Page(
  "/StyleGuide",
  "/StyleGuide/{Part}",
  Policy = PermissionIds.DeveloperAccess,
  Navigable = true,
  Description = "Visual style guide for template components.",
  NavGroup = "Developer",
  NavOrder = 310)]
[Authorize(Policy = PermissionIds.DeveloperAccess)]
partial class StyleGuidePage;
