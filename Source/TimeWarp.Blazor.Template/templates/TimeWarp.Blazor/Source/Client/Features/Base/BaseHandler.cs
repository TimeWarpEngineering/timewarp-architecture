namespace TimeWarp.Blazor.Features.Bases
{
  using BlazorState;
  using TimeWarp.Blazor.Features.Applications;
  using TimeWarp.Blazor.Features.Counters;
  using TimeWarp.Blazor.Features.EventStreams;
  using TimeWarp.Architecture.Features.Superheros;
  using TimeWarp.Blazor.Features.WeatherForecasts;

  /// <summary>
  /// Base Handler that makes it easy to access state
  /// </summary>
  /// <typeparam name="TAction"></typeparam>
  internal abstract class BaseHandler<TAction> : ActionHandler<TAction>
    where TAction : IAction
  {
    protected ApplicationState ApplicationState => Store.GetState<ApplicationState>();

    protected CounterState CounterState => Store.GetState<CounterState>();

    protected EventStreamState EventStreamState => Store.GetState<EventStreamState>();

    protected WeatherForecastsState WeatherForecastsState => Store.GetState<WeatherForecastsState>();
    protected SuperheroState SuperheroState => Store.GetState<SuperheroState>();
    public BaseHandler(IStore aStore) : base(aStore) { }
  }
}
