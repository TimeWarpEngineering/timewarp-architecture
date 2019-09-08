namespace TimeWarp.Blazor.Client.Features.Base
{
  using TimeWarp.Blazor.Client.Features.Application;
  using TimeWarp.Blazor.Client.Features.Counter;
  using TimeWarp.Blazor.Client.Features.WeatherForecast;
  using TimeWarp.Blazor.Client.Features.EventStream;
  using BlazorState;

  /// <summary>
  /// Base Handler that makes it easy to access state
  /// </summary>
  /// <typeparam name="TRequest"></typeparam>
  internal abstract class BaseHandler<TRequest> : ActionHandler<TRequest>
    where TRequest : IAction
  {
    public BaseHandler(IStore aStore) : base(aStore) { }

    protected ApplicationState ApplicationState => Store.GetState<ApplicationState>();
    protected WeatherForecastsState WeatherForecastsState => Store.GetState<WeatherForecastsState>();
    protected CounterState CounterState => Store.GetState<CounterState>();
    protected EventStreamState EventStreamState => Store.GetState<EventStreamState>();
  }
}