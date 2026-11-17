#region Purpose
// TWA0026 category cases: each side-effect target flagged in handlers and lifecycle overrides.
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

public class Should_Ban_Direct_Side_Effects_In_Components_By_Category
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Ban_Direct_Side_Effects_In_Components_By_Category>();

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
          _ = await {|#2:Http.PostAsync("api/weather", null)|};
          _ = await {|#3:Http.PutAsync("api/weather/1", null)|};
          _ = await {|#4:Http.PatchAsync("api/weather/1", null)|};
          _ = await {|#5:Http.SendAsync(new HttpRequestMessage(HttpMethod.Get, "api/weather"))|};
          _ = Http.BaseAddress;
        }
      }
      """;

    CSharpAnalyzerTest<ComponentSideEffectAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag(0, "HttpClient.GetStringAsync"));
    test.ExpectedDiagnostics.Add(Flag(1, "HttpClient.DeleteAsync"));
    test.ExpectedDiagnostics.Add(Flag(2, "HttpClient.PostAsync"));
    test.ExpectedDiagnostics.Add(Flag(3, "HttpClient.PutAsync"));
    test.ExpectedDiagnostics.Add(Flag(4, "HttpClient.PatchAsync"));
    test.ExpectedDiagnostics.Add(Flag(5, "HttpClient.SendAsync"));
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
          await {|#4:Local.RemoveItemAsync("k")|};
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
    test.ExpectedDiagnostics.Add(Flag(4, "ILocalStorageService.RemoveItemAsync"));
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
}
