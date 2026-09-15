#region Purpose
// CrossSliceReference for AddPasskeyPrompt; markup lives in AddPasskeyPrompt.razor.
#endregion

#region Design
// Required passkey mode reads SiteSettingsState (Settings slice). Identity owns the prompt;
// settings owns the policy field. Documented TWA0009 edge.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[CrossSliceReference(typeof(SiteSettingsState), "Required passkey prompt mode is site settings policy; the Entra-without-passkey banner lives in Identity.")]
partial class AddPasskeyPrompt;
