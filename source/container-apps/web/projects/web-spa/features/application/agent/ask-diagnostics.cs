#region Purpose
// Development-only breadcrumbs for Ask tool calls and form-prefill decisions.
#endregion

#region Design
// The browser script posts window.__twBrowserDiag through the existing browser-log queue.
// This service writes one structured line only when the host environment is Development and
// a JavaScript runtime is present. Outside Development, and when JavaScript is missing or the
// page is still prerendering, every method returns without calling JavaScript and without throwing.
// Lines carry route, tool name, field names and counts, and state flags. Error text collapses to a
// stable code. Tokens, secrets, and typed field values are not written.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>
/// Posts one Ask or form-prefill breadcrumb through <c>window.__twBrowserDiag</c> in Development.
/// </summary>
[SideEffectService]
public sealed class AskDiagnostics
{
  public const string HookName = "__twBrowserDiag";

  public const string Level = "info";

  public static readonly AskDiagnostics Disabled = new(isDevelopment: false, jsRuntime: null);

  private readonly bool Enabled;

  private readonly IJSRuntime? JsRuntime;

  private readonly object Gate = new();

  private Task Tail = Task.CompletedTask;

  public AskDiagnostics(bool isDevelopment, IJSRuntime? jsRuntime)
  {
    JsRuntime = jsRuntime;
    Enabled = isDevelopment && jsRuntime is not null;
  }

  public void AgentBuilt(string? path, string? mode, int generation, int toolCount, IReadOnlyList<string>? prefillTools) =>
    Send(AgentBuiltLine(path, mode, generation, toolCount, prefillTools));

  public void AgentDeferred(string? reason, string? path, string? mode, int generation) =>
    Send(AgentDeferredLine(reason, path, mode, generation));

  public void Status(string? status, string? path) =>
    Send(StatusLine(status, path));

  public void ToolStart(string? toolName, string? route, bool wrapped) =>
    Send(ToolStartLine(toolName, route, wrapped));

  public void ToolOutcome
  (
    string? toolName,
    string? route,
    bool wrapped,
    bool currentFound,
    bool requiresApproval,
    string? offPageRoute,
    bool prefill,
    bool completed,
    string? error,
    IReadOnlyList<string>? fieldNames
  ) =>
    Send(ToolOutcomeLine(
      toolName,
      route,
      wrapped,
      currentFound,
      requiresApproval,
      offPageRoute,
      prefill,
      completed,
      error,
      fieldNames));

  public void PrefillSeen
  (
    string? form,
    string? draftId,
    bool ready,
    bool isInteractive,
    bool applying,
    bool alreadyHandled,
    bool isDirty,
    bool accepted
  ) =>
    Send(PrefillSeenLine(form, draftId, ready, isInteractive, applying, alreadyHandled, isDirty, accepted));

  public void PrefillOutcome(string? form, string? draftId, string? outcome, IReadOnlyList<string>? fieldNames) =>
    Send(PrefillOutcomeLine(form, draftId, outcome, fieldNames));

  public Task FlushAsync()
  {
    lock (Gate)
    {
      return Tail;
    }
  }

  public static string AgentBuiltLine
  (
    string? path,
    string? mode,
    int generation,
    int toolCount,
    IReadOnlyList<string>? prefillTools
  ) =>
    $"ask-diag event=agent-built path={Token(path)} mode={Token(mode)} generation={Count(generation)} toolCount={Count(toolCount)} prefillTools={FieldList(prefillTools)}";

  public static string AgentDeferredLine(string? reason, string? path, string? mode, int generation) =>
    $"ask-diag event=agent-deferred reason={Token(reason)} path={Token(path)} mode={Token(mode)} generation={Count(generation)}";

  public static string StatusLine(string? status, string? path) =>
    $"ask-diag event=status status={Token(status)} path={Token(path)}";

  public static string ToolStartLine(string? toolName, string? route, bool wrapped) =>
    $"ask-diag event=tool-start toolName={Token(toolName)} route={Token(route)} wrapped={Flag(wrapped)}";

  public static string ToolOutcomeLine
  (
    string? toolName,
    string? route,
    bool wrapped,
    bool currentFound,
    bool requiresApproval,
    string? offPageRoute,
    bool prefill,
    bool completed,
    string? error,
    IReadOnlyList<string>? fieldNames
  )
  {
    int fieldCount = fieldNames?.Count ?? 0;
    return
      $"ask-diag event=tool-outcome toolName={Token(toolName)} route={Token(route)} wrapped={Flag(wrapped)} currentFound={Flag(currentFound)} requiresApproval={Flag(requiresApproval)} offPageRoute={Token(offPageRoute)} prefill={Flag(prefill)} completed={Flag(completed)} error={ErrorCode(error)} fields={Count(fieldCount)} fieldNames={FieldList(fieldNames)}";
  }

  public static string PrefillSeenLine
  (
    string? form,
    string? draftId,
    bool ready,
    bool isInteractive,
    bool applying,
    bool alreadyHandled,
    bool isDirty,
    bool accepted
  ) =>
    $"ask-diag event=prefill-seen form={Token(form)} draftId={Token(draftId)} ready={Flag(ready)} isInteractive={Flag(isInteractive)} applying={Flag(applying)} alreadyHandled={Flag(alreadyHandled)} isDirty={Flag(isDirty)} accepted={Flag(accepted)}";

  public static string PrefillOutcomeLine
  (
    string? form,
    string? draftId,
    string? outcome,
    IReadOnlyList<string>? fieldNames
  ) =>
    $"ask-diag event=prefill-outcome form={Token(form)} draftId={Token(draftId)} outcome={Token(outcome)} fieldNames={FieldList(fieldNames)}";

  /// <summary>Collapses tool and prefill errors to a stable code. The original text is dropped.</summary>
  public static string ErrorCode(string? error)
  {
    if (string.IsNullOrWhiteSpace(error))
    {
      return "-";
    }

    if (string.Equals(error, CatalogAgentFunctions.ApprovalRequiredError, StringComparison.Ordinal))
    {
      return "ApprovalRequired";
    }

    if (string.Equals(error, "The action is not available on this page.", StringComparison.Ordinal))
    {
      return "NotAvailable";
    }

    if (string.Equals(error, AgentConversationAuthority.ExpiredError, StringComparison.Ordinal))
    {
      return "Expired";
    }

    if (string.Equals(error, AgentConversationAuthority.ScopeError, StringComparison.Ordinal))
    {
      return "Scope";
    }

    if (string.Equals(error, AgentConversationAuthority.PrincipalError, StringComparison.Ordinal))
    {
      return "Principal";
    }

    if (string.Equals(error, AgentNavigate.Refusal, StringComparison.Ordinal))
    {
      return "Refused";
    }

    if (string.Equals(error, AgentNavigate.UrlRequired, StringComparison.Ordinal))
    {
      return "UrlRequired";
    }

    if (string.Equals(error, "Prefill arguments must be a JSON object.", StringComparison.Ordinal))
    {
      return "NotObject";
    }

    if (error.StartsWith("No field matched.", StringComparison.Ordinal))
    {
      return "NoMatch";
    }

    if (error.Contains(" must be ", StringComparison.Ordinal) || error.Contains(" is not a valid ", StringComparison.Ordinal))
    {
      return "Type";
    }

    if (error.Contains(" cannot be null.", StringComparison.Ordinal))
    {
      return "Null";
    }

    return "Failed";
  }

  private void Send(string message)
  {
    if (!Enabled || JsRuntime is null)
    {
      return;
    }

    IJSRuntime runtime = JsRuntime;
    lock (Gate)
    {
      Tail = SendAfter(Tail, runtime, message);
    }
  }

  private static async Task SendAfter(Task previous, IJSRuntime runtime, string message)
  {
    await SwallowAsync(previous).ConfigureAwait(false);
    try
    {
      await runtime.InvokeVoidAsync(HookName, Level, message).ConfigureAwait(false);
    }
    catch (JSDisconnectedException)
    {
    }
    catch (JSException)
    {
    }
    catch (ObjectDisposedException)
    {
    }
    catch (InvalidOperationException)
    {
    }
    catch (OperationCanceledException)
    {
    }
  }

  private static async Task SwallowAsync(Task previous)
  {
    try
    {
      await previous.ConfigureAwait(false);
    }
    catch (JSDisconnectedException)
    {
    }
    catch (JSException)
    {
    }
    catch (ObjectDisposedException)
    {
    }
    catch (InvalidOperationException)
    {
    }
    catch (OperationCanceledException)
    {
    }
  }

  private static string Token(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return "-";
    }

    if (LooksSecret(value))
    {
      return "[redacted]";
    }

    string cleaned = value
      .Replace(" ", "", StringComparison.Ordinal)
      .Replace("\r", "", StringComparison.Ordinal)
      .Replace("\n", "", StringComparison.Ordinal)
      .Replace("\t", "", StringComparison.Ordinal)
      .Replace("=", "", StringComparison.Ordinal)
      .Replace("|", "", StringComparison.Ordinal);
    return cleaned.Length == 0 ? "-" : cleaned;
  }

  private static bool LooksSecret(string value) =>
    value.Contains("Bearer ", StringComparison.OrdinalIgnoreCase)
    || value.Contains("eyJ", StringComparison.Ordinal)
    || value.Contains("token=", StringComparison.OrdinalIgnoreCase)
    || value.Contains("password=", StringComparison.OrdinalIgnoreCase)
    || value.Contains("secret=", StringComparison.OrdinalIgnoreCase);

  private static string FieldList(IReadOnlyList<string>? names)
  {
    if (names is null || names.Count == 0)
    {
      return "-";
    }

    string[] tokens = new string[names.Count];
    for (int index = 0; index < names.Count; index++)
    {
      tokens[index] = Token(names[index]);
    }

    return string.Join('|', tokens);
  }

  private static string Flag(bool value) => value ? "true" : "false";

  private static string Count(int value) => value.ToString(CultureInfo.InvariantCulture);
}
