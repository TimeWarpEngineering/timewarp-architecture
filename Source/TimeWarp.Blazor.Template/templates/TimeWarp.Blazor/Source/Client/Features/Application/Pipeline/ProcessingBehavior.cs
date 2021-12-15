namespace TimeWarp.Blazor.Features.Applications
{
  using BlazorState;
  using Dawn;
  using MediatR;
  using System.Linq;
  using System.Threading;
  using System.Threading.Tasks;
  using static TimeWarp.Blazor.Features.Applications.ApplicationState;

  public class ProcessingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  {
    private readonly IMediator Mediator;

    public ProcessingBehavior(IMediator aMediator) { Mediator = aMediator; }

    public async Task<TResponse> Handle
    (
      TRequest aRequest,
      CancellationToken aCancellationToken,
      RequestHandlerDelegate<TResponse> aNextHandler
    )
    {
      if (typeof(TRequest).GetCustomAttributes(typeof(TrackProcessingAttribute), false).FirstOrDefault() != null)
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
}

