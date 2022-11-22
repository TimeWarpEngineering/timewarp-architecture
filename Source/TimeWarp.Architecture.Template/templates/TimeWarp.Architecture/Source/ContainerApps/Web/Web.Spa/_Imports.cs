global using BlazorComponentUtilities;
global using BlazorState;
global using BlazorState.Pipeline.ReduxDevTools;
global using BlazorState.Features.JavaScriptInterop;
global using BlazorState.Features.Routing;
global using BlazorState.Services;
global using Dawn;
global using FluentValidation;
global using MediatR;
global using MediatR.Pipeline;
global using Grpc.Net.Client;
global using Grpc.Net.Client.Web;
global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.Forms;
global using Microsoft.AspNetCore.Components.Web;
global using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.JSInterop;
global using PeterLeslieMorris.Blazor.Validation;
global using System;
global using System.Net.Http;
global using System.Reflection;
global using System.Text.Json;
global using System.Threading.Tasks;
global using TimeWarp.Architecture.Analyzers;
global using TimeWarp.Architecture.Components;
global using TimeWarp.Architecture.Configuration;
global using TimeWarp.Architecture.Extensions;
global using TimeWarp.Architecture.Features;
global using TimeWarp.Architecture.Features.Applications;
global using TimeWarp.Architecture.Features.ClientLoaders;
global using TimeWarp.Architecture.Features.EventStreams;
global using TimeWarp.Architecture.Pipeline.NotificationPostProcessor;

#if(counter)
global using TimeWarp.Architecture.Features.Counters;
#endif

#if(api)
global using TimeWarp.Architecture.Features.WeatherForecasts;
#endif

#if(grpc)
global using TimeWarp.Architecture.Features.Superheros;
global using ProtoBuf.Grpc.Client;
#endif
