namespace TimeWarp.Architecture.Features.WeatherForecasts;

using System.Collections.Specialized;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Types;

[TimeWarp.Architecture.SourceGenerator.ApiEndpoint]
public static partial class GetWeatherForecasts
{
    public sealed partial class Query : IRequest<OneOf<Response, SharedProblemDetails>>, IQueryStringRouteProvider, IApiRequest
    {
        /// <summary>
        /// The Number of days of forecasts to get
        /// </summary>
        /// <example>5</example>
        public int? Days { get; set; }

        public string GetRouteWithQueryString()
        {
            var parameters = new NameValueCollection
            {
                { nameof(Days), Days?.ToString() }
            };

            return $"{GetRoute()}?{this.GetQueryString(parameters)}";
        }

        public string GetRoute() => "api/weatherForecasts";
        
        public HttpVerb GetHttpVerb() => HttpVerb.Get;
    }

    public sealed class Response(IEnumerable<WeatherForecastDto> WeatherForecasts) : BaseResponse
    {
        public IEnumerable<WeatherForecastDto> WeatherForecasts { get; init; } = WeatherForecasts;
    }

    /// <summary>
    /// The weather forecast
    /// </summary>
    public sealed class WeatherForecastDto
    {
        /// <summary>
        /// The forecast for this Date
        /// </summary>
        /// <example>2020-06-08T12:32:39.9828696+07:00</example>
        public DateTime Date { get; set; }

        /// <summary>
        /// Summary of the forecast
        /// </summary>
        /// <example>Cool</example>
        public required string Summary { get; set; }

        /// <summary>
        /// Temperature in Celsius
        /// </summary>
        /// <example>24</example>
        public int TemperatureC { get; set; }

        /// <summary>
        /// Temperature in Fahrenheit
        /// </summary>
        /// <example>75</example>
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    }

    public sealed class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            RuleFor(x => x.Days)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Days must be greater than or equal to 1");
        }
    }
}
