global using Ardalis.GuardClauses;
global using TimeWarp.State;
global using Blazored.LocalStorage;
global using Blazored.SessionStorage;
global using FluentValidation;
global using FluentValidation.Results;
global using Grpc.Net.Client.Web;
global using Grpc.Net.Client;
global using JetBrains.Annotations;
global using MediatR.Pipeline;
global using MediatR;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Components.Forms;
global using Microsoft.AspNetCore.Components.Web;
global using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
global using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
global using Microsoft.AspNetCore.Components;
global using Microsoft.AspNetCore.Components.Authorization;
global using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
global using Microsoft.AspNetCore.SignalR.Client;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using Microsoft.FluentUI.AspNetCore.Components;
global using Microsoft.FluentUI.AspNetCore.Components.Utilities;
global using Microsoft.JSInterop;
global using OneOf;
global using TimeWarp.Features.JavaScriptInterop;
global using TimeWarp.Features.ReduxDevTools;
global using TimeWarp.Features.Routing;

#if(grpc)
global using ProtoBuf.Grpc.Client;
#endif

global using System;
global using System.Diagnostics.CodeAnalysis;
global using System.Globalization;
global using System.Net;
global using System.Net.Http;
global using System.Net.Http.Headers;
global using System.Net.Http.Json;
global using System.Net.Mime;
global using System.Reflection;
global using System.Security.Claims;
global using System.Text;
global using System.Text.Json;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;
global using TimeWarp.Architecture;

// Solution usings
global using TimeWarp.Architecture.Components;
global using TimeWarp.Architecture.Configuration;
global using TimeWarp.Architecture.Configuration.Passwordless;
global using TimeWarp.Architecture.Extensions;
global using TimeWarp.Architecture.Features;
global using TimeWarp.Architecture.Features.Admin.Roles;
global using TimeWarp.Architecture.Features.Applications;
global using TimeWarp.Architecture.Features.Authentication;
global using TimeWarp.Architecture.Features.Authorization;
global using TimeWarp.Architecture.Features.Chat;
global using TimeWarp.Architecture.Features.EventStreams;
global using TimeWarp.Architecture.Features.Notifications;
global using TimeWarp.Architecture.Features.ProfileMenus;
global using TimeWarp.Architecture.Features.Profiles;
global using TimeWarp.Architecture.Features.Sidebars;
global using TimeWarp.Architecture.Features.ToastNotifications;
global using TimeWarp.Architecture.Hubs;
global using TimeWarp.Architecture.Pipeline.NotificationPostProcessor;
global using TimeWarp.Architecture.Services;
global using TimeWarp.Architecture.Types;
global using TimeWarp.Features.ActionTracking;
global using TimeWarp.Features.StateTransactions;
global using TimeWarp.State.Extensions;
global using TimeWarp.State.Plus.State;
global using static TimeWarp.Architecture.AuthorizationConstants;


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

