#region Purpose
// TWA0026/TWA0027 exemption, opt-out, and gating cases.
#endregion

#region Design
// Each target category is exercised in an event handler AND a lifecycle override of one component
// so "every component member" scope is pinned, not just handlers. Expected locations use
// {|#n:...|} markup (the invocation / method-group syntax) so the tests survive stub edits.
// Framework types are stubbed under their real namespaces (ComponentBase, NavigationManager,
// IJSRuntime, Blazored storage, TimeWarp.Foundation.IApiService); HttpClient is the real BCL type.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;
using static TimeWarp.Architecture.Analyzers.Tests.ComponentSideEffectTestFixture;

public class Should_Ban_Direct_Side_Effects_In_Components_With_Exemptions
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Ban_Direct_Side_Effects_In_Components_With_Exemptions>();

  public static async Task Given_Handler_Service_And_Nested_NonComponent_Code_Does_Not_Flag()
  {
    const string source =
      """
      using System.Threading;
      using System.Threading.Tasks;
      using App.Services;
      using Blazored.SessionStorage;
      using Microsoft.AspNetCore.Components;
      using Microsoft.JSInterop;

      public abstract class BaseHandler<TAction>
      {
        public abstract Task Handle(TAction action, CancellationToken cancellationToken);
      }

      public sealed class SignInHandler(NavigationManager nav, IJSRuntime js, ISessionStorageService session, PasskeyCeremonyClient ceremony)
        : BaseHandler<object>
      {
        public override async Task Handle(object action, CancellationToken cancellationToken)
        {
          _ = await ceremony.AuthenticateAsync(cancellationToken);
          await js.InvokeVoidAsync("x");
          await session.SetItemAsync("later", true, cancellationToken);
          nav.NavigateTo("/", forceLoad: true);
        }
      }

      public static class JsHelper
      {
        public static async Task Focus(IJSRuntime js) => await js.InvokeVoidAsync("focus");
      }

      public class HostComponent : ComponentBase
      {
        private sealed class Worker(NavigationManager nav)
        {
          public void Go() => nav.NavigateTo("/");
        }
      }
      """;

    await Test(source).RunAsync();
  }

  public static async Task Given_Reasoned_OptOut_On_Class_Or_Member_Does_Not_Flag()
  {
    const string source =
      """
      using System.Threading.Tasks;
      using Microsoft.AspNetCore.Components;
      using Microsoft.JSInterop;
      using TimeWarp.Architecture.Attributes;

      [DirectComponentSideEffect("Hotkey and focus wiring is presentational JS.")]
      public partial class CommandPalette : ComponentBase
      {
        private IJSRuntime Js { get; set; } = null!;

        protected override async Task OnAfterRenderAsync(bool firstRender) =>
          await Js.InvokeVoidAsync("register");
      }

      public class RedirectComponent : ComponentBase
      {
        private NavigationManager Nav { get; set; } = null!;

        [DirectComponentSideEffect("Full-document load the store cannot express.")]
        protected override void OnInitialized() => Nav.NavigateTo("/Login", forceLoad: true);

        [DirectComponentSideEffect("Property-scoped opt-out.")]
        private System.Action Go => () => Nav.NavigateTo("/");

        private void Other() => {|#0:Nav.NavigateTo("/x")|};
      }
      """;

    CSharpAnalyzerTest<ComponentSideEffectAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "NavigationManager.NavigateTo"));
    await test.RunAsync();
  }

  public static async Task Given_Empty_OptOut_Reason_Reports_And_Still_Flags()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;
      using TimeWarp.Architecture.Attributes;

      [{|#0:DirectComponentSideEffect("  ")|}]
      public class LazyComponent : ComponentBase
      {
        private NavigationManager Nav { get; set; } = null!;

        [{|#1:DirectComponentSideEffect("")|}]
        protected override void OnInitialized() => {|#2:Nav.NavigateTo("/")|};
      }
      """;

    CSharpAnalyzerTest<ComponentSideEffectAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(EmptyReason(0, "LazyComponent"));
    test.ExpectedDiagnostics.Add(EmptyReason(1, "OnInitialized"));
    test.ExpectedDiagnostics.Add(Flag(2, "NavigationManager.NavigateTo"));
    await test.RunAsync();
  }

  public static async Task Given_Razor_Generated_Tree_Flags()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;

      public partial class CounterPage : ComponentBase
      {
        private NavigationManager Nav { get; set; } = null!;

        protected override void OnInitialized() => {|#0:Nav.NavigateTo("/")|};
      }
      """;

    CSharpAnalyzerTest<ComponentSideEffectAnalyzer, RoslynTestVerifier> test =
      Test(source, "Features_Counter_Pages_CounterPage_razor.g.cs");
    test.ExpectedDiagnostics.Add(Flag(0, "NavigationManager.NavigateTo"));
    await test.RunAsync();
  }

  public static async Task Given_Other_Generated_Tree_Does_Not_Flag()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;

      public partial class GeneratedComponent : ComponentBase
      {
        private NavigationManager Nav { get; set; } = null!;

        protected override void OnInitialized() => Nav.NavigateTo("/");
      }
      """;

    await Test(source, "GeneratedComponent.g.cs").RunAsync();
  }

  public static async Task Given_No_Blazor_WebAssembly_Property_Does_Not_Flag()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;
      using TimeWarp.Architecture.Attributes;

      [DirectComponentSideEffect("")]
      public class ServerComponent : ComponentBase
      {
        private NavigationManager Nav { get; set; } = null!;

        protected override void OnInitialized() => Nav.NavigateTo("/");
      }
      """;

    await Test(source, globalConfig: null).RunAsync();
  }

  public static async Task Given_Blazor_WebAssembly_Property_False_Does_Not_Flag()
  {
    const string source =
      """
      using Microsoft.AspNetCore.Components;

      public class ServerComponent : ComponentBase
      {
        private NavigationManager Nav { get; set; } = null!;

        protected override void OnInitialized() => Nav.NavigateTo("/");
      }
      """;

    const string falseConfig =
      """
      is_global = true
      build_property.UsingMicrosoftNETSdkBlazorWebAssembly = false
      """;

    await Test(source, globalConfig: falseConfig).RunAsync();
  }
}
