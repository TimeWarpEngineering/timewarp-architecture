#region Purpose
// Task 536-003: Ask diagnostic lines stay structural, and the browser forwarder posts only the explicit hook.
#endregion

#region Design
// Formatting is pure, so the secret and field-value checks do not need a host. The Development
// gate is the service constructor: a non-Development instance never calls JavaScript, and a
// Development instance posts level info through __twBrowserDiag and swallows a missing runtime.
// The forwarder check runs the emitted classic script under node. The wiring proofs live in
// ask-diagnostics-wiring-tests.cs. This file is a project test, not a standalone runfile:
// the integration project compiles it, and CI runs that project.
#endregion

namespace AskDiagnostics_;

using System.Text.Json;
using Microsoft.JSInterop;
using TimeWarp.Amuru;
using TimeWarp.Architecture.Features.Applications;

[TestTag("Unit")]
public partial class AskDiagnostics_Should_
{
  private const string Secret = "super-secret-token-value";

  private const string Bearer = "Bearer abc.DEF-123_xyz";

  private const string Person = "Ada Lovelace";

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<AskDiagnostics_Should_>();

  public static Task Format_Includes_Structural_Fields_And_Drops_Secrets()
  {
    string built = AskDiagnostics.AgentBuiltLine(
      "/admin/roles/new?access_token=" + Bearer,
      "AskBeforeEditing",
      generation: 3,
      toolCount: 4,
      prefillTools: ["Prefill.Role.CreateRole"]);
    built.ShouldContain("event=agent-built");
    built.ShouldContain("path=[redacted]");
    built.ShouldContain("mode=AskBeforeEditing");
    built.ShouldContain("generation=3");
    built.ShouldContain("toolCount=4");
    built.ShouldContain("prefillTools=Prefill.Role.CreateRole");
    AssertNoSecrets(built);

    string deferred = AskDiagnostics.AgentDeferredLine("TurnInProgress", "/feedback", "AutomaticallyEdit", 2);
    deferred.ShouldContain("event=agent-deferred");
    deferred.ShouldContain("reason=TurnInProgress");
    deferred.ShouldContain("path=/feedback");
    deferred.ShouldContain("generation=2");

    string status = AskDiagnostics.StatusLine("Streaming", "/admin/roles?secret=" + Secret);
    status.ShouldContain("event=status");
    status.ShouldContain("status=Streaming");
    status.ShouldContain("path=[redacted]");
    AssertNoSecrets(status);

    const string rawError = "Json reader failed near " + Person + " with " + Secret;
    string failed = AskDiagnostics.ToolOutcomeLine(
      "eyJhbGciOiJIUzI1NiJ9.payload",
      "/admin/roles",
      wrapped: false,
      currentFound: true,
      requiresApproval: false,
      offPageRoute: null,
      prefill: true,
      completed: false,
      error: rawError,
      fieldNames: ["name", "description"]);
    failed.ShouldContain("event=tool-outcome");
    failed.ShouldContain("toolName=[redacted]");
    failed.ShouldContain("error=Failed");
    failed.ShouldContain("fields=2");
    failed.ShouldContain("fieldNames=name|description");
    failed.ShouldContain("prefill=true");
    failed.ShouldContain("completed=false");
    failed.ShouldNotContain("{");
    failed.ShouldNotContain("\"");
    AssertNoSecrets(failed);

    string approval = AskDiagnostics.ToolOutcomeLine(
      "Prefill.Role.CreateRole",
      "/admin/roles",
      wrapped: true,
      currentFound: true,
      requiresApproval: true,
      offPageRoute: "/admin/roles/new",
      prefill: true,
      completed: false,
      error: CatalogAgentFunctions.ApprovalRequiredError,
      fieldNames: ["name"]);
    approval.ShouldContain("error=ApprovalRequired");
    approval.ShouldContain("wrapped=true");
    approval.ShouldContain("requiresApproval=true");
    approval.ShouldContain("offPageRoute=/admin/roles/new");
    approval.ShouldNotContain("ask again");
    AssertNoSecrets(approval);

    string noMatch = AskDiagnostics.ToolOutcomeLine(
      "Prefill.Role.CreateRole",
      "/admin/roles",
      wrapped: false,
      currentFound: true,
      requiresApproval: false,
      offPageRoute: null,
      prefill: true,
      completed: false,
      error: FormPrefill.NoFieldMatchedPrefix + " Accepted keys: name. " + Person,
      fieldNames: []);
    noMatch.ShouldContain("error=NoMatch");
    noMatch.ShouldContain("fieldNames=-");
    noMatch.ShouldNotContain("Accepted keys");
    AssertNoSecrets(noMatch);

    AskDiagnostics.ErrorCode("name cannot be null.").ShouldBe("Null");
    AskDiagnostics.ErrorCode("name must be a string.").ShouldBe("Type");
    AskDiagnostics.ErrorCode(FormPrefill.ArgumentsMustBeObjectError).ShouldBe("NotObject");
    AskDiagnostics.ErrorCode(WebMcpDispatcher.UnavailableError).ShouldBe("NotAvailable");
    AskDiagnostics.ErrorCode(FormPrefill.NoFieldMatchedPrefix + " Accepted keys: name.").ShouldBe("NoMatch");
    AskDiagnostics.ErrorCode("ArgumentException").ShouldBe("ArgumentException");
    AskDiagnostics.ErrorCode("not an exception").ShouldBe("Failed");

    string seen = AskDiagnostics.PrefillSeenLine(
      "TimeWarp.Architecture.Features.Admin.Roles.RoleState+CreateRoleActionSet+Action",
      "abc123",
      ready: true,
      isInteractive: true,
      applying: false,
      alreadyHandled: false,
      isDirty: true,
      accepted: false);
    seen.ShouldContain("event=prefill-seen");
    seen.ShouldContain("draftId=abc123");
    seen.ShouldContain("ready=true");
    seen.ShouldContain("isDirty=true");
    seen.ShouldContain("accepted=false");
    AssertNoSecrets(seen);

    string applied = AskDiagnostics.PrefillOutcomeLine(
      "RoleState.CreateRole",
      "abc123",
      "applied",
      ["name", "description"]);
    applied.ShouldContain("event=prefill-outcome");
    applied.ShouldContain("outcome=applied");
    applied.ShouldContain("fieldNames=name|description");
    applied.ShouldNotContain("{");
    applied.ShouldNotContain("\"");
    AssertNoSecrets(applied);
    return Task.CompletedTask;
  }

  public static async Task Write_Outside_Development_Does_Not_Call_Js()
  {
    RecordingRuntime runtime = new()
    {
      Failure = new InvalidOperationException("JavaScript interop is not available."),
    };
    AskDiagnostics diagnostics = new(isDevelopment: false, runtime);
    diagnostics.IsEnabled.ShouldBeFalse();
    diagnostics.AgentBuilt("/admin/roles", "AskBeforeEditing", 1, 2, ["Prefill.Role.CreateRole"]);
    diagnostics.AgentDeferred("Readiness", "/admin/roles", "AskBeforeEditing", 1);
    diagnostics.Status("Error", "/admin/roles");
    diagnostics.ToolStart("Prefill.Role.CreateRole", "/admin/roles", wrapped: true);
    diagnostics.ToolOutcome(
      "Prefill.Role.CreateRole",
      "/admin/roles",
      wrapped: true,
      currentFound: false,
      requiresApproval: true,
      offPageRoute: null,
      prefill: true,
      completed: false,
      error: Person,
      fieldNames: [Secret]);
    diagnostics.PrefillSeen("form", "draft", true, true, false, false, true, false);
    diagnostics.PrefillOutcome("form", "draft", "declined", [Person]);
    await diagnostics.FlushAsync().ConfigureAwait(false);
    runtime.Calls.Count.ShouldBe(0);

    AskDiagnostics missing = new(isDevelopment: true, jsRuntime: null);
    missing.IsEnabled.ShouldBeFalse();
    missing.Status("Idle", "/admin/roles");
    await missing.FlushAsync().ConfigureAwait(false);
  }

  public static async Task Write_In_Development_Posts_Info_And_Swallows_Js_Failures()
  {
    RecordingRuntime runtime = new();
    AskDiagnostics diagnostics = new(isDevelopment: true, runtime);
    diagnostics.IsEnabled.ShouldBeTrue();
    diagnostics.Status("Streaming", "/admin/roles");
    await diagnostics.FlushAsync().ConfigureAwait(false);
    runtime.Calls.Count.ShouldBe(1);
    JsCall call = runtime.Calls[0];
    call.Identifier.ShouldBe(AskDiagnostics.HookName);
    call.Arguments[0].ShouldBe(AskDiagnostics.Level);
    string message = call.Arguments[1].ShouldBeOfType<string>();
    message.ShouldContain("event=status");
    message.ShouldContain("status=Streaming");

    Exception[] failures =
    [
      new JSException("js"),
      new JSDisconnectedException("gone"),
      new InvalidOperationException("prerender"),
    ];
    foreach (Exception failure in failures)
    {
      runtime.Failure = failure;
      diagnostics.Status("Idle", "/admin/roles");
      await diagnostics.FlushAsync().ConfigureAwait(false);
    }

    runtime.Calls.Count.ShouldBe(1 + failures.Length);
  }

  public static async Task Forwarder_Posts_Info_Diagnostics_And_Ignores_Console_Log_And_Info()
  {
    string sourcePath = FindRepoFile(
      "source",
      "container-apps",
      "web",
      "projects",
      "web-spa",
      "source",
      "browser-log-forwarder.ts");
    string source = await File.ReadAllTextAsync(sourcePath).ConfigureAwait(false);
    source.ShouldContain("window.__twBrowserDiag");
    source.ShouldContain("wrap('error', 'error')");
    source.ShouldContain("wrap('warn', 'warn')");
    source.ShouldNotContain("console.log =");
    source.ShouldNotContain("console.info =");
    source.ShouldNotContain("wrap('log'");
    source.ShouldNotContain("wrap('info'");

    string scriptPath = FindRepoFile(
      "source",
      "container-apps",
      "web",
      "projects",
      "web-spa",
      "wwwroot",
      "js",
      "browser-log-forwarder.js");
    string harness = ForwarderHarness.Replace("__JS_PATH__", JsonSerializer.Serialize(scriptPath), StringComparison.Ordinal);
    string harnessPath = Path.Combine(Path.GetTempPath(), "ask-diag-" + Guid.NewGuid().ToString("N") + ".js");
    await File.WriteAllTextAsync(harnessPath, harness).ConfigureAwait(false);
    try
    {
      CommandOutput output = await Shell.Builder("node")
        .WithArguments(harnessPath)
        .WithTimeout(TimeSpan.FromSeconds(30))
        .WithNoValidation()
        .CaptureAsync()
        .ConfigureAwait(false);
      output.Success.ShouldBeTrue(output.Combined);
      ForwarderProbe? probe = JsonSerializer.Deserialize<ForwarderProbe>(output.Stdout);
      probe.ShouldNotBeNull(output.Combined);
      probe.Hook.ShouldBe("function");
      probe.Posted.ShouldContain("\"level\":\"info\"");
      probe.Posted.ShouldContain("\"source\":\"console\"");
      probe.Posted.ShouldContain("ask-diag event=status status=Streaming");
      probe.Posted.ShouldContain("ask-diag event=level-coerced");
      probe.Posted.ShouldNotContain("plain-log-marker");
      probe.Posted.ShouldNotContain("plain-info-marker");
    }
    finally
    {
      File.Delete(harnessPath);
    }
  }

  private static void AssertNoSecrets(string line)
  {
    line.ShouldNotContain(Secret);
    line.ShouldNotContain(Bearer);
    line.ShouldNotContain("eyJ");
    line.ShouldNotContain(Person);
    line.ShouldNotContain("AdaLovelace");
  }

  private static string FindRepoFile(params string[] segments)
  {
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory is not null)
    {
      string candidate = directory.FullName;
      foreach (string segment in segments)
      {
        candidate = Path.Combine(candidate, segment);
      }

      if (File.Exists(candidate))
      {
        return candidate;
      }

      directory = directory.Parent;
    }

    throw new FileNotFoundException(string.Join('/', segments));
  }

  private const string ForwarderHarness = """
    const fs = require('fs');
    const vm = require('vm');
    const jsPath = __JS_PATH__;
    const posted = [];
    const listeners = {};
    const fakeConsole = {
      error: function () {},
      warn: function () {},
      log: function () {},
      info: function () {},
    };
    const sandbox = {
      console: fakeConsole,
      location: { pathname: '/admin/roles/new' },
      document: { baseURI: 'https://localhost/' },
      fetch: function (url, init) {
        posted.push(String(init && init.body));
        return Promise.resolve({ status: 204 });
      },
      setTimeout: function () { return 1; },
      clearTimeout: function () {},
      addEventListener: function (type, listener) { listeners[type] = listener; },
      URL: URL,
      Date: Date,
      JSON: JSON,
      Error: Error,
      String: String,
      Object: Object,
      Promise: Promise,
    };
    sandbox.window = sandbox;
    sandbox.globalThis = sandbox;
    vm.runInNewContext(fs.readFileSync(jsPath, 'utf8'), sandbox, { filename: jsPath });
    sandbox.console.log('plain-log-marker');
    sandbox.console.info('plain-info-marker');
    if (typeof sandbox.__twBrowserDiag !== 'function') {
      throw new Error('missing __twBrowserDiag');
    }
    sandbox.__twBrowserDiag('info', 'ask-diag event=status status=Streaming');
    sandbox.__twBrowserDiag('nope', 'ask-diag event=level-coerced');
    if (typeof listeners.pagehide !== 'function') {
      throw new Error('missing pagehide listener');
    }
    listeners.pagehide();
    process.stdout.write(JSON.stringify({
      Posted: posted.join('\n'),
      Hook: typeof sandbox.__twBrowserDiag,
    }));
    """;

  private sealed record ForwarderProbe(string Posted, string Hook);

}
