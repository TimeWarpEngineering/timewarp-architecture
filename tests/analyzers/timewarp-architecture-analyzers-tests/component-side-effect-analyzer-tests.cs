#region Purpose
// TWA0026: SPA components only dispatch actions — navigation, JS interop, API/HttpClient calls,
// browser-storage writes and [SideEffectService] calls from a ComponentBase member are flagged.
// TWA0027: the [DirectComponentSideEffect] opt-out needs a non-empty reason.
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

public class Should_Ban_Direct_Side_Effects_In_Components
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Ban_Direct_Side_Effects_In_Components>();

  private const string SpaGlobalConfig =
    """
    is_global = true
    build_property.UsingMicrosoftNETSdkBlazorWebAssembly = true
    """;

  private const string Stubs =
    """
    using System.Threading;
    using System.Threading.Tasks;

    namespace Microsoft.AspNetCore.Components
    {
      public abstract class ComponentBase
      {
        protected virtual void OnInitialized() { }
        protected virtual Task OnInitializedAsync() => Task.CompletedTask;
        protected virtual Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;
      }

      public abstract class NavigationManager
      {
        public string Uri => "";
        public void NavigateTo(string uri, bool forceLoad = false) { }
        public void Refresh(bool forceReload = false) { }
        public string ToBaseRelativePath(string uri) => uri;
      }
    }

    namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
    {
      public static class NavigationManagerExtensions
      {
        public static void NavigateToLogin(this Microsoft.AspNetCore.Components.NavigationManager manager, string loginPath) { }
      }
    }

    namespace Microsoft.JSInterop
    {
      public interface IJSRuntime
      {
        ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args);
      }

      public interface IJSObjectReference : System.IAsyncDisposable
      {
        ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args);
      }

      public static class JSRuntimeExtensions
      {
        public static ValueTask InvokeVoidAsync(this IJSRuntime jsRuntime, string identifier, params object[] args) => default;
      }

      public static class JSObjectReferenceExtensions
      {
        public static ValueTask InvokeVoidAsync(this IJSObjectReference reference, string identifier, params object[] args) => default;
      }
    }

    namespace TimeWarp.Foundation
    {
      public interface IApiService
      {
        Task<object> GetResponse<TResponse>(object request, CancellationToken cancellationToken);
      }
    }

    namespace App.Services
    {
      public interface IWebServerApiService : TimeWarp.Foundation.IApiService { }
    }

    namespace Blazored.SessionStorage
    {
      public interface ISessionStorageService
      {
        ValueTask SetItemAsync<T>(string key, T data, CancellationToken cancellationToken = default);
        ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default);
        ValueTask ClearAsync(CancellationToken cancellationToken = default);
        ValueTask<T> GetItemAsync<T>(string key, CancellationToken cancellationToken = default);
        ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default);
      }
    }

    namespace Blazored.LocalStorage
    {
      public interface ILocalStorageService
      {
        ValueTask SetItemAsStringAsync(string key, string data, CancellationToken cancellationToken = default);
        ValueTask<string> GetItemAsStringAsync(string key, CancellationToken cancellationToken = default);
      }
    }

    namespace TimeWarp.Architecture.Attributes
    {
      [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = false, Inherited = false)]
      public sealed class DirectComponentSideEffectAttribute : System.Attribute
      {
        public DirectComponentSideEffectAttribute(string reason) => Reason = reason;
        public string Reason { get; }
      }

      [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Interface)]
      public sealed class SideEffectServiceAttribute : System.Attribute { }
    }

    namespace App.Services
    {
      [TimeWarp.Architecture.Attributes.SideEffectService]
      public sealed class PasskeyCeremonyClient
      {
        public Task<bool> AuthenticateAsync(CancellationToken cancellationToken) => Task.FromResult(true);
      }

      public sealed class PlainHelper
      {
        public string Format(string value) => value;
      }
    }
    """;

  private static CSharpAnalyzerTest<ComponentSideEffectAnalyzer, RoslynTestVerifier> Test
  (
    string source,
    string sourcePath = "Feature.cs",
    string? globalConfig = SpaGlobalConfig
  )
  {
    CSharpAnalyzerTest<ComponentSideEffectAnalyzer, RoslynTestVerifier> test = new();
    if (globalConfig is not null)
    {
      test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
    }

    test.TestState.Sources.Add(("Stubs.cs", Stubs));
    test.TestState.Sources.Add((sourcePath, source));
    return test;
  }

  private static DiagnosticResult Flag(int location, string member) =>
    new DiagnosticResult(id: "TWA0026", DiagnosticSeverity.Warning)
      .WithLocation(location)
      .WithArguments(member);

  private static DiagnosticResult EmptyReason(int location, string symbol) =>
    new DiagnosticResult(id: "TWA0027", DiagnosticSeverity.Warning)
      .WithLocation(location)
      .WithArguments(symbol);

  public static async Task Given_Navigation_In_Handler_And_Lifecycle_Flags()
  {
    const string source =
      """
      using System.Threading.Tasks;
      using Microsoft.AspNetCore.Components;
      using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

      public class LoginPage : ComponentBase
      {
        private NavigationManager Nav { get; set; } = null!;

        protected override Task OnInitializedAsync()
        {
          {|#0:Nav.NavigateTo("/")|};
          return Task.CompletedTask;
        }

        private void OnClick()
        {
          {|#1:Nav.Refresh()|};
          {|#2:Nav.NavigateToLogin("/Login")|};
          _ = Nav.ToBaseRelativePath(Nav.Uri);
        }
      }
      """;

    CSharpAnalyzerTest<ComponentSideEffectAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "NavigationManager.NavigateTo"));
    test.ExpectedDiagnostics.Add(Flag(1, "NavigationManager.Refresh"));
    test.ExpectedDiagnostics.Add(Flag(2, "NavigationManager.NavigateToLogin"));
    await test.RunAsync();
  }

  public static async Task Given_JsInterop_In_Handler_And_Lifecycle_Flags()
  {
    const string source =
      """
      using System.Threading.Tasks;
      using Microsoft.AspNetCore.Components;
      using Microsoft.JSInterop;

      public class CounterPage : ComponentBase
      {
        private IJSRuntime Js { get; set; } = null!;
        private IJSObjectReference? Module;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
          Module = await {|#0:Js.InvokeAsync<IJSObjectReference>("import", new object[] { "./x.js" })|};
          await {|#1:Module.InvokeVoidAsync("focus")|};
        }

        private async Task OnClick()
        {
          await {|#2:Js.InvokeVoidAsync("Spa.Counter.DispatchIncrementCountAction")|};
          if (Module is not null) await Module.DisposeAsync();
        }
      }
      """;

    CSharpAnalyzerTest<ComponentSideEffectAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "IJSRuntime.InvokeAsync"));
    test.ExpectedDiagnostics.Add(Flag(1, "IJSObjectReference.InvokeVoidAsync"));
    test.ExpectedDiagnostics.Add(Flag(2, "IJSRuntime.InvokeVoidAsync"));
    await test.RunAsync();
  }

  public static async Task Given_ApiService_And_Subtype_In_Handler_And_Lifecycle_Flags()
  {
    const string source =
      """
      using System.Threading;
      using System.Threading.Tasks;
      using App.Services;
      using Microsoft.AspNetCore.Components;
      using TimeWarp.Foundation;

      public class ProfilePage : ComponentBase
      {
        private IApiService Api { get; set; } = null!;
        private IWebServerApiService WebApi { get; set; } = null!;

        protected override async Task OnInitializedAsync() =>
          await {|#0:Api.GetResponse<string>(new object(), CancellationToken.None)|};

        private async Task OnSave() =>
          await {|#1:WebApi.GetResponse<string>(new object(), CancellationToken.None)|};
      }
      """;

    CSharpAnalyzerTest<ComponentSideEffectAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "IApiService.GetResponse"));
    test.ExpectedDiagnostics.Add(Flag(1, "IApiService.GetResponse"));
    await test.RunAsync();
  }

  public static async Task Given_HttpClient_In_Handler_And_Lifecycle_Flags()
  {
    const string source =
      """
      using System.Net.Http;
      using System.Threading.Tasks;
      using Microsoft.AspNetCore.Components;

      public class WeatherPage : ComponentBase
      {
        private HttpClient Http { get; set; } = null!;

        protected override async Task OnInitializedAsync() =>
          _ = await {|#0:Http.GetStringAsync("api/weather")|};

        private async Task OnDelete()
        {
          _ = await {|#1:Http.DeleteAsync("api/weather/1")|};
          _ = Http.BaseAddress;
        }
      }
      """;

    CSharpAnalyzerTest<ComponentSideEffectAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "HttpClient.GetStringAsync"));
    test.ExpectedDiagnostics.Add(Flag(1, "HttpClient.DeleteAsync"));
    await test.RunAsync();
  }

  public static async Task Given_Storage_Writes_Flag_And_Reads_Do_Not()
  {
    const string source =
      """
      using System.Threading.Tasks;
      using Blazored.LocalStorage;
      using Blazored.SessionStorage;
      using Microsoft.AspNetCore.Components;

      public class PromptBanner : ComponentBase
      {
        private ISessionStorageService Session { get; set; } = null!;
        private ILocalStorageService Local { get; set; } = null!;

        protected override async Task OnInitializedAsync()
        {
          _ = await Session.ContainKeyAsync("later");
          _ = await Session.GetItemAsync<bool>("later");
          await {|#0:Session.RemoveItemAsync("later")|};
        }

        private async Task OnLater()
        {
          await {|#1:Session.SetItemAsync("later", true)|};
          await {|#2:Session.ClearAsync()|};
          await {|#3:Local.SetItemAsStringAsync("k", "v")|};
          _ = await Local.GetItemAsStringAsync("k");
        }
      }
      """;

    CSharpAnalyzerTest<ComponentSideEffectAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "ISessionStorageService.RemoveItemAsync"));
    test.ExpectedDiagnostics.Add(Flag(1, "ISessionStorageService.SetItemAsync"));
    test.ExpectedDiagnostics.Add(Flag(2, "ISessionStorageService.ClearAsync"));
    test.ExpectedDiagnostics.Add(Flag(3, "ILocalStorageService.SetItemAsStringAsync"));
    await test.RunAsync();
  }

  public static async Task Given_SideEffectService_In_Handler_And_Lifecycle_Flags()
  {
    const string source =
      """
      using System.Threading;
      using System.Threading.Tasks;
      using App.Services;
      using Microsoft.AspNetCore.Components;

      public class PasskeysPage : ComponentBase
      {
        private PasskeyCeremonyClient Ceremony { get; set; } = null!;
        private PlainHelper Helper { get; set; } = null!;

        protected override async Task OnInitializedAsync() =>
          _ = await {|#0:Ceremony.AuthenticateAsync(CancellationToken.None)|};

        private async Task OnSignIn()
        {
          _ = await {|#1:Ceremony.AuthenticateAsync(CancellationToken.None)|};
          _ = Helper.Format("unmarked services are not side effects");
        }
      }
      """;

    CSharpAnalyzerTest<ComponentSideEffectAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "PasskeyCeremonyClient.AuthenticateAsync"));
    test.ExpectedDiagnostics.Add(Flag(1, "PasskeyCeremonyClient.AuthenticateAsync"));
    await test.RunAsync();
  }

  public static async Task Given_Method_Group_And_Lambda_In_Component_Flags()
  {
    const string source =
      """
      using System;
      using Microsoft.AspNetCore.Components;

      public class ToolbarComponent : ComponentBase
      {
        private NavigationManager Nav { get; set; } = null!;

        private Action<bool> RefreshHandler => {|#0:Nav.Refresh|};
        private Action GoHome => () => {|#1:Nav.NavigateTo("/")|};
      }
      """;

    CSharpAnalyzerTest<ComponentSideEffectAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "NavigationManager.Refresh"));
    test.ExpectedDiagnostics.Add(Flag(1, "NavigationManager.NavigateTo"));
    await test.RunAsync();
  }

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
