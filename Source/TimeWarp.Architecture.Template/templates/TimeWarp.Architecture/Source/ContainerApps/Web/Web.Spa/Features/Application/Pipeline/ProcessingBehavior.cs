namespace TimeWarp.Architecture.Features.Applications;

using BlazorState;
using Dawn;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static TimeWarp.Architecture.Features.Applications.ApplicationState;

public class ProcessingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : IRequest<TResponse>
{
  private readonly IMediator Mediator;

  public ProcessingBehavior(IMediator aMediator) { Mediator = aMediator; }

  public async Task<TResponse> Handle
  (
    TRequest aRequest,
    RequestHandlerDelegate<TResponse> aNextHandler,
    CancellationToken aCancellationToken
  )
  {
    if (typeof(TRequest).GetCustomAttributes(typeof(TrackProcessingAttribute), false).Any())
    {
      Guard.Support(aRequest is IAction);
      Guard.Argument(aRequest as object, nameof(aRequest))
        .NotType<StartProcessingAction>()
        .NotType<CompleteProcessingAction>();

      string actionName = typeof(TRequest).Name;
      await Mediator.Send(new StartProcessingAction { ActionName = actionName }).ConfigureAwait(false);
      TResponse response = await aNextHandler().ConfigureAwait(false);
      await Mediator.Send(new CompleteProcessingAction { ActionName = actionName }).ConfigureAwait(false);
      return response;
    }
    else
    {
      TResponse response = await aNextHandler().ConfigureAwait(false);
      return response;
    }
  }
}
