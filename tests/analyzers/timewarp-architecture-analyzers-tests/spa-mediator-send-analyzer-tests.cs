#region Purpose
// TWA0022: SPA client code must dispatch through generated ActionSet methods, never a raw
// mediator Send.
#endregion

#region Design
// SPA-ness is supplied via a global analyzer config file (build_property.UsingMicrosoft-
// NETSdkBlazorWebAssembly) so tests do not depend on project SDK detection.
// Two cases pin the generated-code behaviour that diverges from every sibling TWA analyzer:
// a razor-generated tree MUST be analyzed (it carries user @code), while a TimeWarp.State
// ActionSet dispatcher tree — which calls Sender.Send and carries no GeneratedCodeAttribute —
// MUST be exempt.
#endregion

namespace TimeWarp.Architecture.Analyzers.Tests;

using Microsoft.CodeAnalysis.CSharp.Testing;

public class Should_Ban_Direct_Mediator_Send_In_Spa
{
  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<Should_Ban_Direct_Mediator_Send_In_Spa>();

  private const string SpaGlobalConfig =
    """
    is_global = true
    build_property.UsingMicrosoftNETSdkBlazorWebAssembly = true
    """;

  // Mirrors TimeWarp.Mediator 13.0.0: ISender declares exactly three Send overloads plus
  // CreateStream; IMediator adds nothing over ISender + IPublisher. CreateStream's real return
  // type is IAsyncEnumerable<T>, simplified here — the analyzer keys on the method name only.
  private const string MediatorStubs =
    """
    #region Purpose
    // Minimal TimeWarp.Mediator stubs for TWA0022 tests.
    #endregion
    using System.Threading;
    using System.Threading.Tasks;

    namespace TimeWarp.Mediator
    {
      public interface IBaseRequest { }
      public interface IRequest : IBaseRequest { }
      public interface IRequest<out TResponse> : IBaseRequest { }
      public interface IStreamRequest<out TResponse> { }

      public interface IPublisher
      {
        Task Publish(object notification, CancellationToken cancellationToken = default);
      }

      public interface ISender
      {
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
        Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest;
        Task<object> Send(object request, CancellationToken cancellationToken = default);
        object CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default);
      }

      public interface IMediator : ISender, IPublisher { }

      public class Mediator : IMediator
      {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) => default!;
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => default!;
        public Task<object> Send(object request, CancellationToken cancellationToken = default) => default!;
        public object CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => default!;
        public Task Publish(object notification, CancellationToken cancellationToken = default) => default!;
      }
    }
    """;

  private static CSharpAnalyzerTest<SpaMediatorSendAnalyzer, RoslynTestVerifier> Test
  (
    string source,
    string sourcePath = "Feature.cs",
    string? globalConfig = SpaGlobalConfig
  )
  {
    CSharpAnalyzerTest<SpaMediatorSendAnalyzer, RoslynTestVerifier> test = new();
    if (globalConfig is not null)
    {
      test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
    }

    test.TestState.Sources.Add(("Stubs.cs", MediatorStubs));
    test.TestState.Sources.Add((sourcePath, source));
    return test;
  }

  private static DiagnosticResult Flag(string path, int line, int startColumn, int endColumn, string receiver) =>
    new DiagnosticResult(id: "TWA0022", DiagnosticSeverity.Warning)
      .WithSpan(path, line, startColumn, line, endColumn)
      .WithArguments(receiver);

  public static async Task Given_Component_Sending_Via_Inherited_Mediator_Flags()
  {
    // The shape TWA0022 exists for: Mediator is inherited protected from the TimeWarp.State
    // component base, so no access modifier can prevent this call.
    const string source =
      """
      #region Purpose
      // SPA component dispatching directly.
      #endregion
      using System.Threading;
      using System.Threading.Tasks;
      using TimeWarp.Mediator;

      public class Ping : IRequest { }

      public abstract class BaseComponent
      {
        protected IMediator Mediator { get; } = default!;
      }

      public class CounterPage : BaseComponent
      {
        public async Task Go(CancellationToken cancellationToken)
        {
          await Mediator.Send(new Ping(), cancellationToken);
        }
      }
      """;

    CSharpAnalyzerTest<SpaMediatorSendAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 19, 11, 55, "IMediator"));
    await test.RunAsync();
  }

  public static async Task Given_NonComponent_Client_Service_Flags_Every_Send_Overload()
  {
    // Scope is ALL SPA client code — services, handlers and pipeline behaviors included.
    const string source =
      """
      #region Purpose
      // SPA hub wrapper dispatching directly (ChatHubConnection shape).
      #endregion
      using System.Threading;
      using System.Threading.Tasks;
      using TimeWarp.Mediator;

      public class Ping : IRequest { }
      public class Query : IRequest<string> { }

      public class ChatHubConnection
      {
        private readonly ISender Sender;
        public ChatHubConnection(ISender sender) => Sender = sender;

        public async Task Go()
        {
          await Sender.Send(new Ping());
          await Sender.Send(new Query());
          await Sender.Send((object)new Ping());
        }
      }
      """;

    CSharpAnalyzerTest<SpaMediatorSendAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 18, 11, 34, "ISender"));
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 19, 11, 35, "ISender"));
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 20, 11, 42, "ISender"));
    await test.RunAsync();
  }

  public static async Task Given_Concrete_Receiver_Implementing_ISender_Flags()
  {
    // A concrete Mediator (or a state's Sender property typed as the implementation) must not
    // slip past a check written only against the interface.
    const string source =
      """
      #region Purpose
      // SPA code holding the concrete mediator.
      #endregion
      using System.Threading.Tasks;
      using TimeWarp.Mediator;

      public class Ping : IRequest { }

      public class ClientService
      {
        private readonly Mediator Concrete = new();

        public async Task Go()
        {
          await Concrete.Send(new Ping());
        }
      }
      """;

    CSharpAnalyzerTest<SpaMediatorSendAnalyzer, RoslynTestVerifier> test = Test(source);
    test.ExpectedDiagnostics.Add(Flag("Feature.cs", 15, 11, 36, "Mediator"));
    await test.RunAsync();
  }

  public static async Task Given_Razor_Generated_Tree_Flags()
  {
    // Pins the divergence from GeneratedCodeAnalysisFlags.None: a component's @code block
    // compiles into a *_razor.g.cs tree, which sibling analyzers would skip entirely.
    const string source =
      """
      #region Purpose
      // Razor-generated tree carrying user-authored @code.
      #endregion
      using System.Threading.Tasks;
      using TimeWarp.Mediator;

      public class Ping : IRequest { }

      public class StyleGuidePage
      {
        protected IMediator Mediator { get; } = default!;

        private async Task TriggerException()
        {
          await Mediator.Send(new Ping());
        }
      }
      """;

    CSharpAnalyzerTest<SpaMediatorSendAnalyzer, RoslynTestVerifier> test =
      Test(source, sourcePath: "Pages_StyleGuidePage_razor.g.cs");
    test.ExpectedDiagnostics.Add(Flag("Pages_StyleGuidePage_razor.g.cs", 15, 11, 36, "IMediator"));
    await test.RunAsync();
  }

  public static async Task Given_Generated_ActionSet_Dispatcher_Does_Not_Flag()
  {
    // TimeWarp.State's emitted dispatchers legitimately call Sender.Send and carry NO
    // GeneratedCodeAttribute, so the path-based exemption is what saves them.
    const string source =
      """
      #region Purpose
      // TimeWarp.State ActionSet dispatcher, as emitted.
      #endregion
      using System.Threading;
      using System.Threading.Tasks;
      using TimeWarp.Mediator;

      public class ThrowExceptionAction : IRequest { }

      public class CounterState
      {
        protected ISender Sender { get; } = default!;

        public async Task ThrowException(CancellationToken cancellationToken)
        {
          await Sender.Send(new ThrowExceptionAction(), cancellationToken);
        }
      }
      """;

    CSharpAnalyzerTest<SpaMediatorSendAnalyzer, RoslynTestVerifier> test =
      Test(source, sourcePath: "App.CounterState.ThrowExceptionActionSet_Method.g.cs");
    await test.RunAsync();
  }

  public static async Task Given_No_Blazor_WebAssembly_Property_Does_Not_Flag()
  {
    // Server-side code (web-server, api-server) legitimately sends through the mediator.
    const string source =
      """
      #region Purpose
      // Server-side handler.
      #endregion
      using System.Threading.Tasks;
      using TimeWarp.Mediator;

      public class Ping : IRequest { }

      public class ServerEndpoint
      {
        private readonly ISender Sender = default!;

        public async Task Go()
        {
          await Sender.Send(new Ping());
        }
      }
      """;

    CSharpAnalyzerTest<SpaMediatorSendAnalyzer, RoslynTestVerifier> test =
      Test(source, globalConfig: null);
    await test.RunAsync();
  }

  public static async Task Given_Blazor_WebAssembly_Property_False_Does_Not_Flag()
  {
    const string globalConfig =
      """
      is_global = true
      build_property.UsingMicrosoftNETSdkBlazorWebAssembly = false
      """;

    const string source =
      """
      #region Purpose
      // Non-SPA project that still lists the property.
      #endregion
      using System.Threading.Tasks;
      using TimeWarp.Mediator;

      public class Ping : IRequest { }

      public class ServerEndpoint
      {
        private readonly ISender Sender = default!;

        public async Task Go()
        {
          await Sender.Send(new Ping());
        }
      }
      """;

    CSharpAnalyzerTest<SpaMediatorSendAnalyzer, RoslynTestVerifier> test =
      Test(source, globalConfig: globalConfig);
    await test.RunAsync();
  }

  public static async Task Given_Unrelated_Send_Does_Not_Flag()
  {
    // Name-only matching would catch HttpClient/SignalR/hub sends; the receiver must be a
    // mediator. CreateStream and Publish on the mediator itself are also out of scope.
    const string source =
      """
      #region Purpose
      // SPA code sending through things that are not the mediator.
      #endregion
      using System.Threading.Tasks;
      using TimeWarp.Mediator;

      public class Stream : IStreamRequest<string> { }

      public class Transport
      {
        public Task Send(string message) => Task.CompletedTask;
      }

      public class ClientService
      {
        private readonly Transport Transport = new();
        private readonly IMediator Mediator = default!;

        public async Task Go()
        {
          await Transport.Send("hello");
          Mediator.CreateStream(new Stream());
          await Mediator.Publish(new object());
        }
      }
      """;

    CSharpAnalyzerTest<SpaMediatorSendAnalyzer, RoslynTestVerifier> test = Test(source);
    await test.RunAsync();
  }
}
