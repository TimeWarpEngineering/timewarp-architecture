#region Purpose
// TWA0044–TWA0046: an SPA handler agrees with the contract it sends ([Destructive] and ConfirmAsync, [CatalogAction]
// Permissions, a mock response factory). Task 483, the 464 Option A checks.
#endregion

#region Design
// SPA-ness comes from a global analyzer config (build_property.UsingMicrosoftNETSdkBlazorWebAssembly), as the TWA0022
// tests do. Stubs mirror the shapes the analyzer matches: ApiHandler`3 by name, TimeWarp.Contracts.DestructiveAttribute
// and TimeWarp.State.CatalogActionAttribute by full name, EndpointAuthorize, ContractState and NoMockResponse by
// simple name. Expected locations use {|ID:...|} markup, so each case pins the span without restating messages.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;

public class Should_Keep_Spa_Handlers_In_Agreement_With_Contracts
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Keep_Spa_Handlers_In_Agreement_With_Contracts>();

  private const string SpaGlobalConfig =
    """
    is_global = true
    build_property.UsingMicrosoftNETSdkBlazorWebAssembly = true
    """;

  private const string Stubs =
    """
    #region Purpose
    // Minimal contract, State and web-spa stand-ins for TWA0044–TWA0046 tests.
    #endregion
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    namespace TimeWarp.Contracts
    {
      public sealed class DestructiveAttribute : Attribute
      {
        public DestructiveAttribute(bool destructive = true) { }
      }
    }

    namespace TimeWarp.State
    {
      public sealed class CatalogActionAttribute : Attribute
      {
        public string? Description { get; set; }
        public string[] Permissions { get; set; } = [];
      }
    }

    namespace App
    {
      public sealed class EndpointAuthorizeAttribute : Attribute
      {
        public string? Policy { get; set; }
      }

      public sealed class NoMockResponseAttribute : Attribute
      {
        public NoMockResponseAttribute(string reason) { }
      }

      public sealed class ContractStateAttribute : Attribute
      {
        public ContractStateAttribute(params Type[] contracts) { }
        public Type? ApiService { get; set; }
      }

      public interface IWebServerApiService { }
      public interface IApiServerApiService { }

      public abstract class ApiHandler<TAction, TRequest, TResponse>
      {
        protected virtual Task<bool> ConfirmAsync(TAction action, CancellationToken cancellationToken) => Task.FromResult(true);
      }

      public abstract class DefaultApiHandler<TAction, TRequest, TResponse> : ApiHandler<TAction, TRequest, TResponse> { }

      public sealed class Action { }

      public static class Mocked
      {
        public sealed class Command { }
        public sealed class Response { }
        public static object GetMockResponseFactory() => new();
      }

      [EndpointAuthorize(Policy = "things.manage")]
      public static class Guarded
      {
        public sealed class Command { }
        public sealed class Response { }
        public static object GetMockResponseFactory() => new();
      }

      public static class Removed
      {
        [TimeWarp.Contracts.Destructive]
        public sealed class Command { }
        public sealed class Response { }
        public static object GetMockResponseFactory() => new();
      }

      public static class Unmocked
      {
        public sealed class Query { }
        public sealed class Response { }
      }

      [NoMockResponse("A passkey ceremony has no JSON answer.")]
      public static class Ceremony
      {
        public sealed class Command { }
        public sealed class Response { }
      }

      [NoMockResponse(" ")]
      public static class BlankOptOut
      {
        public sealed class Command { }
        public sealed class Response { }
      }
    }
    """;

  private static CSharpAnalyzerTest<ContractSpaAgreementAnalyzer, RoslynTestVerifier> Test(string source, bool spa = true)
  {
    CSharpAnalyzerTest<ContractSpaAgreementAnalyzer, RoslynTestVerifier> test = new();
    if (spa) test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", SpaGlobalConfig));
    test.TestState.Sources.Add(("Stubs.cs", Stubs));
    test.TestState.Sources.Add(("Feature.cs", source));
    return test;
  }

  // flag: the diagnostic id expected on the handler's name, or null for a clean handler.
  private static string Handler(string request, string body = "", string api = "IWebServerApiService", string? flag = null) =>
    $$"""
    namespace App
    {
      internal sealed class {{(flag is null ? "Handler" : "{|" + flag + ":Handler|}")}} : DefaultApiHandler<Action, {{request}}, object>
      {
        public Handler({{api}} api) { }
        {{body}}
      }
    }
    """;

  private const string Confirm =
    "protected override System.Threading.Tasks.Task<bool> ConfirmAsync(Action action, System.Threading.CancellationToken cancellationToken) => System.Threading.Tasks.Task.FromResult(true);";

  public static async Task Destructive_Request_Without_Confirm_Flags()
  {
    await Test(Handler("Removed.Command", flag: "TWA0044")).RunAsync();
  }

  public static async Task Confirm_Without_Destructive_Request_Flags()
  {
    await Test(Handler("Mocked.Command", Confirm, flag: "TWA0044")).RunAsync();
  }

  public static async Task Destructive_Request_With_Confirm_Is_Clean()
  {
    await Test(Handler("Removed.Command", Confirm)).RunAsync();
  }

  public static async Task Catalog_Permissions_Without_The_Contract_Policy_Flag()
  {
    const string source =
      """
      namespace App
      {
        public static class Actions
        {
          [{|TWA0045:TimeWarp.State.CatalogAction(Description = "Guard a thing.", Permissions = new[] { "other" })|}]
          public sealed class Save { }
        }

        internal sealed class SaveHandler : DefaultApiHandler<Actions.Save, Guarded.Command, object>
        {
          public SaveHandler(IWebServerApiService api) { }
        }
      }
      """;
    await Test(source).RunAsync();
  }

  public static async Task Catalog_Permissions_With_The_Contract_Policy_Are_Clean()
  {
    const string source =
      """
      namespace App
      {
        public static class Actions
        {
          [TimeWarp.State.CatalogAction(Description = "Written for the palette.", Permissions = new[] { "things.manage" })]
          public sealed class Save { }
        }

        internal sealed class SaveHandler : DefaultApiHandler<Actions.Save, Guarded.Command, object>
        {
          public SaveHandler(IWebServerApiService api) { }
        }
      }
      """;
    await Test(source).RunAsync();
  }

  public static async Task Web_Server_Handler_Without_A_Mock_Factory_Flags()
  {
    await Test(Handler("Unmocked.Query", flag: "TWA0046")).RunAsync();
  }

  public static async Task Api_Server_Handler_Is_Not_Checked_For_A_Mock()
  {
    await Test(Handler("Unmocked.Query", api: "IApiServerApiService")).RunAsync();
  }

  public static async Task NoMockResponse_With_A_Reason_Opts_Out_And_A_Blank_Reason_Does_Not()
  {
    await Test(Handler("Ceremony.Command")).RunAsync();
    await Test(Handler("BlankOptOut.Command", flag: "TWA0046")).RunAsync();
  }

  public static async Task ContractState_Contract_Without_A_Mock_Factory_Flags_The_Attribute()
  {
    const string source =
      """
      namespace App
      {
        [{|TWA0046:ContractState(typeof(Mocked), typeof(Unmocked))|}]
        public sealed partial class ThingState { }

        [ContractState(typeof(Unmocked), ApiService = typeof(IApiServerApiService))]
        public sealed partial class OtherState { }
      }
      """;
    await Test(source).RunAsync();
  }

  public static async Task Outside_The_Spa_Nothing_Is_Reported()
  {
    await Test(Handler("Removed.Command"), spa: false).RunAsync();
    await Test(Handler("Unmocked.Query"), spa: false).RunAsync();
  }
}
