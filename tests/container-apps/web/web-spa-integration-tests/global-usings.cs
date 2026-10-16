#region Purpose
// Project-wide using directives so individual files omit repeated imports.
#endregion

global using TimeWarp.Features.Cloning;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Configuration;
// Auth and options: only the api-server client and weather surfaces use them.
#if(api)
global using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
global using Microsoft.Extensions.Options;
#endif
global using Microsoft.FluentUI.AspNetCore.Components;
global using Shouldly;
global using System.Text.Json;
// Solution usings
global using TimeWarp.Architecture.Features.Applications;
global using TimeWarp.Architecture.Features.Counters;
#if(api)
global using TimeWarp.Architecture.Features.WeatherForecasts;
#endif
global using TimeWarp.Architecture.JsonSerializer.Tests;
global using TimeWarp.Architecture.Services;
global using TimeWarp.Architecture.Testing;
global using TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;
global using OneOf;
global using TimeWarp.Mediator;
global using TimeWarp.State;
global using TimeWarp.State.Plus.Extensions;
global using TimeWarp.Jaribu;
global using TimeWarp.Foundation.Types;
global using static TimeWarp.Jaribu.TestRunner;
