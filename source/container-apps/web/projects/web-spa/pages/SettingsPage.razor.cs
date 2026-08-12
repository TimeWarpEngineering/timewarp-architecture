#region Purpose
// Settings — signed-in passkey management (list / create / revoke) via CredentialsState.
#endregion

#region Design
// Task 167 product Settings UI; task 169 rehomes data through TimeWarp.State (COPIC rule):
// every backend HTTP call is a CredentialsState ActionSet — never page-local List<> + ceremony
// client GetResponse. Page owns only chrome UX (expanded row). Loading =
// IsAnyActive(FetchCredentials); IsBusy = any tracked credentials action.
// Credentials is null remains the fetch-once guard (no snapshot yet). API failures toast via
// DefaultApiHandler; browser ceremony failures surface as CredentialsState.CeremonyError.
// Backend surface remains 104-005 (GetCredentials, AddPasskey, RevokeCredential).
#endregion

namespace TimeWarp.Architecture.Features.Applications;

[Page("/Settings", Policy = PermissionIds.SettingsRead)]
[Authorize(Policy = PermissionIds.SettingsRead)]
[CrossSliceReference(typeof(CredentialsState), "Settings is Applications chrome; credentials list/create/revoke live on Identity CredentialsState.")]
partial class SettingsPage
{
  private Guid? ExpandedId;

  private bool IsLoading =>
    IsAnyActive(typeof(CredentialsState.FetchCredentialsActionSet.Action));
  private bool IsBusy => ActionTrackingState.IsActive;
  private IReadOnlyList<GetCredentials.CredentialSummary> Passkeys => CredentialsState.ActivePasskeys;
  private string? ErrorMessage => CredentialsState.CeremonyError;
  private string? StatusMessage => CredentialsState.StatusMessage;

  protected override async Task OnInitializedAsync()
  {
    await base.OnInitializedAsync();
    if (RendererInfo.IsInteractive)
    {
      await NoSubCredentialsState.FetchCredentials();
      SyncExpandedId();
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    await base.OnAfterRenderAsync(firstRender);
    if (firstRender && RendererInfo.IsInteractive && CredentialsState.Credentials is null)
    {
      await NoSubCredentialsState.FetchCredentials();
      SyncExpandedId();
      await InvokeAsync(StateHasChanged);
    }
  }

  private void ToggleExpanded(Guid id) =>
    ExpandedId = ExpandedId == id ? null : id;

  private static string DisplayLabel(GetCredentials.CredentialSummary credential) =>
    string.IsNullOrWhiteSpace(credential.Label) ? "Passkey" : credential.Label!;

  private async Task CreatePasskeyAsync()
  {
    await NoSubCredentialsState.AddPasskey();
    if (CredentialsState.LastAddedCredentialId is Guid added)
    {
      ExpandedId = added;
    }
    else
    {
      SyncExpandedId();
    }
  }

  private async Task ConfirmRevokeAsync(GetCredentials.CredentialSummary credential)
  {
    await NoSubCredentialsState.RevokeCredential(credential.Id.Value);
    SyncExpandedId();
  }

  private void SyncExpandedId()
  {
    if (Passkeys.Count == 0)
    {
      ExpandedId = null;
      return;
    }

    if (ExpandedId is Guid id && Passkeys.All(c => c.Id.Value != id))
    {
      ExpandedId = Passkeys[0].Id.Value;
    }
    else if (ExpandedId is null)
    {
      ExpandedId = Passkeys[0].Id.Value;
    }
  }
}
