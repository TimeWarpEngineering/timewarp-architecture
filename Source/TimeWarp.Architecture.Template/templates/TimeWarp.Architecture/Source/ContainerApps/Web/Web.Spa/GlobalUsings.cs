global using BlazorComponentUtilities;
global using BlazorState;
global using TimeWarp.Features.JavaScriptInterop;
global using TimeWarp.Features.Routing;
global using TimeWarp.Features.ReduxDevTools;
global using Dawn;
global using FluentValidation;
global using MediatR;
global using MediatR.Pipeline;
global using Grpc.Net.Client;
global using Grpc.Net.Client.Web;
global using JetBrains.Annotations;
global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.Forms;
global using Microsoft.AspNetCore.Components.Web;
global using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
global using Microsoft.AspNetCore.SignalR.Client;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.JSInterop;
global using Morris.Blazor.Validation;

#if(grpc)
global using ProtoBuf.Grpc.Client;
#endif

global using System;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.Net.Http;
global using System.Reflection;
global using System.Text.Json;
global using System.Threading.Tasks;

// Solution usings
global using TimeWarp.Architecture.Components;
global using TimeWarp.Architecture.Configuration;
global using TimeWarp.Architecture.Extensions;
global using TimeWarp.Architecture.Features;
global using TimeWarp.Architecture.Features.Applications;
global using TimeWarp.Architecture.Features.Chat;
global using TimeWarp.Architecture.Features.ClientLoaders;
global using TimeWarp.Architecture.Features.EventStreams;
global using TimeWarp.Architecture.Features.Notifications;
global using TimeWarp.Architecture.Features.ProfileMenus;
global using TimeWarp.Architecture.Features.Sidebars;
global using TimeWarp.Architecture.Hubs;
global using TimeWarp.Architecture.Pipeline.NotificationPostProcessor;
global using TimeWarp.Architecture.Services;
global using TimeWarp.Architecture.Types;
global using TimeWarp.Features.ActionTracking;


#if(counter)
global using TimeWarp.Architecture.Features.Counters;
#endif

#if(grpc)
global using TimeWarp.Architecture.Features.Superheros;
#endif

#if(api)
global using TimeWarp.Architecture.Features.WeatherForecast.Pages;
global using TimeWarp.Architecture.Features.WeatherForecasts;
#endif

