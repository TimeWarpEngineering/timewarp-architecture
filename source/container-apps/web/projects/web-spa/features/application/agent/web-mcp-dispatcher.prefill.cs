#region Purpose
// Validates a WebMCP prefill call, waits for approval, and stores the draft.
#endregion

#region Design
// Validation runs before the prompt, so a draft that cannot apply never asks. The prompt shows
// the pretty-printed suggestion. Approval follows the same page-bound gate as other tools. The
// suggestion action is what executes. The submit action is not saved.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class WebMcpDispatcher
{
  private async Task<string> PrefillAsync(string path, string name, CatalogAgentTool tool, string? argumentsJson)
  {
    if (tool.Entry is null)
    {
      return Error(name, UnavailableError);
    }

    EditFormSubmitAttribute? submit = tool.Entry.ActionType.GetCustomAttribute<EditFormSubmitAttribute>();
    if (submit is null)
    {
      return Error(name, UnavailableError);
    }

    if (!FormPrefill.TryNormalize(
      submit.CommandType,
      argumentsJson,
      out string suggestionJson,
      out string? error,
      out IReadOnlyList<string> fields))
    {
      return Serialize(new FormPrefill.Result(Drafted: false, error, fields));
    }

    if (tool.RequiresApproval)
    {
      if (!Gate.TryBegin(out Guid callId, out Task<bool> decision))
      {
        return Error(name, BusyError);
      }

      bool pageBound = PageAgentScope.IsPageBound(tool.Entry.ActionType);
      (bool approved, bool navigated) = await WaitForApprovalAsync
      (
        callId,
        decision,
        name,
        FormPrefill.Render(suggestionJson),
        pageBound
      );
      if (pageBound && (navigated || !IsOnPath(path)))
      {
        return Error(name, PageChangedError);
      }

      if (!approved)
      {
        return Serialize(new WebMcpRejected(name, Approved: false));
      }

      if (pageBound)
      {
        CatalogAgentTool? current = await FindOfferedAsync(path, name);
        if (current?.Entry is null || current.OffPageRoute is not null || !current.Prefill)
        {
          return Error(name, UnavailableError);
        }

        if (!IsOnPath(path))
        {
          return Error(name, PageChangedError);
        }

        tool = current;
      }
      else
      {
        string currentPath = Route.PathOr(Navigation);
        CatalogAgentTool? current = await FindOfferedAsync(currentPath, name);
        if (current?.Entry is null || !current.Prefill)
        {
          return Error(name, UnavailableError);
        }

        tool = current;
      }
    }

    if (tool.Entry is null)
    {
      return Error(name, UnavailableError);
    }

    string? denial = await CredentialDenialAsync(tool.Entry.Permissions);
    if (denial is not null)
    {
      return Error(name, denial);
    }

    FormPrefill.Result drafted = await FormPrefill.DispatchAsync
    (
      Catalog,
      Store,
      tool.Entry,
      suggestionJson,
      fields,
      CancellationToken.None
    );
    return Serialize(drafted);
  }
}
