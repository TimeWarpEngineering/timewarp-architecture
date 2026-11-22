#region Purpose
// Source text for the [ContractState] generator tests: a contracts assembly and the host stand-ins.
#endregion

#region Design
// Contracts: a Query fetch (GetThings), a draft-built auth Command (SaveThing, with a get-only member that must not be
// copied), a [Destructive] Command (RemoveThing), and Broken, which has no Response.
// Host: stand-ins with the shapes the generated code names. DefaultApiHandler's constructor order matches web-spa's
// (store, apiService, logger, publisher, ...), because the generator reads the store and publisher types from it.
// ContractStateAttribute matches web-spa's: const templates over GeneratedRequestPatternAttribute.
#endregion

namespace TimeWarp.Architecture.SourceGenerator.Tests;

internal static class ContractStateHostSources
{
  public const string Contracts = """
    namespace App.Contracts
    {
      public interface IApiRequest { }

      public interface IAuthApiRequest : IApiRequest
      {
        System.Guid UserId { get; set; }
      }

      public static class GetThings
      {
        public sealed class Query : IAuthApiRequest
        {
          public System.Guid UserId { get; set; }
          public string? Search { get; set; }
        }

        public sealed class Response
        {
          public int Count { get; set; }
        }
      }

      public static class SaveThing
      {
        public sealed class Command : IAuthApiRequest
        {
          public System.Guid UserId { get; set; }
          public System.Collections.Generic.List<string> Names { get; set; } = [];
          public string Computed => "computed";
        }

        public sealed class Response { }
      }

      public static class RemoveThing
      {
        [TimeWarp.Contracts.Destructive]
        public sealed class Command : IApiRequest
        {
          public System.Guid ThingId { get; set; }
        }

        public sealed class Response { }
      }

      public static class Broken
      {
        public sealed class Command : IApiRequest { }
      }
    }
    """;

  public const string Host = """
    namespace TimeWarp.Mediator
    {
      public interface IAction { }

      public interface IPublisher<T> { }

      public interface ISender
      {
        System.Threading.Tasks.Task Send(object request, System.Threading.CancellationToken cancellationToken);
      }

      public class GeneratedRequestPatternAttribute : System.Attribute
      {
        public GeneratedRequestPatternAttribute(string requestTypeName, string handlerTypeName, string requestInterfaceName, params System.Type[] contracts) { }

        protected GeneratedRequestPatternAttribute(params System.Type[] contracts) { }
      }
    }

    namespace TimeWarp.State
    {
      public sealed class ClientPipeline { }

      public interface IStore
      {
        T GetState<T>() where T : class;
      }

      public abstract class State<TState> where TState : State<TState>
      {
        protected TimeWarp.Mediator.ISender Sender { get; } = null!;
        protected System.Threading.CancellationToken CancellationToken { get; }
      }
    }

    namespace Microsoft.Extensions.Logging
    {
      public interface ILogger<out TCategoryName> { }
    }

    namespace TimeWarp.Features.ActionTracking
    {
      [System.AttributeUsage(System.AttributeTargets.Class)]
      public sealed class TrackActionAttribute : System.Attribute { }
    }

    namespace App.Features
    {
      public interface IBaseAction : TimeWarp.Mediator.IAction { }

      public interface IWebServerApiService { }

      public interface IConfirmationService { }

      public abstract class DefaultApiHandler<TAction, TRequest, TResponse>
        where TAction : IBaseAction
        where TRequest : class
        where TResponse : class
      {
        protected DefaultApiHandler
        (
          TimeWarp.State.IStore store,
          IWebServerApiService apiService,
          Microsoft.Extensions.Logging.ILogger<DefaultApiHandler<TAction, TRequest, TResponse>> logger,
          TimeWarp.Mediator.IPublisher<TimeWarp.State.ClientPipeline> publisher,
          object? validator = null
        )
        {
          Store = store;
        }

        protected TimeWarp.State.IStore Store { get; }

        protected virtual System.Threading.Tasks.Task<bool> ConfirmAsync(TAction action, System.Threading.CancellationToken cancellationToken) =>
          System.Threading.Tasks.Task.FromResult(true);

        protected abstract System.Threading.Tasks.Task<TRequest?> GetRequest(TAction action, System.Threading.CancellationToken cancellationToken);

        protected abstract System.Threading.Tasks.Task HandleSuccess(TResponse response, System.Threading.CancellationToken cancellationToken);
      }

      [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
      public sealed class ContractStateAttribute(params System.Type[] contracts) : TimeWarp.Mediator.GeneratedRequestPatternAttribute(contracts)
      {
        public const string RequestTypeName = "{Host}.{Contract}ActionSet.Action";
        public const string HandlerTypeName = "{Host}.{Contract}ActionSet.Handler";
        public const string RequestInterfaceName = "TimeWarp.Mediator.IAction";

        public System.Type[] DraftRequests { get; set; } = [];

        public System.Type? ApiService { get; set; }
      }
    }
    """;
}
