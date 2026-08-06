#region Purpose
// Settings — signed-in passkey management (list / create / revoke) modeled on passkeys.io account UI.
#endregion

#region Design
// Task 167: product Settings is no longer a stub. Backend surface is 104-005
// (GetCredentials, AddPasskey, RevokeCredential). Layout mirrors the maintainer screenshot
// (Passkeys heading, expandable credential rows, Create a passkey link). Rename / last-used /
// emails / delete-account are out of scope until domain supports them (Credential.Label is
// set-at-create only; no LastUsedAt field). Create uses AddPasskey (attach to existing principal),
// never CompletePasskeyRegistration (which would mint a second account).
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using TimeWarp.Architecture.Features.Identity;
using TimeWarp.Architecture.Services;
using TimeWarp.Foundation.Types;
using TimeWarp.Identity;
using static TimeWarp.Architecture.Features.Identity.GetCredentials;

[Page("/Settings", Policy = Policies.CanViewSettings)]
[Authorize(Policy = Policies.CanViewSettings)]
partial class SettingsPage
{
  [Inject] private PasskeyCeremonyClient Ceremony { get; set; } = null!;

  private readonly List<CredentialSummary> Passkeys = [];
  private Guid? ExpandedId;
  private bool IsLoading = true;
  private bool IsBusy;
  private string? ErrorMessage;
  private string? StatusMessage;

  protected override async Task OnInitializedAsync()
  {
    await base.OnInitializedAsync();
    if (RendererInfo.IsInteractive)
    {
      await LoadCredentialsAsync();
    }
  }

  protected override async Task OnAfterRenderAsync(bool firstRender)
  {
    await base.OnAfterRenderAsync(firstRender);
    if (firstRender && RendererInfo.IsInteractive && IsLoading)
    {
      await LoadCredentialsAsync();
      await InvokeAsync(StateHasChanged);
    }
  }

  private async Task LoadCredentialsAsync()
  {
    IsLoading = true;
    ErrorMessage = null;
    try
    {
      OneOf<Response, SharedProblemDetails> result =
        await Ceremony.ListCredentialsAsync(CancellationToken.None);

      Passkeys.Clear();
      if (result.IsT1)
      {
        ErrorMessage = PasskeyCeremonyClient.FormatError(result.AsT1);
        return;
      }

      foreach (CredentialSummary credential in result.AsT0.Credentials
                 .Where(c => c.Type == CredentialType.Passkey && c.IsActive)
                 .OrderByDescending(c => c.CreatedAt))
      {
        Passkeys.Add(credential);
      }

      if (ExpandedId is Guid id && Passkeys.All(c => c.Id.Value != id))
      {
        ExpandedId = Passkeys.FirstOrDefault()?.Id.Value;
      }
      else if (ExpandedId is null && Passkeys.Count > 0)
      {
        ExpandedId = Passkeys[0].Id.Value;
      }
    }
    finally
    {
      IsLoading = false;
    }
  }

  private void ToggleExpanded(Guid id) =>
    ExpandedId = ExpandedId == id ? null : id;

  private static string DisplayLabel(CredentialSummary credential) =>
    string.IsNullOrWhiteSpace(credential.Label) ? "Passkey" : credential.Label!;

  private async Task CreatePasskeyAsync()
  {
    ErrorMessage = null;
    StatusMessage = null;
    IsBusy = true;
    try
    {
      OneOf<AddPasskey.Response, SharedProblemDetails> result =
        await Ceremony.AddPasskeyAsync(CancellationToken.None);

      if (result.IsT1)
      {
        ErrorMessage = PasskeyCeremonyClient.FormatError(result.AsT1);
        return;
      }

      StatusMessage = "Passkey created.";
      await LoadCredentialsAsync();
      ExpandedId = result.AsT0.CredentialId.Value;
    }
    catch (JSException jsException)
    {
      ErrorMessage = $"The browser could not complete the passkey ceremony: {jsException.Message}";
    }
    finally
    {
      IsBusy = false;
    }
  }

  private async Task ConfirmRevokeAsync(CredentialSummary credential)
  {
    ErrorMessage = null;
    StatusMessage = null;
    IsBusy = true;
    try
    {
      OneOf<RevokeCredential.Response, SharedProblemDetails> result =
        await Ceremony.RevokeCredentialAsync(credential.Id.Value, CancellationToken.None);

      if (result.IsT1)
      {
        ErrorMessage = PasskeyCeremonyClient.FormatError(result.AsT1);
        return;
      }

      StatusMessage = "Passkey deleted.";
      await LoadCredentialsAsync();
    }
    finally
    {
      IsBusy = false;
    }
  }
}
