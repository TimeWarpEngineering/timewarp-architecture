#region Purpose
// RoleForm (New Role) applies a Prefill.Role.CreateRole draft through the shared FormPrefillScope.
#endregion

#region Design
// Task 508. The point is that a form other than Profile opts in by rendering FormPrefillScope
// alone: RoleForm has no apply, dirty, or badge code of its own. RoleForm loads nothing, so the
// test renders it on the shared InteractiveHtmlRenderer with no scripted API. StaticHtmlRenderer
// never runs OnAfterRenderAsync, so the test runs it on each FormPrefillScope, which a recording
// IComponentActivator keeps as the renderer creates components. The draft goes into
// FormSuggestionState under CreateRole's key, as the Prefill tool stores it. A scripted
// IConfirmationService records the replace question. The command is private, so the test reads
// it by name.
#endregion

namespace RoleFormPrefill_;

using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features.Admin.Roles;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Services;
using TimeWarp.Architecture.Testing;

[TestTag("Unit")]
public class RoleForm_Should_
{
  private const string Draft = """{"name":"Auditor"}""";

  private static readonly string Form = FormSuggestionState.KeyFor(typeof(RoleState.CreateRoleActionSet.Action));

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<RoleForm_Should_>();

  public static async Task Apply_A_Draft_With_The_Agent_Badge()
  {
    using RoleSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);

    FormView view = await RenderAsync(spa, scope, form =>
    {
      ArgumentNullException.ThrowIfNull(form);
      return Task.CompletedTask;
    });

    view.Name.ShouldBe("Auditor");
    view.Description.ShouldBeNull();
    view.Confirmations.ShouldBe(0);
    view.Html.ShouldContain("data-qa=\"AgentFilled\"");
    scope.Store.GetState<FormSuggestionState>().For(Form).ShouldBeNull();
  }

  public static async Task Ask_Before_Replacing_Typed_Values()
  {
    using RoleSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    spa.Confirmation.Answer = false;

    FormView view = await RenderAsync(spa, scope, form =>
    {
      CommandOf(form).Name = "Typed";
      return Task.CompletedTask;
    });

    view.Name.ShouldBe("Typed");
    view.Confirmations.ShouldBe(1);
    view.Message.ShouldBe(
      "An agent drafted new role values. Applying them replaces unsaved changes on this form.");
    view.Html.ShouldNotContain("data-qa=\"AgentFilled\"");
    scope.Store.GetState<FormSuggestionState>().For(Form).ShouldBeNull();
  }

  private static async Task<FormView> RenderAsync(RoleSpa spa, SpaTestScope scope, Func<RoleForm, Task> beforeDraft)
  {
    IServiceProvider services = scope.ServiceProvider;
    FormCapture capture = new();
    await using InteractiveHtmlRenderer renderer = new(services, services.GetRequiredService<ILoggerFactory>());
    HtmlRootComponent root = await renderer.Dispatcher.InvokeAsync(() =>
      renderer.BeginRenderingComponent(
        typeof(FormHost),
        ParameterView.FromDictionary(new Dictionary<string, object?>
        {
          [nameof(FormHost.Capture)] = capture,
        })));

    await root.QuiescenceTask;
    RoleForm form = capture.Form ?? throw new InvalidOperationException("RoleForm did not render.");
    await AfterRenderAsync(spa);
    await beforeDraft(form);
    await scope.Send(new FormSuggestionState.SetFormSuggestionActionSet.Action(Form, Draft));
    await AfterRenderAsync(spa);

    form.ReRender();
    string html = await renderer.Dispatcher.InvokeAsync(() => WebUtility.HtmlDecode(root.ToHtmlString()));
    CreateRole.Command command = CommandOf(form);
    return new FormView(html, command.Name, command.Description, spa.Confirmation.Calls, spa.Confirmation.Message);
  }

  private static async Task AfterRenderAsync(RoleSpa spa)
  {
    foreach (ComponentBase prefillScope in spa.Activator.Scopes.ToArray())
    {
      await InteractiveHtmlRenderer.InvokeAfterRenderAsync(prefillScope);
    }
  }

  private static CreateRole.Command CommandOf(RoleForm form)
  {
    PropertyInfo property = typeof(RoleForm).GetProperty("Command", BindingFlags.Instance | BindingFlags.NonPublic)
      ?? throw new InvalidOperationException("Command");
    return (CreateRole.Command?)property.GetValue(form) ?? throw new InvalidOperationException("Command");
  }

  private sealed record FormView(string Html, string? Name, string? Description, int Confirmations, string? Message);

  private sealed class FormCapture
  {
    public RoleForm? Form { get; set; }
  }

  private sealed class FormHost : ComponentBase
  {
    [Parameter] public FormCapture? Capture { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
      builder.OpenComponent<CascadingAuthenticationState>(0);
      builder.AddAttribute(1, "ChildContent", (RenderFragment)(child =>
      {
        child.OpenComponent<RoleForm>(0);
        child.AddComponentReferenceCapture(1, instance =>
        {
          if (Capture is not null)
          {
            Capture.Form = (RoleForm)instance;
          }
        });
        child.CloseComponent();
      }));
      builder.CloseComponent();
    }
  }

  private sealed class RoleSpa : ISpaTestApplication, IDisposable
  {
    public RecordingConfirmation Confirmation { get; } = new();

    public ScopeRecordingActivator Activator { get; }

    public IServiceProvider ServiceProvider { get; }

    public RoleSpa()
    {
      ClaimsIdentity identity = new([new Claim(ClaimTypes.Name, "ada")], authenticationType: "test");
      ServiceCollection services = StaticRenderHost.CreateServices(user: new ClaimsPrincipal(identity));
      services.AddSingleton<IConfirmationService>(Confirmation);
      services.AddSingleton<IComponentActivator>(provider => new ScopeRecordingActivator(provider));
      ServiceProvider = services.BuildServiceProvider();
      Activator = (ScopeRecordingActivator)ServiceProvider.GetRequiredService<IComponentActivator>();
    }

    public void Dispose()
    {
      if (ServiceProvider is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }

  private sealed class ScopeRecordingActivator(IServiceProvider services) : IComponentActivator
  {
    public List<ComponentBase> Scopes { get; } = [];

    public IComponent CreateInstance(Type componentType)
    {
      ArgumentNullException.ThrowIfNull(componentType);
      var component = (IComponent)ActivatorUtilities.CreateInstance(services, componentType);
      if (component is IAgentFilledFields and ComponentBase scope)
      {
        Scopes.Add(scope);
      }

      return component;
    }
  }

  private sealed class RecordingConfirmation : IConfirmationService
  {
    public bool Answer { get; set; } = true;

    public int Calls { get; private set; }

    public string? Message { get; private set; }

    public Task<bool> ConfirmAsync
    (
      string title,
      string message,
      string confirmLabel,
      string cancelLabel,
      CancellationToken cancellationToken
    )
    {
      cancellationToken.ThrowIfCancellationRequested();
      Calls++;
      Message = message;
      _ = title;
      _ = confirmLabel;
      _ = cancelLabel;
      return Task.FromResult(Answer);
    }
  }
}
