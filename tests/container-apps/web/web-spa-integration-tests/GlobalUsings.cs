global using AnyClone;
global using FakeItEasy;
global using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.Options;
global using Microsoft.FluentUI.AspNetCore.Components;
global using Shouldly;
global using System.Text.Json;
// Solution usings
global using TimeWarp.Architecture.Features.Applications;
#if(counter)
global using TimeWarp.Architecture.Features.Counters;
#endif
#if(eventstream)
global using TimeWarp.Architecture.Features.EventStreams;
#endif
#if(api)
global using TimeWarp.Architecture.Features.WeatherForecasts;
#endif
global using TimeWarp.Architecture.JsonSerializer.Tests;
global using TimeWarp.Architecture.Services;
global using TimeWarp.Foundation.Services;
global using TimeWarp.Architecture.Testing;
global using TimeWarp.Architecture.Web.Spa.Integration.Tests.Infrastructure;
global using TimeWarp.Mediator;
global using TimeWarp.State;

global using TimeWarp.Fixie;
global using TimeWarp.Foundation.Types;
